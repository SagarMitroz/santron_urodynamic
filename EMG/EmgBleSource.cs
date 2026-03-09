using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace SantronWinApp
{
    public sealed class EmgBleSource : IEmgSampleSource, IDisposable
    {
        public event Action<double> OnSample;
        public event Action<string> OnConnectionStateChanged;
        public event Action<int> OnBatteryLevelChanged; // NEW: Battery level event

        public double SampleRateHz { get; private set; } = 1000;

        // Battery related properties
        public int? BatteryLevel { get; private set; } = null; // NEW: Current battery level
        public bool HasBattery { get; private set; } = false; // NEW: Whether device supports battery

        // UUIDs including battery characteristic
        private static readonly Guid BATTERY_CHAR_UUID = Guid.Parse("f633d0ec-46b4-43c1-a39f-1ca06d0602e1"); // NEW: Battery characteristic UUID

        private BluetoothLEDevice _device;
        private GattDeviceService _service;
        private GattCharacteristic _dataChar;
        private GattCharacteristic _controlChar;
        private GattCharacteristic _batteryChar; // NEW: Battery characteristic
        private BluetoothLEAdvertisementWatcher _watcher;

        private volatile bool _running;
        private volatile bool _isConnecting;
        private long _notifyCount;
        private ulong _lastKnownAddress;

        // Discovery
        public string DeviceNameContains { get; set; } = "NPG-";
        public ulong? ForcedBluetoothAddress { get; set; } = null;

        // Firmware control protocol (ASCII)
        public bool SendStartCommand { get; set; } = true;

        // 0..2 (firmware packs 3 channels)
        public int PlotChannelIndex { get; set; } = 0;

        public bool EnableDebugLogs { get; set; } = true;

        // Reconnection settings
        public bool AutoReconnect { get; set; } = true;
        public int ReconnectDelayMs { get; set; } = 2000;
        public int MaxReconnectAttempts { get; set; } = 5;

        public bool IsConnected =>
            _device != null &&
            _device.ConnectionStatus == BluetoothConnectionStatus.Connected &&
            _dataChar != null;

        public void Start() => _ = SafeStartAsync();
        public void Stop()
        {
            _running = false;
            AutoReconnect = false; // Disable auto-reconnect on manual stop
            _ = SafeStopAsync();
        }
        public void Dispose()
        {
            _running = false;
            AutoReconnect = false;
            _ = SafeStopAsync();
        }

        private async Task SafeStartAsync()
        {
            try { await StartAsync().ConfigureAwait(false); }
            catch (Exception ex)
            {
                Log("StartAsync FAILED: " + ex);
                RaiseState("ERROR: " + ex.Message);
                _running = false;
                _isConnecting = false;

                // Attempt reconnection if enabled
                if (AutoReconnect && _running)
                {
                    _ = Task.Run(async () => await AttemptReconnectAsync().ConfigureAwait(false));
                }
            }
        }


        private async Task DiscoverGattAsync()
        {
            Log("DiscoverGattAsync: Begin");

            var servicesResult = await _device
                .GetGattServicesAsync(BluetoothCacheMode.Uncached)
                .ToTask()
                .ConfigureAwait(false);

            if (servicesResult.Status != GattCommunicationStatus.Success)
                throw new InvalidOperationException("GATT service discovery failed");

            foreach (var service in servicesResult.Services)
            {
               // Log($"SERVICE: {service.Uuid}");

                var charsResult = await service
                    .GetCharacteristicsAsync(BluetoothCacheMode.Uncached)
                    .ToTask()
                    .ConfigureAwait(false);

                if (charsResult.Status != GattCommunicationStatus.Success)
                    continue;

                foreach (var ch in charsResult.Characteristics)
                {
                    var props = ch.CharacteristicProperties;
                  //  Log($"CHAR: {ch.Uuid} PROPS={props}");

                    // 🔹 DATA STREAM → Notify
                    if (_dataChar == null &&
                        props.HasFlag(GattCharacteristicProperties.Notify))
                    {
                        _dataChar = ch;
                        Log("→ DATA characteristic selected");
                    }

                    // 🔹 CONTROL → Write
                    if (_controlChar == null &&
                       (props.HasFlag(GattCharacteristicProperties.Write) ||
                        props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse)))
                    {
                        _controlChar = ch;
                        Log("→ CONTROL characteristic selected");
                    }

                    // 🔹 BATTERY → Notify or Read (NEW)
                    if (_batteryChar == null && ch.Uuid == BATTERY_CHAR_UUID)
                    {
                        _batteryChar = ch;
                        HasBattery = true;
                        Log("→ BATTERY characteristic selected");
                    }
                }
            }

            if (_dataChar == null)
                throw new InvalidOperationException("Notify characteristic not found");

            Log("DiscoverGattAsync: Completed");
        }

        // NEW: Enable battery notifications and read initial value
        private async Task EnableBatteryNotificationsAsync()
        {
            if (_batteryChar == null) return;

            try
            {
                // First try to read initial battery level
                var readResult = await _batteryChar.ReadValueAsync(BluetoothCacheMode.Uncached)
                    .ToTask().ConfigureAwait(false);

                if (readResult.Status == GattCommunicationStatus.Success)
                {
                    ProcessBatteryValue(readResult.Value);
                }

                // Enable notifications if supported
                if (_batteryChar.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    _batteryChar.ValueChanged += OnBatteryValueChanged;

                    var status = await _batteryChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify)
                        .ToTask().ConfigureAwait(false);

                    Log($"Battery notifications enabled: {status}");
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to setup battery: {ex.Message}");
            }
        }

        // NEW: Process battery value
        private void ProcessBatteryValue(IBuffer value)
        {
            try
            {
                var reader = DataReader.FromBuffer(value);
                if (reader.UnconsumedBufferLength == 1) // Battery level is typically 1 byte
                {
                    byte batteryValue = reader.ReadByte();
                    BatteryLevel = batteryValue;

                   // Log($"Battery level: {BatteryLevel}%");

                    // Raise event for UI
                    OnBatteryLevelChanged?.Invoke(BatteryLevel.Value);
                }
            }
            catch (Exception ex)
            {
                Log($"Error processing battery value: {ex.Message}");
            }
        }

        // NEW: Battery value changed handler
        private void OnBatteryValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            ProcessBatteryValue(args.CharacteristicValue);
        }

        private async Task SafeStopAsync()
        {
            try { await StopAsync().ConfigureAwait(false); }
            catch (Exception ex) { Log("StopAsync FAILED: " + ex); }
        }

        public async Task StartAsync(CancellationToken ct = default)
        {
            if (_isConnecting)
            {
                Log("Already connecting, ignoring duplicate start request");
                return;
            }

            _isConnecting = true;

            try
            {
                if (_running && IsConnected)
                {
                    Log("Already running and connected");
                    return;
                }

                _running = true;
                Interlocked.Exchange(ref _notifyCount, 0);

                // Clean up any existing connection first
                await CleanupConnectionAsync().ConfigureAwait(false);

                RaiseState("SCANNING...");
                Log("StartAsync: Begin");

                ulong addr = ForcedBluetoothAddress ?? _lastKnownAddress;
                if (addr == 0)
                {
                    addr = await ScanForDeviceAddressAsync(TimeSpan.FromSeconds(15), ct).ConfigureAwait(false);
                    _lastKnownAddress = addr;
                }
                else
                {
                    Log($"Using known address: {addr:X}");
                }

                RaiseState("CONNECTING...");
                _device = await BluetoothLEDevice.FromBluetoothAddressAsync(addr).ToTask().ConfigureAwait(false);
                if (_device == null) throw new InvalidOperationException("FromBluetoothAddressAsync returned null.");

                Log($"Connected: Name='{_device.Name}', Status={_device.ConnectionStatus}");

                // Check device name for battery capability (like in TypeScript)
                if (_device.Name != null)
                {
                    if (_device.Name.Contains("3CH") || _device.Name.Contains("6CH"))
                    {
                        HasBattery = true;
                        Log($"Device {_device.Name} supports battery");
                    }
                }

                RaiseState($"CONNECTED: {_device.Name}");

                _device.ConnectionStatusChanged += OnDeviceConnectionStatusChanged;

                await DiscoverGattAsync().ConfigureAwait(false);

                Log($"DATA characteristic found. Props={_dataChar.CharacteristicProperties}");
                if (_controlChar != null)
                    Log("CONTROL characteristic found.");

                if (_batteryChar != null)
                    Log("BATTERY characteristic found.");

                // Enable battery notifications if available
                if (HasBattery && _batteryChar != null)
                {
                    await EnableBatteryNotificationsAsync().ConfigureAwait(false);
                }

                RaiseState("ENABLING NOTIFY...");
                _dataChar.ValueChanged += DataCharOnValueChanged;

                // Notify then Indicate fallback
                var cfg = await _dataChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                              GattClientCharacteristicConfigurationDescriptorValue.Notify)
                          .ToTask().ConfigureAwait(false);

                Log($"Enable notify result: {cfg}");
                if (cfg != GattCommunicationStatus.Success)
                {
                    Log("Notify failed, trying Indicate...");
                    cfg = await _dataChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                              GattClientCharacteristicConfigurationDescriptorValue.Indicate)
                          .ToTask().ConfigureAwait(false);
                    Log($"Enable indicate result: {cfg}");
                }

                if (cfg != GattCommunicationStatus.Success)
                    throw new InvalidOperationException($"Failed to enable Notify/Indicate (Status={cfg}).");

                RaiseState("NOTIFY ENABLED");

                if (SendStartCommand)
                {
                    RaiseState("SENDING START...");
                    await SendControlTextAsync("START", ct).ConfigureAwait(false);
                    Log("START command sent.");
                }

                Log("StartAsync: Completed OK. Waiting for notifications...");
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private void OnDeviceConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            Log($"ConnectionStatusChanged: {sender.ConnectionStatus}");
            RaiseState("LINK: " + sender.ConnectionStatus);

            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                Log("Device disconnected - cleaning up");
                _ = Task.Run(async () =>
                {
                    await CleanupConnectionAsync().ConfigureAwait(false);

                    if (AutoReconnect && _running)
                    {
                        Log("Auto-reconnect enabled, attempting to reconnect...");
                        await AttemptReconnectAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        RaiseState("DISCONNECTED");
                    }
                });
            }
        }

        private async Task AttemptReconnectAsync()
        {
            int attempts = 0;
            while (_running && AutoReconnect && attempts < MaxReconnectAttempts)
            {
                attempts++;
                Log($"Reconnection attempt {attempts}/{MaxReconnectAttempts}");
                RaiseState($"RECONNECTING... (Attempt {attempts}/{MaxReconnectAttempts})");

                await Task.Delay(ReconnectDelayMs).ConfigureAwait(false);

                try
                {
                    await StartAsync().ConfigureAwait(false);
                    if (IsConnected)
                    {
                        Log("Reconnection successful!");
                        RaiseState("RECONNECTED");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log($"Reconnection attempt {attempts} failed: {ex.Message}");
                }
            }

            if (attempts >= MaxReconnectAttempts)
            {
                Log("Max reconnection attempts reached");
                RaiseState("RECONNECT FAILED");
                _running = false;
            }
        }

        private async Task CleanupConnectionAsync()
        {
            Log("CleanupConnectionAsync: Begin");

            try
            {
                if (_batteryChar != null) // NEW: Clean up battery characteristic
                {
                    try
                    {
                        _batteryChar.ValueChanged -= OnBatteryValueChanged;
                        await _batteryChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                            GattClientCharacteristicConfigurationDescriptorValue.None)
                            .ToTask().ConfigureAwait(false);
                    }
                    catch (Exception ex) { Log("Failed to cleanup battery characteristic: " + ex.Message); }
                    _batteryChar = null;
                }

                if (_dataChar != null)
                {
                    try
                    {
                        _dataChar.ValueChanged -= DataCharOnValueChanged;
                        // Try to disable notifications
                        await _dataChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                            GattClientCharacteristicConfigurationDescriptorValue.None)
                            .ToTask().ConfigureAwait(false);
                    }
                    catch (Exception ex) { Log("Failed to cleanup data characteristic: " + ex.Message); }
                    _dataChar = null;
                }

                _controlChar = null;

                if (_service != null)
                {
                    try { _service.Dispose(); } catch { }
                    _service = null;
                }

                if (_device != null)
                {
                    try
                    {
                        _device.ConnectionStatusChanged -= OnDeviceConnectionStatusChanged;
                        _device.Dispose();
                    }
                    catch (Exception ex) { Log("Failed to cleanup device: " + ex.Message); }
                    _device = null;
                }

                // Reset battery state
                HasBattery = false;
                BatteryLevel = null;
            }
            catch (Exception ex)
            {
                Log("CleanupConnectionAsync error: " + ex.Message);
            }

            Log("CleanupConnectionAsync: Completed");
        }

        public async Task StopAsync()
        {
            RaiseState("STOPPING...");
            Log("StopAsync: Begin");
            _running = false;
            AutoReconnect = false;

            try
            {
                if (_controlChar != null && _device?.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    try
                    {
                        await SendControlTextAsync("STOP").ConfigureAwait(false);
                        Log("STOP command sent");
                    }
                    catch (Exception ex) { Log("STOP command failed (ignored): " + ex.Message); }
                }
            }
            catch (Exception ex) { Log("Error during stop: " + ex.Message); }

            await CleanupConnectionAsync().ConfigureAwait(false);

            RaiseState("STOPPED");
            Log("StopAsync: Completed");
        }

        public Task SendControlTextAsync(string text, CancellationToken ct = default)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return SendControlAsync(Encoding.ASCII.GetBytes(text), ct);
        }

        public async Task SendControlAsync(byte[] payload, CancellationToken ct = default)
        {
            if (_controlChar == null) throw new InvalidOperationException("Control characteristic not available.");
            if (payload == null || payload.Length == 0) throw new ArgumentException("Control payload is empty.", nameof(payload));

            var writer = new DataWriter();
            writer.WriteBytes(payload);

            var status = await _controlChar.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse)
                                           .ToTask().ConfigureAwait(false);

            Log("CONTROL write: " + status + " Payload=" + BitConverter.ToString(payload));
            if (status != GattCommunicationStatus.Success)
                throw new InvalidOperationException($"Control write failed (Status={status}).");
        }

        // NEW: Method to read battery level on demand
        public async Task<int?> ReadBatteryLevelAsync()
        {
            if (_batteryChar == null)
                return null;

            try
            {
                var result = await _batteryChar.ReadValueAsync(BluetoothCacheMode.Uncached)
                    .ToTask().ConfigureAwait(false);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    var reader = DataReader.FromBuffer(result.Value);
                    if (reader.UnconsumedBufferLength == 1)
                    {
                        byte level = reader.ReadByte();
                        BatteryLevel = level;
                        return level;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error reading battery: {ex.Message}");
            }

            return null;
        }

        private async Task<ulong> ScanForDeviceAddressAsync(TimeSpan timeout, CancellationToken ct)
        {
            Log("ScanForDeviceAddressAsync: Starting BLE advertisement watcher...");
            var tcs = new TaskCompletionSource<ulong>();

            int advSeen = 0;
            string nameNeedle = DeviceNameContains?.Trim();

            _watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };

            _watcher.Received += (w, e) =>
            {
                try
                {
                    advSeen++;
                    string name = e.Advertisement.LocalName ?? "";

                    if (advSeen <= 50)
                        Log($"ADV#{advSeen}: addr={e.BluetoothAddress:X}, rssi={e.RawSignalStrengthInDBm}, name='{name}', svcCount={e.Advertisement.ServiceUuids.Count}");

                    if (!string.IsNullOrWhiteSpace(nameNeedle))
                    {
                        if (string.IsNullOrWhiteSpace(name)) return;
                        if (name.IndexOf(nameNeedle, StringComparison.OrdinalIgnoreCase) < 0) return;
                    }

                    tcs.TrySetResult(e.BluetoothAddress);
                }
                catch (Exception ex) { Log("ADV handler error: " + ex.Message); }
            };

            _watcher.Stopped += (w, e) => Log($"Watcher stopped: {e.Error} (advSeen={advSeen})");

            _watcher.Start();

            try
            {
                using (var timeoutCts = new CancellationTokenSource(timeout))
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token))
                using (linked.Token.Register(() => tcs.TrySetCanceled()))
                {
                    var addr = await tcs.Task.ConfigureAwait(false);
                    Log($"ScanForDeviceAddressAsync: Selected addr={addr:X}");
                    _lastKnownAddress = addr;
                    return addr;
                }
            }
            finally
            {
                try { _watcher.Stop(); } catch { }
                _watcher = null;
            }
        }

        private void DataCharOnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            try
            {
                long n = Interlocked.Increment(ref _notifyCount);

                var reader = DataReader.FromBuffer(args.CharacteristicValue);
                uint len = reader.UnconsumedBufferLength;
                if (len == 0) return;

                if (n <= 10)
                {
                    byte[] preview = new byte[Math.Min(16, (int)len)];
                    reader.ReadBytes(preview);
                    Log($"Notify#{n}: len={len} bytes, hex={BitConverter.ToString(preview)}");
                    reader = DataReader.FromBuffer(args.CharacteristicValue);
                }
                else if (n == 11)
                {
                    Log("Notifications are flowing. (Further packet logs suppressed.)");
                    RaiseState("STREAMING");
                }

                byte[] buf = new byte[len];
                reader.ReadBytes(buf);

                // Firmware: 10 blocks × 7 bytes = 70 bytes typical
                // [counter:1][ch0:2][ch1:2][ch2:2] Big-endian
                const int blockSize = 7;
                int blocks = (int)len / blockSize;
                if (blocks <= 0) return;

                int ch = PlotChannelIndex;
                if (ch < 0) ch = 0;
                if (ch > 2) ch = 2;

                for (int i = 0; i < blocks; i++)
                {
                    int o = i * blockSize;

                    ushort v0 = (ushort)((buf[o + 1] << 8) | buf[o + 2]);
                    ushort v1 = (ushort)((buf[o + 3] << 8) | buf[o + 4]);
                    ushort v2 = (ushort)((buf[o + 5] << 8) | buf[o + 6]);

                    //double value = (ch == 0) ? v0 : (ch == 1) ? v1 : v2;
                    //OnSample?.Invoke(value);
                    ushort rawAdc = (ch == 0) ? v0 : (ch == 1) ? v1 : v2;

                    // Center around 0 - your device sends ~1000-2500 range
                    // Assuming 12-bit ADC (0-4095) with center at 2048
                    double centered = rawAdc - 2048.0;

                    // Debug
                    if (i == 0 && n % 100 == 0)
                      //  Log($"Raw={rawAdc}, Centered={centered:F1}");

                    OnSample?.Invoke(centered);
                }
            }
            catch (Exception ex)
            {
                Log("ValueChanged parse error: " + ex.Message);
            }
        }

        private void RaiseState(string state)
        {
            try { OnConnectionStateChanged?.Invoke(state); } catch { }
        }

        private void Log(string msg)
        {
            if (!EnableDebugLogs) return;
            Debug.WriteLine("[BLE] " + msg);
        }
    }
}