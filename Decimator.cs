using System;
namespace SantronWinApp // change to your namespace
{
    public sealed class Decimator
    {
        private readonly int _factor;
        private int _count;
        private double _sum;

        public Decimator(double inputHz, double outputHz)
        {
            if (inputHz <= 0) inputHz = 1000;
            if (outputHz <= 0) outputHz = 50;
            _factor = Math.Max(1, (int)Math.Round(inputHz / outputHz));
        }

        public bool Push(double x, out double y)
        {
            _sum += x;
            _count++;

            if (_count >= _factor)
            {
                y = _sum / _count;
                _sum = 0;
                _count = 0;
                return true;
            }

            y = 0;
            return false;
        }

        public void Reset()
        {
            _sum = 0;
            _count = 0;
        }
    }
}
