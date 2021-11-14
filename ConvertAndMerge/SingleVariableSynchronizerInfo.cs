using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertMerge
{
    /// <summary>
    /// Contains variable synchronize information used by VariableMerger.
    /// </summary>
    internal class SingleVariableSynchronizerInfo
    {
        public SingleVariableSynchronizerInfo(VariableInfo v)
        {
            _variableInfo = v;
        }

        private VariableInfo _variableInfo;
        public VariableInfo VariableInfo { get { return _variableInfo; } set { _variableInfo = value; } }

        /// <summary>
        /// Index of the first element of the time/value lists after setting the time step. It is greater than 0 if there are values to omit. Set by SetMergeProperties(). Used by VariableMerger.
        /// </summary>
        public int StartIndex;

        /// <summary>
        /// Index of the last element of the time/value lists after setting the time step. Set by SetSync(). Used by VariableMerger.
        /// </summary>
        public int LastIndex;

        /// <summary>
        /// The number of the missing records from start. Set by SetSync().
        /// </summary>
        public int MissingRecordsFromStart;

        /// <summary>
        /// Current index of the time/value lists after setting the time step. Used by VariableMerger.writeToFile().
        /// </summary>
        public int SyncIndex;

        /// <summary>
        /// Used by VariableMerger before beginning synchronized file exporting. Called by VariableMerger.resetVariableIndices().
        /// </summary>
        /// <param name="mergeMode"></param>
        public void ResetSyncIndex(SyncModes mergeMode)
        {
            //this will increment each time. when it reaches zero, records appear to the file
            if (mergeMode.HasFlag(SyncModes.KeepEarliestStartTime))
                SyncIndex = -MissingRecordsFromStart;
            else if (mergeMode.HasFlag(SyncModes.KeepLatestStartTime))
                SyncIndex = StartIndex;
        }

        /// <summary>
        /// Increments the SynchronizeIndex each time a new record is added. Used by VariableMerger.writeToFile().
        /// </summary>
        /// <returns></returns>
        public int IncrementSyncIndex() { return ++SyncIndex; }

        /// <summary>
        /// Called by VariableMerger.calculateStartEndVariableIndices() in the Merge function.
        /// </summary>
        /// <param name="startSyncAbsoluteTime"></param>
        /// <param name="endSyncAbsoluteTime"></param>
        public void SetSync(DateTime startSyncAbsoluteTime, DateTime endSyncAbsoluteTime)
        {
            if (!_variableInfo.ContainsData)
            {
                //_startIndexAfterMerge = _missingRecordsAfterMerge = -1;
                return;
            }

            #region Beginning
            DateTime t0 = _variableInfo.StartAbsoluteTimeAfterTimeStepChange;

            //if the first time is greater than the startExportingTime then there are missing records
            if (t0 >= startSyncAbsoluteTime)
            {
                StartIndex = 0;
                MissingRecordsFromStart = (int)Math.Floor((t0 - startSyncAbsoluteTime).TotalSeconds / _variableInfo.ExportTimeStepInSeconds);
            }
            else //there are rows to omit
            {
                StartIndex = (int)Math.Floor((startSyncAbsoluteTime - t0).TotalSeconds / _variableInfo.ExportTimeStepInSeconds);
                MissingRecordsFromStart = 0;
            }
            #endregion

            #region End
            if (_variableInfo.EndAbsoluteTimeAfterTimeStepChange >= endSyncAbsoluteTime)
                LastIndex = (int)Math.Floor((endSyncAbsoluteTime - t0).TotalSeconds / _variableInfo.ExportTimeStepInSeconds);
            else
                LastIndex = (int)Math.Floor((_variableInfo.EndAbsoluteTimeAfterTimeStepChange - t0).TotalSeconds / _variableInfo.ExportTimeStepInSeconds);
            #endregion
        }

    }
}
