using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Jaminet
{
    #region Import config model

    [Serializable]
    [XmlRoot("ImportConfiguration")]
    public class ImportConfiguration
    {
        [XmlAttribute]
        //[XmlElement(Order = 1)]
        public string Version { get; set; }

        [XmlArrayItem("Rule")]
        [XmlArray(Order = 1)]
        public List<Rule> Rules;
    }

    [Serializable]
    public class Rule
    {
        [XmlElement(Order = 1)]
        public string RuleType { get; set; }

        [XmlElement(Order = 2)]
        public string Element { get; set; }

        [XmlElement(Order = 3)]
        public string NewValue { get; set; }

        [XmlArrayItem("Condition")]
        [XmlArray(Order = 4)]
        public List<RuleCondition> Conditions;
    }

    [Serializable]
    public class RuleCondition
    {
        [XmlAttribute]
        //[XmlElement(Order = 1, IsNullable = true)]
        public int Order { get; set; }

        [XmlAttribute]
        //[XmlElement(Order = 2)]
        public string NextCondition { get; set; }

        [XmlElement(Order = 1)]
        public string Element { get; set; }

        [XmlElement(Order = 2)]
        public string Operator { get; set; }

        [XmlElement(Order = 3)]
        public string Value { get; set; }
    }
    #endregion
}
