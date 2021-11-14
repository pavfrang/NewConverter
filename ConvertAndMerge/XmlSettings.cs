using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;
using Paulus.IO;

namespace ConvertMerge
{
    internal static class XmlSettings
    {
        public static SyncModes getMergeMode(XmlElement e)
        {
            Dictionary<string, SyncModes> dic = new Dictionary<string, SyncModes>();
            dic.Add("keepall", SyncModes.KeepAll);
            dic.Add("crop", SyncModes.Crop);
            dic.Add("keepallstartcropend",SyncModes.KeepAllStartCropEnd);
            dic.Add("cropstartkeepallend",SyncModes.CropStartKeepAllEnd);

            return e.GetAttributeOrElementCustom("mergemode",dic,SyncModes.InvalidOrMissing);
        }


    }
}
