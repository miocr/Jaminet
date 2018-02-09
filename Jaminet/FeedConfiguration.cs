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

        private DataContractJsonSerializer serializer;

        public FeedConfiguration()
        {
            serializer = new DataContractJsonSerializer(typeof(ImportConfiguration));
        }

        public void LoadConfig(string fileName)
        {
            ImportConfiguration icx;
            // JSON musí být UTF-8 !!!
            // https://cs.wikipedia.org/wiki/Byte_order_mark
            using (FileStream fsr = File.OpenRead(fileName))
            {
                if (fsr.ReadByte() == 0xEF)
                    // přeskočit BOM ! JSON deserializer umi jen UTF-8 bez BOM
                    fsr.Position = 3; 
                else
                    // není B0M, čteme od 0
                    fsr.Position = 0;  
                
                icx = (ImportConfiguration)serializer.ReadObject(fsr);
            }
        }

        public void SaveConfig()
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



            //using (FileStream fsw = File.OpenWrite(@"Data/import-config-example-out.json"))
            //{
            //    serializer.WriteObject(fsw, icx);
            //}

        }
    }
}
