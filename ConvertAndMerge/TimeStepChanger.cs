using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using Paulus.Collections;

namespace ConvertMerge
{
    //used by TImeStepChanger

    //where T is the type of the value
    public class TimeStepChanger<T>
    {


        #region Constructors
        public TimeStepChanger() { }

        public TimeStepChanger(double exportStartRelativeTime, double exportTimeStep, InterpolationMode interpolationMode)
        {
            ExportTimeStep = exportTimeStep; ExportStartRelativeTime = exportStartRelativeTime;
            InterpolationMode = interpolationMode;
        }

        public TimeStepChanger(double targetStartTime, double targetTimeStep, InterpolationMode interpolationMode, IEnumerable<double> originalTimes, IEnumerable<T> originalValues, bool exportNow)
            : this(targetStartTime, targetTimeStep, interpolationMode)
        {
            ImportedTimes = originalTimes.ToArray();

            ImportedValues = originalValues.ToArray();

            if (exportNow) ExportWithNewTimeStep();
        }
        #endregion



        #region Properties
        //settings
        public InterpolationMode InterpolationMode;
        public double ExportTimeStep;
        public double ExportStartRelativeTime;


        
        //input arrays
        public double[] _importedTimes;
        public double[] ImportedTimes
        {
            get { return _importedTimes; }
            set
            {
                if (!value.IsSortedInIncreasingOrder(true))
                    throw new ArgumentOutOfRangeException("ImportedTimes",value.GetIndicesOfNonIncreasingOrder(), "The original times need to be sorted.");
                else
                    _importedTimes = value;
            }
        }

        public double[] ExportedTimes;
        public T[] ImportedValues, ExportedValues;

        #endregion

        public void ExportWithNewTimeStep()
        {
            var indices = GetInterpolatedArithmeticSequenceIndices(_importedTimes, ExportStartRelativeTime, ExportTimeStep);

            int count = indices.Length;

            ExportedTimes = new double[count];
            ExportedValues = new T[count];

            for (int j = 0; j < count; j++)
                ExportedTimes[j] = ExportStartRelativeTime + indices[j].n * ExportTimeStep;

            switch (InterpolationMode)
            {
                case InterpolationMode.Previous: //tested
                    for (int j = 0; j < count; j++)
                        ExportedValues[j] = ImportedValues[indices[j].i];
                    break;

                case InterpolationMode.Next: //tested
                    for (int j = 0; j < count; j++)
                        ExportedValues[j] = ImportedValues[indices[j].i + 1];
                    break;
                case InterpolationMode.Nearest: //tested
                    for (int j = 0; j < count; j++)
                    {
                        bool previous = ExportedTimes[j] - _importedTimes[indices[j].i] <= _importedTimes[indices[j].i + 1] - ExportedTimes[j];
                        ExportedValues[j] = previous ? ImportedValues[indices[j].i] : ImportedValues[indices[j].i + 1];
                    }
                    break;
                case InterpolationMode.Linear: //allowed only if T is numeric!
                    for (int j = 0; j < count; j++)
                    {
                        dynamic v2 = ImportedValues[indices[j].i + 1], v1 = ImportedValues[indices[j].i];
                        double coefficient = ((double)v2 - (double)v1) / (_importedTimes[indices[j].i + 1] - _importedTimes[indices[j].i]);
                        //needs to be dynamic to allow runtime cast to T
                        dynamic offset = (double)v1 + (ExportedTimes[j] - _importedTimes[indices[j].i]) * coefficient;
                        ExportedValues[j] = offset;
                    }
                    break;
                //case InterpolationMode.Cubic: //allowed only if T is numeric!
                //    for (int j = 0; j < count; j++)
                //    {
                //        dynamic v2 = ImportedValues[indices[j].i + 1], v1 = ImportedValues[indices[j].i];
                //          Interpolator.Cubic()
                //      double coefficient = ((double)v2 - (double)v1) / (_importedTimes[indices[j].i + 1] - _importedTimes[indices[j].i]);
                //        //needs to be dynamic to allow runtime cast to T
                //        dynamic offset = (double)v1 + (ExportedTimes[j] - _importedTimes[indices[j].i]) * coefficient;
                //        ExportedValues[j] = offset;
                //    }
                //    break;
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

        internal static InterpolatedArithmeticValuesIndices[] GetInterpolatedArithmeticSequenceIndices(double[] sortedValues, double a0, double w)
        {
            //tested
            //n can be negative if a0<sortedValues

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
