using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Paulus.Extensions;

using System.Diagnostics;

namespace testExpand
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("{0:0}", 1.23); //rounds to one digit

            //int[] c1 = GetIntegersBetween(1.2, 3.1);
            //int[] c2 = GetIntegersBetween(1.2, 4.5);
            //int[] c3 = GetIntegersBetween(1.2, 1.8);
            //int[] c4 = GetIntegersBetween(1.2, 2);
            //int[] c5 = GetIntegersBetween(1.2, 3);
            //int[] c6 = GetIntegersBetween(1, 2);
            //int[] c7 = GetIntegersBetween(1, 2.1);

            var c1 = TimeStepChanger.GetInterpolatedArithmeticSequenceIndices(new double[] { 1.1, 5 }, 0.1, 0.2);
            var c2 = TimeStepChanger.GetInterpolatedArithmeticSequenceIndices(new double[] { 0, 1.5, 3, 4.5 }, 0.1, 0.2);
           
            var c3 = TimeStepChanger.GetInterpolatedArithmeticSequenceIndices(new double[] { 1.1, 5 }, 0, 1);
            var c4 = TimeStepChanger.GetInterpolatedArithmeticSequenceIndices(new double[] { 0, 1.5, 3, 4.5 }, 0, 1);

            TimeStepChanger changer = new TimeStepChanger(1, 0, TimeStepChanger.InterpolationModes.Linear);
            changer.OriginalTimes = new double[] { 0, 1.5, 3, 4.5 };
            changer.OriginalValues = new double[] { 10, 20, 30, 40 };
            changer.ChangeTimeStep();

            changer.ExportedStartTime = 2;
            changer.ChangeTimeStep();


            Debugger.Break();
        }

        /// <summary>
        /// Retrieves all integer values between two double values. Works only for positive values.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        /// <example>
        /// int[] c1 = GetIntegersBetween(1.2, 3.1);
        /// int[] c2 = GetIntegersBetween(1.2, 4.5);
        /// int[] c3 = GetIntegersBetween(1.2, 1.8);
        /// int[] c4 = GetIntegersBetween(1.2, 2);
        /// int[] c5 = GetIntegersBetween(1.2, 3);
        /// int[] c6 = GetIntegersBetween(1, 2);
        /// int[] c7 = GetIntegersBetween(1, 2.1);
        /// </example>
        public static int[] GetIntegersBetween(double v1, double v2, bool excludeLastValueIfInteger = false)
        {
            int startInt = v1 > (int)v1 ? (int)v1 + 1 : (int)v1;
            int lastInt = v2 > (int)v2 ? (int)v2 : excludeLastValueIfInteger ? (int)v2 - 1 : (int)v2;

            int[] betweens;

            if (startInt == lastInt)
                betweens = new int[] { startInt };
            else
            {
                betweens = new int[lastInt - startInt + 1];
                for (int i = startInt; i <= lastInt; i++)
                    betweens[i - startInt] = i;
            }

            return betweens;
        }

    }
}
