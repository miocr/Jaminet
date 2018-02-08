using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Jaminet
{

    [DataContract(Name = "root")]
    public class ImportConfiguration
    {
        [DataMember(Name = "version", Order = 1)]
        public string Version { get; set; }
        [DataMember(Name = "rules",Order = 2)]
        public List<ImportConfigurationRule> Rules;
    }

    [DataContract]
    public class ImportConfigurationRule
    {
        [DataMember(Name = "operation",Order = 1)]
        public string Operation { get; set; }
        [DataMember(Name = "element",Order = 2)]
        public string Element { get; set; }
        [DataMember(Name = "newValue",Order = 3)]
        public string NewValue { get; set; }
        [DataMember(Name = "conditions",Order = 4)]
        public List<ImportConfigurationRuleCondition> Conditions;
    }

    [DataContract]
    public class ImportConfigurationRuleCondition
    {
        [DataMember(Name = "element",Order = 1)]
        public string Element { get; set; }
        [DataMember(Name = "operator",Order = 2)]
        public string Operator { get; set; }
        [DataMember(Name = "value",Order = 3)]
        public string Value { get; set; }
        [DataMember(Name = "nextCondition",Order = 4)]
        public string NextCondition { get; set; }

    }

    public class FeedConfiguration
    {

        public FeedConfiguration()
        {
            /*
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            sett.RootName = "root";
            sett.UseSimpleDictionaryFormat = true;
            sett.KnownTypes =
                    new List<Type>() {
                        typeof(ImportConfiguration),
                        typeof(ImportConfigurationRule),
                        typeof(ImportConfigurationRuleCondition)};

            DataContractJsonSerializer des = new DataContractJsonSerializer(typeof(ImportConfiguration), settings);
            */


            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ImportConfiguration));

            ImportConfigurationRuleCondition icrc = new ImportConfigurationRuleCondition
            {
                Element = "element",
                Operator = "==",
                Value = "5",
                NextCondition = null
            };

            ImportConfigurationRule icr = new ImportConfigurationRule
            {
                Element = "element",
                NewValue = "100",
                Conditions = new List<ImportConfigurationRuleCondition>() { icrc }
            };

            ImportConfiguration ic = new ImportConfiguration
            {
                Version = "1.0",
                Rules = new List<ImportConfigurationRule>() { icr }
            };

            using (FileStream fsw = File.OpenWrite(@"Data/test.json"))
            {
                serializer.WriteObject(fsw, ic);
            }

            ImportConfiguration icx;
            using (FileStream fsr = File.OpenRead(@"Data/import-config-example.json"))
            {
                icx = (ImportConfiguration)serializer.ReadObject(fsr);
            }

            using (FileStream fsw = File.OpenWrite(@"Data/import-config-example-out.json"))
            {
                serializer.WriteObject(fsw, icx);
            }

        }
    }
}
