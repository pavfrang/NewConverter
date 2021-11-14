using System;
using System.Collections.Generic;

using System.Collections;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices; //Marshal

namespace ConvertMerge
{
    public class ComDisposer :Queue, IDisposable
    {
        public void Dispose()
        {
            //collect all objects to be disposed
            GC.Collect(); GC.WaitForPendingFinalizers();

            //the duplicate is needed only if VSTO are used
            GC.Collect(); GC.WaitForPendingFinalizers();

            while (base.Count > 0)
                Marshal.FinalReleaseComObject(base.Dequeue());
        }
    }
}
