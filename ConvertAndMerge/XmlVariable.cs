using Paulus.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;

namespace ConvertMerge
{
    public class XmlVariable //στο VariableInfo θα έπρεπε να μπουν όλα αυτά!
    {
        //public static XmlVariable()
        //{
        //    typeDictionary = getTypeDictionary();
        //}

        private static Dictionary<string, Type> typeDictionary = getTypeDictionary();
        private static Dictionary<string, Type> getTypeDictionary()
        {
            Dictionary<string, Type> d = new Dictionary<string, Type>();
            d.Add("string", typeof(string));
            d.Add("integer", typeof(int));
            d.Add("datetime", typeof(DateTime));
            d.Add("double", typeof(double));
            return d;
        }

        public string OriginalName { get; set; }

        public string TranslatedName { get; set; }

        public string Group { get; set; }

        public Type Type { get; set; }

        public int ColumnIndex { get; set; }

        public string ColumnLetter { get; set; }

        public InterpolationMode Interpolation { get; set; }

        public static XmlVariable CreateFromXml(XmlElement v)
        {
            //<variable name="T_CHAN1" translatedname="T1" group="Temperatures" type="double" columnindex="2" columnletter="B" />
            string originalName = v.GetAttribute("name");
            string translatedName = v.GetAttribute("translatedname");
            if (string.IsNullOrWhiteSpace(translatedName)) translatedName = originalName;

            Type type = v.GetAttributeOrElementCustom("type", typeDictionary, typeof(string));

            InterpolationMode interpolationMode = InterpolationMode.Undefined;
            if (type == typeof(double) || type == typeof(decimal) || type == typeof(float))
                interpolationMode = v.GetAttributeOrElementCustom("interpolation", Interpolator.InterpolationDictionary, ExperimentManager.DefaultInterpolationMode);
            else if (type == typeof(string) || type == typeof(int) || type == typeof(long) || type == typeof(decimal) || type == typeof(bool))
                interpolationMode = v.GetAttributeOrElementCustom("interpolation", Interpolator.InterpolationDictionary, InterpolationMode.Previous);
            else
                throw new InvalidOperationException($"Invalid type for variable: {v.InnerText}.");

            return new XmlVariable()
            {
                OriginalName = originalName,
                TranslatedName = translatedName,
                //ignored attribute for the moment
                Group = v.GetAttribute("group"),
                Type = type,
                ColumnIndex = !string.IsNullOrWhiteSpace(v.GetAttribute("columnindex")) ? int.Parse(v.GetAttribute("columnindex")) : 0,
                ColumnLetter = v.GetAttribute("columnletter"),
                Interpolation = interpolationMode
            };
        }

        public static explicit operator XmlVariable(XmlElement v)
        {
            return CreateFromXml(v);
        }

        public static Dictionary<string, XmlVariable> CreateFromXml(XmlNodeList variables)
        {
            //<?xml version="1.0"?>
            //<?xml-stylesheet type="text/xsl" href="variables.xsl"?>
            //<variables>
            //  <!--<variable name="-" translatedname="Time [s]" group="General" type="datetime" columnindex="1" columnletter="A" />-->
            //  <variable

            //IEnumerable<XmlElement> xmlVariables = (d["variables"].GetElementsByTagName("variable")).Cast<XmlElement>();
            //XmlNodeList xmlVariables = d["variables"].SelectNodes("variable");

            Dictionary<string, XmlVariable> vars = new Dictionary<string, XmlVariable>();

            foreach (XmlElement el in variables)
            {
                string name = el.Attributes["name"].Value;
                if (vars.ContainsKey(name))
                    throw new InvalidOperationException($"'{name}' variable is defined more than once.");
                vars.Add(name, (XmlVariable)el);
            }
            return vars;
        }


        public static Dictionary<string, XmlVariable> CreateFromXml(string xmlPath)
        {
            XmlDocument d = new XmlDocument();
            d.Load(xmlPath);
            return CreateFromXml(d["variables"].SelectNodes("variable"));
        }
    }
}
