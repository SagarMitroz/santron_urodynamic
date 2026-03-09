using SantronWinApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace SantronWinApp.Test
{

    public sealed class TestRecorder
    {
        private readonly List<SampleFrame> _frames = new List<SampleFrame>();
        private readonly object _gate = new object();

        private readonly Stopwatch _sw = new Stopwatch();
        private bool _isRecording;
        private bool _isPaused;

        // You can expose metadata if you like
        public bool IsRecording => _isRecording;
        public bool IsPaused => _isPaused;
        public TimeSpan Elapsed => _sw.Elapsed;

        public void Start()
        {
            lock (_gate)
            {
                _frames.Clear();
                _isRecording = true;
                _isPaused = false;
                _sw.Reset();
                _sw.Start();
            }
        }

        public void Pause()
        {
            lock (_gate)
            {
                if (!_isRecording) return;
                _isPaused = true;
                _sw.Stop();
            }
        }

        public void Resume()
        {
            lock (_gate)
            {
                if (!_isRecording) return;
                _isPaused = false;
                _sw.Start();
            }
        }

        public void Stop()
        {
            lock (_gate)
            {
                if (!_isRecording) return;
                _isRecording = false;
                _sw.Stop();
            }
        }

        // Call this from your plotting callback for each new frame
        public void Append(SampleFrame frame)
        {
            lock (_gate)
            {
                if (!_isRecording || _isPaused) return;
                _frames.Add(frame);
            }
        }

        // Save the currently captured frames as CSV:
        // Header: Time,Ch1,Ch2,... (Time in seconds, channels as double)
        public void SaveCsv(string path)
        {
            List<SampleFrame> snapshot;
            lock (_gate)
            {
                snapshot = _frames.ToList();
            }

            using (var w = new StreamWriter(path))
            {
                if (snapshot.Count == 0)
                {
                    // still write a header with no data, infer channel count as 0
                    w.WriteLine("Time");
                    return;
                }


                int chCount = snapshot[0].Values?.Length ?? 0;
                var header = new List<string> { "Time" };
                for (int i = 1; i <= chCount; i++) header.Add($"Ch{i}");
                w.WriteLine(string.Join(",", header));

                var inv = CultureInfo.InvariantCulture;
                foreach (var fr in snapshot)
                {
                    var line = new List<string> { fr.T.ToString(inv) };
                    if (chCount > 0)
                    {
                        line.AddRange(fr.Values.Select(v => v.ToString(inv)));
                    }
                    w.WriteLine(string.Join(",", line));
                }
            }
        }

        // Load a CSV previously saved by SaveCsv
        public static (double[] t, double[][] channels) LoadCsv(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length <= 1) return (Array.Empty<double>(), Array.Empty<double[]>());

            var tList = new List<double>();
            var colLists = new List<List<double>>();

            // Determine columns from header
            var header = lines[0].Split(',');
            int chColumns = Math.Max(0, header.Length - 1);
            for (int i = 0; i < chColumns; i++) colLists.Add(new List<double>());

            var inv = CultureInfo.InvariantCulture;
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var parts = lines[i].Split(',');
                if (parts.Length == 0) continue;

                if (double.TryParse(parts[0], NumberStyles.Float, inv, out var tVal))
                {
                    tList.Add(tVal);
                    for (int c = 0; c < chColumns; c++)
                    {
                        var s = (c + 1) < parts.Length ? parts[c + 1] : null;
                        if (double.TryParse(s, NumberStyles.Float, inv, out var v))
                            colLists[c].Add(v);
                        else
                            colLists[c].Add(double.NaN);
                    }
                }
            }

            var t = tList.ToArray();
            var channels = colLists.Select(l => l.ToArray()).ToArray();
            return (t, channels);
        }
    }
}