using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SantronWinApp.EmgLiteForm;

namespace SantronWinApp
{
    public class EmgSignalProcessor
    {
        private readonly double _sampleRate;
        private readonly Queue<double> _rmsWindow;
        private readonly int _rmsWindowSize;
        private readonly double _smoothingFactor;

        // Filter coefficients
        private readonly double _hpAlpha; // High-pass
        private double _bpB0, _bpB1, _bpB2, _bpA1, _bpA2; // Band-pass
        private double _notchB0, _notchB1, _notchB2, _notchA1, _notchA2; // Notch

        // Filter states
        private double _hpPrevInput, _hpPrevOutput;
        private double _bpPrevInput1, _bpPrevInput2, _bpPrevOutput1, _bpPrevOutput2;
        private double _notchX1, _notchX2, _notchY1, _notchY2;

        // Running calculations
        private double _rmsSum;
        private double _smoothedValue;

        // Debug counters
        private int _sampleCounter = 0;

        private readonly EmgOutputMode _mode;

        public EmgSignalProcessor(double sampleRate, int rmsWindowMs, double smoothingPercent, EmgOutputMode mode = EmgOutputMode.RmsEnvelope)
        {
            _mode = mode;
            _sampleRate = sampleRate;

            // High-pass filter (cutoff ~0.5 Hz for DC removal)
            double hpCutoff = 0.5;
            double hpRC = 1.0 / (2.0 * Math.PI * hpCutoff);
            double hpDt = 1.0 / sampleRate;
            _hpAlpha = hpRC / (hpRC + hpDt);

            // Muscle-specific band-pass filter (20-500 Hz) - more stable coefficients
            SetupBandPassFilter(20.0, 500.0);

            // 50 Hz Notch filter - more stable implementation
            SetupNotchFilter(50.0, 10.0); // 50 Hz with 10 Hz bandwidth for stability

            // RMS window
            _rmsWindowSize = Math.Max(1, (int)(sampleRate * rmsWindowMs / 1000.0));
            _rmsWindow = new Queue<double>(_rmsWindowSize);

            // Smoothing factor - ensure it's between 0 and 1
            smoothingPercent = Math.Max(0, Math.Min(100, smoothingPercent));
            _smoothingFactor = smoothingPercent / 100.0;

            Debug.WriteLine($"EMG Processor initialized: SampleRate={sampleRate}, RMS Window={_rmsWindowSize}, Smoothing={_smoothingFactor}");
        }

        private void SetupBandPassFilter(double lowFreq, double highFreq)
        {
            // Ensure frequencies are within Nyquist limit
            lowFreq = Math.Max(0.1, Math.Min(lowFreq, _sampleRate / 2.1));
            highFreq = Math.Max(lowFreq * 1.1, Math.Min(highFreq, _sampleRate / 2.1));

            // More stable band-pass filter implementation
            double w0 = 2.0 * Math.PI * (lowFreq + highFreq) / (2.0 * _sampleRate);
            double bw = 2.0 * Math.PI * (highFreq - lowFreq) / _sampleRate;

            // Ensure Q factor is reasonable
            double Q = Math.Max(0.5, Math.Min(100, w0 / bw));

            double alpha = Math.Sin(w0) / (2.0 * Q);
            double cosW0 = Math.Cos(w0);

            // Band-pass coefficients (normalized)
            double a0 = 1 + alpha;
            _bpB0 = alpha / a0;
            _bpB1 = 0;
            _bpB2 = -alpha / a0;
            _bpA1 = -2 * cosW0 / a0;
            _bpA2 = (1 - alpha) / a0;

            Debug.WriteLine($"Band-pass filter: f_low={lowFreq}, f_high={highFreq}, Q={Q:F2}, b0={_bpB0:F6}, a1={_bpA1:F6}, a2={_bpA2:F6}");
        }

        private void SetupNotchFilter(double notchFreq, double bandwidth)
        {
            // Ensure notch frequency is within range
            notchFreq = Math.Max(1, Math.Min(notchFreq, _sampleRate / 2.1));
            bandwidth = Math.Max(1, Math.Min(bandwidth, notchFreq / 2));

            double w0 = 2.0 * Math.PI * notchFreq / _sampleRate;
            double alpha = Math.Sin(w0) / (2.0 * bandwidth);
            double cosW0 = Math.Cos(w0);

            // Notch filter coefficients (normalized)
            double a0 = 1 + alpha;
            _notchB0 = 1 / a0;
            _notchB1 = -2 * cosW0 / a0;
            _notchB2 = 1 / a0;
            _notchA1 = -2 * cosW0 / a0;
            _notchA2 = (1 - alpha) / a0;

            Debug.WriteLine($"Notch filter: f={notchFreq}, BW={bandwidth}, b0={_notchB0:F6}, b1={_notchB1:F6}, b2={_notchB2:F6}");
        }

