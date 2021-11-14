using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testExpand
{
    public class TimeStepChanger
    {
        public enum InterpolationModes
        {
            Previous, //get the value of the previous time
            Next, //get the value of the next time 
            Nearest, //get the value of the nearest time
            Linear, //interpolate linearly
        }

        public TimeStepChanger(double targetTimeStep, double targetStartTime, InterpolationModes interpolationMode)
        {
            ExportedTimeStep = targetTimeStep; ExportedStartTime = targetStartTime;
            InterpolationMode = interpolationMode;
        }

        public TimeStepChanger() { }


        public double[] OriginalTimes;

        public double[] OriginalValues;

        public double ExportedTimeStep;

        public double ExportedStartTime;

        public double[] ExportedTimes;
        public double[] ExportedValues;

        public InterpolationModes InterpolationMode;

        public void ChangeTimeStep()
        {
            var indices = GetInterpolatedArithmeticSequenceIndices(OriginalTimes, ExportedStartTime, ExportedTimeStep);

            int count = indices.Length;

            ExportedTimes = new double[count];
            ExportedValues = new double[count];

            for (int j = 0; j < count; j++)
                ExportedTimes[j] = ExportedStartTime + indices[j].n * ExportedTimeStep;

            switch (InterpolationMode)
            {
                case InterpolationModes.Previous: //tested
                    for (int j = 0; j < count; j++)
                        ExportedValues[j] = OriginalValues[indices[j].i];
                        break;

                case InterpolationModes.Next: //tested
                    for (int j = 0; j < count; j++)
                        ExportedValues[j] = OriginalValues[indices[j].i+1];
                        break;
                case InterpolationModes.Nearest: //tested
                        for (int j = 0; j < count; j++)
                        {
                            bool previous = ExportedTimes[j] - OriginalTimes[indices[j].i] <=  OriginalTimes[indices[j].i + 1]-ExportedTimes[j];
                            ExportedValues[j] = previous ? OriginalValues[indices[j].i] : OriginalValues[indices[j].i+1];
                        }
                        break;
                case InterpolationModes.Linear: //tested
                        for (int j = 0; j < count; j++)
                        {
                            double coefficient = ( OriginalValues[indices[j].i + 1]- OriginalValues[indices[j].i])/( OriginalTimes[indices[j].i + 1]- OriginalTimes[indices[j].i]);
                            ExportedValues[j] =  OriginalValues[indices[j].i] +   (ExportedTimes[j] - OriginalTimes[indices[j].i])*coefficient ;
                        }
                        break;
            }

        }

        internal struct InterpolatedArithmeticValuesIndices
        {
            public int n; //arithmetic sequence int n: where a(n)=a0+n*w
            public int i; //index of the largest SortedValues value where a(n)>=SortedValues(i)
            public override string ToString()
            {
                return string.Format("n={0}, i={1}", n, i);
            }
        }

        //tested
        //n can be negative if a0<sortedValues
        internal static InterpolatedArithmeticValuesIndices[] GetInterpolatedArithmeticSequenceIndices(double[] sortedValues, double a0, double w)
        {
            //an = a0+n*w (n=0,1,2,...)
            //so the need is to find the n's and the i of the sortedValues where is less than a(n)>=sortedValues(i)
            double smallestValue = sortedValues[0];
            double largestValue = sortedValues[sortedValues.Length - 1];

            //find the smallest n : a(n)>=smallestValue or a0+n*w>=smallestvalue or n>=(smallestvalue-a0)/w
            int smalln = (int)Math.Ceiling((smallestValue - a0) / w);
            //find the largest n : a(n)<=largestValue or a0+n*w<=largestvalue or n<=(largestvalue-a0)/w
            int bign = (int)Math.Floor((largestValue - a0) / w);
            //so all n's are between the smalln and bign (included)
            int count = bign - smalln + 1;

            InterpolatedArithmeticValuesIndices[] indices = new InterpolatedArithmeticValuesIndices[count];
            //get all the n's of the arithmetic sequence
            for (int i = 0; i < count; i++) indices[i].n = i + smalln;

            //now assign sortedValues index to n's
            //find the largest sortedValue that is smaller than a0+w*n
            int startSortedIndex = sortedValues.Length - 2;

            for (int iArithmetic = count - 1; iArithmetic >= 0; iArithmetic--)
                for (int iSorted = startSortedIndex; iSorted >= 0; iSorted--)
                    if (sortedValues[iSorted] <= a0 + indices[iArithmetic].n * w)
                    {
                        startSortedIndex = indices[iArithmetic].i = iSorted;
                        break;
                    }

            return indices;
        }

    }
}
