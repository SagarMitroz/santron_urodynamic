using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SantronWinApp
{
    public class NotchFilter50Hz
    {// Filter coefficients for 50Hz notch at 500Hz sampling rate
        // These are pre-calculated for 50Hz rejection at 500Hz sampling rate
        private readonly double[] _bCoeffs = { 0.989, -1.561, 0.989 };
        private readonly double[] _aCoeffs = { 1.0, -1.561, 0.978 };

        // Input history (x[n-1], x[n-2])
        private double _x1 = 0, _x2 = 0;
        // Output history (y[n-1], y[n-2])
        private double _y1 = 0, _y2 = 0;

        public NotchFilter50Hz()
        {
            // Optional: Initialize with zeros
            Reset();
        }

        public void Reset()
        {
            _x1 = _x2 = _y1 = _y2 = 0;
        }

        public double Process(double input)
        {
            // Direct Form I implementation (more intuitive)
            // y[n] = (b0*x[n] + b1*x[n-1] + b2*x[n-2] - a1*y[n-1] - a2*y[n-2]) / a0

            double output = (_bCoeffs[0] * input + _bCoeffs[1] * _x1 + _bCoeffs[2] * _x2
                           - _aCoeffs[1] * _y1 - _aCoeffs[2] * _y2) / _aCoeffs[0];

            // Update history
            _x2 = _x1;
            _x1 = input;
            _y2 = _y1;
            _y1 = output;

            return output;
        }

        // Alternative: Process an array of samples
        public double[] ProcessArray(double[] inputs)
        {
            double[] outputs = new double[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                outputs[i] = Process(inputs[i]);
            }
            return outputs;
        }


    }
}