        public double Process(double rawValue)
        {
            _sampleCounter++;

            // Clamp input value to prevent extreme values
            rawValue = ClampValue(rawValue, -10000, 10000);

            // Step 1: High-pass filter (DC removal)
            double hpOutput = _hpAlpha * (_hpPrevOutput + rawValue - _hpPrevInput);
            hpOutput = ClampValue(hpOutput, -10000, 10000);
            _hpPrevInput = rawValue;
            _hpPrevOutput = hpOutput;

            // Step 2: Muscle-specific band-pass filter
            double bpOutput = ApplyBandPassFilter(hpOutput);
            bpOutput = ClampValue(bpOutput, -10000, 10000);

            // Step 3: 50 Hz Notch filter
            double notchOutput = ApplyNotchFilter(bpOutput);
            notchOutput = ClampValue(notchOutput, -10000, 10000);

            // If caller wants signed waveform, return signed filtered value (optionally smoothed)
            if (_mode == EmgOutputMode.SignedFiltered)
            {
                // simple smoothing on signed signal (re-use same smoothing percent)
                if (double.IsNaN(_smoothedValue) || double.IsInfinity(_smoothedValue))
                    _smoothedValue = 0;

                if (_smoothedValue == 0) _smoothedValue = notchOutput;
                _smoothedValue = (_smoothingFactor * notchOutput) + ((1 - _smoothingFactor) * _smoothedValue);

                // clamp symmetric
                _smoothedValue = ClampValue(_smoothedValue, -5000, 5000);
                return _smoothedValue;
            }


            // Step 4: RMS Envelope calculation
            double squared = notchOutput * notchOutput;

            // Ensure squared value is reasonable
            if (double.IsNaN(squared) || double.IsInfinity(squared))
            {
                squared = 0;
                Debug.WriteLine($"Warning: Invalid squared value at sample {_sampleCounter}");
            }

            _rmsWindow.Enqueue(squared);
            _rmsSum += squared;

            if (_rmsWindow.Count > _rmsWindowSize)
            {
                _rmsSum -= _rmsWindow.Dequeue();
            }

            // Ensure we don't divide by zero or take sqrt of negative
            double rms;
            if (_rmsWindow.Count > 0 && _rmsSum >= 0)
            {
                rms = Math.Sqrt(_rmsSum / _rmsWindow.Count);
            }
            else
            {
                rms = 0;
            }

            rms = ClampValue(rms, 0, 5000); // EMG signals typically < 5000 uV

            // Step 5: Exponential smoothing
            if (double.IsNaN(_smoothedValue) || double.IsInfinity(_smoothedValue))
            {
                _smoothedValue = 0;
            }

            if (_smoothedValue == 0) _smoothedValue = rms;
            _smoothedValue = (_smoothingFactor * rms) + ((1 - _smoothingFactor) * _smoothedValue);

            // Final clamp
            _smoothedValue = ClampValue(_smoothedValue, 0, 5000);

            // Debug output every 1000 samples
            if (_sampleCounter % 1000 == 0)
            {
                Debug.WriteLine($"Sample {_sampleCounter}: Raw={rawValue:F2}, HP={hpOutput:F2}, BP={bpOutput:F2}, Notch={notchOutput:F2}, RMS={rms:F2}, Smoothed={_smoothedValue:F2}");
            }

            return _smoothedValue;
        }

        private double ApplyBandPassFilter(double input)
        {
            // Check for NaN/Infinity
            if (double.IsNaN(input) || double.IsInfinity(input))
            {
                ResetFilterStates();
                return 0;
            }

            double output = _bpB0 * input + _bpB1 * _bpPrevInput1 + _bpB2 * _bpPrevInput2
                          - _bpA1 * _bpPrevOutput1 - _bpA2 * _bpPrevOutput2;

            // Update filter states
            _bpPrevInput2 = _bpPrevInput1;
            _bpPrevInput1 = input;
            _bpPrevOutput2 = _bpPrevOutput1;
            _bpPrevOutput1 = output;

            return output;
        }

        private double ApplyNotchFilter(double input)
        {
            // Check for NaN/Infinity
            if (double.IsNaN(input) || double.IsInfinity(input))
            {
                ResetNotchStates();
                return 0;
            }

            double output = _notchB0 * input + _notchB1 * _notchX1 + _notchB2 * _notchX2
                          - _notchA1 * _notchY1 - _notchA2 * _notchY2;

            // Update filter states
            _notchX2 = _notchX1;
            _notchX1 = input;
            _notchY2 = _notchY1;
            _notchY1 = output;

            return output;
        }

        private void ResetFilterStates()
        {
            _hpPrevInput = 0;
            _hpPrevOutput = 0;
            _bpPrevInput1 = 0;
            _bpPrevInput2 = 0;
            _bpPrevOutput1 = 0;
            _bpPrevOutput2 = 0;
            _notchX1 = 0;
            _notchX2 = 0;
            _notchY1 = 0;
            _notchY2 = 0;
        }

        private void ResetNotchStates()
        {
            _notchX1 = 0;
            _notchX2 = 0;
            _notchY1 = 0;
            _notchY2 = 0;
        }

        private double ClampValue(double value, double min, double max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;

            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
