using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertMerge
{
    public enum InterpolationMode
    {
        Undefined,
        Previous, //get the value of the previous time
        Next, //get the value of the next time 
        Nearest, //get the value of the nearest time
        Linear, //interpolate linearly
        Cubic
    }

    public static class Interpolator
    {
        private static Dictionary<string, InterpolationMode> getInterpolationDictionary()
        {
            Dictionary<string, InterpolationMode> d = new Dictionary<string, InterpolationMode>();
            d.Add("next", InterpolationMode.Next);
            d.Add("linear", InterpolationMode.Linear);
            d.Add("nearest", InterpolationMode.Nearest);
            d.Add("previous", InterpolationMode.Previous);
            d.Add("cubic", InterpolationMode.Cubic);
            return d;
        }

        private static Dictionary<string, InterpolationMode> _interpolationDictionary = getInterpolationDictionary();
        public static Dictionary<string, InterpolationMode> InterpolationDictionary { get { return _interpolationDictionary; } }

        /// <summary>
        /// Returns a linear interpolation betweeen y1 and y2.
        /// </summary>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="mu">If mu=0 returns y1 and  if mu=1 it returns y2.</param>
        /// <returns></returns>
        public static double Linear(double y1, double y2, double mu)
        {
            return y1 * (1 - mu) + y2 * mu;
        }

        public static double Cosine(double y1, double y2, double mu)
        {
            double mu2;
            mu2 = (1 - Math.Cos(mu * Math.PI)) / 2;
            return (y1 * (1 - mu2) + y2 * mu2);
        }

        public static double Cubic(double y0, double y1, double y2, double y3, double mu)
        {
            double a0, a1, a2, a3, mu2;

            mu2 = mu * mu;
            a0 = y3 - y2 - y0 + y1;
            a1 = y0 - y1 - a0;
            a2 = y2 - y0;
            a3 = y1;

            return a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3;
        }

        public static double CubicCatmullRom(double y0, double y1, double y2, double y3, double mu)
        {
            double a0, a1, a2, a3, mu2;

            mu2 = mu * mu;
            a0 = -0.5 * y0 + 1.5 * y1 - 1.5 * y2 + 0.5 * y3;
            a1 = y0 - 2.5 * y1 + 2 * y2 - 0.5 * y3;
            a2 = -0.5 * y0 + 0.5 * y2;
            a3 = y1;

            return a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3;
        }

        //Tension: 1 is high, 0 normal, -1 is low
        //Bias: 0 is even,
        //      positive is towards first segment,
        //      negative towards the other
        public static double Hermite(double y0, double y1, double y2, double y3, double mu, double tension, double bias)
        {
            double m0, m1, mu2, mu3;
            double a0, a1, a2, a3;

            mu2 = mu * mu;
            mu3 = mu2 * mu;
            m0 = (y1 - y0) * (1 + bias) * (1 - tension) / 2;
            m0 += (y2 - y1) * (1 - bias) * (1 - tension) / 2;
            m1 = (y2 - y1) * (1 + bias) * (1 - tension) / 2;
            m1 += (y3 - y2) * (1 - bias) * (1 - tension) / 2;
            a0 = 2 * mu3 - 3 * mu2 + 1;
            a1 = mu3 - 2 * mu2 + mu;
            a2 = mu3 - mu2;
            a3 = -2 * mu3 + 3 * mu2;

            return a0 * y1 + a1 * m0 + a2 * m1 + a3 * y2;
        }
    }
}
