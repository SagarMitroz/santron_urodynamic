using System;
using NationalInstruments.DAQmx;

public sealed class ArmController : IDisposable
{
    private readonly string _devicePort;

    // C++ state variables
    private int startstop = 0;
    private int stepperspeed = 0;
    private int stepperdir = 0;

    // Must be U32 (same as DAQmxWriteDigitalU32)
    private uint datastm = 0;

    public ArmController(string devicePort = "Dev1/port0")
    {
        _devicePort = devicePort ?? "Dev1/port0";
    }

    public void StartArm()
    {
        startstop = 1;
        OnBase();
    }

    public void StopArm()
    {
        startstop = 0;
        OnBase();
    }

    public void ToggleDirection()
    {
        stepperdir = (stepperdir == 0) ? 1 : 0;
        OnBase();
        return;
    }

    public void ToggleSpeed()
    {
        stepperspeed = (stepperspeed == 0) ? 1 : 0;
        OnBase();
    }

    /// <summary>
    /// Exact C++ OnBase() behavior replicated in C#
    /// </summary>
    private void OnBase()
    {
        // ===== Bit logic (IDENTICAL to C++) =====
        if (startstop == 1)
        {
            if (stepperspeed == 0 && stepperdir == 0) datastm = 0x01;
            if (stepperspeed == 1 && stepperdir == 0) datastm = 0x03;
            if (stepperspeed == 0 && stepperdir == 1) datastm = 0x05;
            if (stepperspeed == 1 && stepperdir == 1) datastm = 0x07;
        }
        else
        {
            // datastm = datastm & 0x06;
            datastm &= 0x06;
        }

        Task task = null;

        try
        {
            task = new Task();

            // DAQmxCreateDOChan(taskHandle,"Dev1/port0","",DAQmx_Val_ChanForAllLines)
            task.DOChannels.CreateChannel(
                _devicePort,
                "",
                ChannelLineGrouping.OneChannelForAllLines);

            // DAQmxStartTask
            task.Start();

            // DAQmxWriteDigitalU32(... GroupByChannel ...)
            var writer = new DigitalSingleChannelWriter(task.Stream);
            writer.WriteSingleSamplePort(true, datastm);
        }
        catch (DaqException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "ArmController DAQ Error: " + ex.Message);
        }
        finally
        {
            // DAQmxStopTask + DAQmxClearTask (safe even if Start failed)
            try { task?.Stop(); } catch { }
            task?.Dispose();
        }
    }

    public void Dispose()
    {
        // No persistent task to dispose
    }
}
