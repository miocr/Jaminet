using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Linq;
using log4net;



namespace Jaminet
{

    public class Supplier
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Supplier));

        #region Protected Constants
        protected const string dataFolder = @"./Data";
        protected const string categoryWLfileName = "categories-WL";
        protected const string categoryBLfileName = "categories-BL";
        protected const string productWLfileName = "products-WL";
        protected const string productBLfileName = "products-BL";
        protected const string importConfigFileName = "import-config";
        protected const string feedFileName = "feed-original";
        protected const string feedProcessedFileName = "feed-processed";
        protected const string extParametersFileName = "ext-products-parameters";

        StringComparison scomp = StringComparison.CurrentCulture;

        #endregion

        #region Protected Properties

        protected XElement Feed { get; set; }
        protected XElement FeedProcessed { get; set; }

        protected List<string> CategoryWhiteList { get; set; }
        protected List<string> CategoryBlackList { get; set; }
        protected List<string> ProductWhiteList { get; set; }
        protected List<string> ProductBlackList { get; set; }
        protected ImportConfiguration ImportConfig { get; set; }
        protected SupplierSettings SupplierSettings { get; set; }

        #endregion

        #region Private Properties
        private GoogleDriveAPI gd;
        #endregion

        public Supplier()
        {
            log.Info("Supplier created");
        }

        /// <summary>
        /// Download and save feed to file
        /// </summary>
        public virtual void GetAndSaveFeed()
        {
            Downloader downloader = new Downloader(SupplierSettings.FeedUrlLogin, SupplierSettings.FeedUrlPassword);
            long content = downloader.DownloadFile(SupplierSettings.FeedUrl, FullFileName(feedFileName, "xml"));
        }

        /// <summary>
        /// Loads feed from file
        /// </summary>
        /// <returns>The feed.</returns>
        public virtual XElement LoadFeed()
        {
            Feed = null;
            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Loading feed from file ");
                Console.ResetColor();
                Console.WriteLine("'{0}'", FullFileName(feedFileName, "xml"));
                Console.WriteLine();

                log.InfoFormat("Loading feed from file:{0}", FullFileName(feedFileName, "xml"));

                using (FileStream fs = new FileStream(FullFileName(feedFileName, "xml"), FileMode.Open, FileAccess.Read))
                {
                    Feed = XElement.Load(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);

                log.ErrorFormat("Load Feed Exception {0}",ex.Message);

            }
            return Feed;
        }

        /// <summary>
        /// Saves feed to file
        /// </summary>
        /// <param name="isProcessed">If set to <c>true</c> save to "processed" file</param>
        public virtual void SaveFeed(bool isProcessed = true)
        {
            try
            {
                if (isProcessed)
                {
                    if (FeedProcessed != null)
                    {
                        //using (FileStream fs = File.Create(FullFileName(feedProcessedFileName, "xml")))
                        using (FileStream fs = new FileStream(FullFileName(feedProcessedFileName, "xml"), FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            FeedProcessed.Save(fs);
                        }
                    }
                    else
                    {
                        Console.WriteLine("SaveFeed error - FeedProcessed is empty");
                    }
                }
                else
                {
                    if (Feed != null)
                    {
                        using (FileStream fs = new FileStream(FullFileName(feedProcessedFileName, "xml"), FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            Feed.Save(fs);
                        }
                    }
                    else
                    {
                        Console.WriteLine("SaveFeed error - Feed is empty");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveFeed Exception: {0}", ex.Message);
            }
        }

        public virtual XElement LoadHeurekaProductsParameters()
        {
            XElement extParameters = null;
            if (File.Exists(FullFileName(extParametersFileName, "xml")))
            {
                try
                {
                    using (FileStream fs = new FileStream(FullFileName(extParametersFileName, "xml"), FileMode.Open, FileAccess.Read))
                    {
                        extParameters = XElement.Load(fs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("LoadHeurekaProductsParameters Exception: {0}", ex.Message);
                    log.ErrorFormat("LoadHeurekaProductsParameters Exception: {0}", ex.Message);
                }
            }
            return extParameters;
        }

        public virtual void SaveHeurekaProductsParameters(XElement extParameters)
        {
            if (extParameters == null)
                return;

            XDocument outDoc = new XDocument();
            outDoc.Add(extParameters);

            try
            {
                using (FileStream fs = new FileStream(FullFileName(extParametersFileName, "xml"),
                    FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    outDoc.Save(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveHeurekaProductsParameters Exception: {0}", ex.Message);
                log.ErrorFormat("SaveHeurekaProductsParameters Exception: {0}", ex.Message);
            }
        }

        public virtual void ReadImportConfiguration()
        {
            if (gd == null)
                gd = new GoogleDriveAPI();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Reading import configuration for supplier '{0}'", SupplierSettings.SupplierCode);
            Console.ResetColor();
            foreach (FeedImportSetting importSetting in SupplierSettings.FeedImportSettings)
            {
                if (importSetting.Name == importConfigFileName)
                {
                    // Konfigurace - pravdila
                    ReadImportConfigRulesFromGD(importSetting);
                }
                else if (importSetting.Name.Contains("WL"))
                {
                    ReadImportConfigWlBlFromGD(importSetting);
                }
                else if (importSetting.Name.Contains("BL"))
                {
                    ReadImportConfigWlBlFromGD(importSetting);
                }

            }
            Console.WriteLine();
        }

        public virtual void ProcessFeed()
        {
            if (CategoryBlackList == null || CategoryWhiteList == null ||
                ProductWhiteList == null || ProductBlackList == null)
            {
                Console.WriteLine("Error - incomplete configuration.");
                return;
            }

            LoadFeed();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Processing feed by configuration...");

            bool enabled;

            FeedProcessed = new XElement("SHOP");

            int enabledCount = 0;
            int disabledByListsCount = 0;
            int disabledByRuleCount = 0;
            int totalCount = 0;

            foreach (XElement origItem in Feed.Descendants("SHOPITEM"))
            {
#if DEBUG
                string itemCode = null;
                if (origItem.Element("CODE") != null)
                    itemCode = origItem.Element("CODE").Value;
#endif

                totalCount++;

                // zpracujeme pravidla 
                enabled = ProcessItemByRules(origItem);

                // polozku zakazanou nekterym z pravidel uz nemuze povolit ani WhiteList
                if (enabled == false)
                {
                    disabledByRuleCount++;
                }
                else
                {
                    // polozku NEzakazanou pravidly zpracujeme podle Black/WhiteListu
                    // mohou ji zakazat
                    enabled = ChechProductByWBList(origItem);
                    if (enabled)
                    {
                        enabledCount++;
                        FeedProcessed.Add(origItem);
                    }
                    else
                    {
                        disabledByListsCount++;
                    }
                }
            }
            Console.WriteLine("finished !");
            Console.WriteLine("Items in original feed: {0}", totalCount);
            Console.WriteLine("Items in processed feed: {0}", enabledCount);
            Console.WriteLine("* disabled items by rules: {0}", disabledByRuleCount);
            Console.WriteLine("* disabled items by black lists: {0}", disabledByListsCount);
            Console.WriteLine();
            Console.ResetColor();
        }

        /// <summary>
        /// Merges the feed with grabbed parameters from external source
        /// </summary>
        /// <param name="extParamaters">Ext paramaters.</param>
        public virtual void MergeFeedWithExtParameters(XElement extParamaters)
        {
            if (Feed == null || extParamaters == null)
                return;


            List<string> grabbedExtParametersProductCodes = new List<string>();
            foreach (XElement extItemParameters in extParamaters.Descendants("SHOPITEM"))
            {
                grabbedExtParametersProductCodes.Add(extItemParameters.Attribute("CODE").Value);
            }

            string supplierProductCode;

            int itemCounter = 0;
            bool itemEnabled;
            foreach (XElement origItem in Feed.Descendants("SHOPITEM"))
            {
                supplierProductCode = origItem.Element("CODE").Value;

                itemEnabled = ChechProductByWBList(origItem);
                if (!itemEnabled)
                    continue;

                if (!grabbedExtParametersProductCodes.Contains(supplierProductCode))
                    continue;

                //if (supplierProductCode == "#END#")
                //    break;

                Console.Write("{0} > Mergin parameters for item code: {1}",
                    (itemCounter++).ToString("000000"), supplierProductCode.PadRight(10));

                int origParamsCount = 0;
                try
                {

                    XElement extParametersItem =
                        extParamaters.Descendants("SHOPITEM")
                        .Where(i => i.Attribute("CODE").Value == supplierProductCode).Single();

                    XElement origParams = origItem.Element("INFORMATION_PARAMETERS");

                    if (origParams == null)
                    {
                        origParams = new XElement("INFORMATION_PARAMETERS");
                        origItem.Add(origParams);
                    }

                    origParamsCount = origParams.Descendants("INFORMATION_PARAMETER").Count();
                    int mergedParamsCount = 0;
                    foreach (XElement extParameter in extParametersItem.Descendants("INFORMATION_PARAMETER"))
                    {
                        origParams.Add(extParameter);
                        mergedParamsCount++;
                    }

                    Console.WriteLine("... merged {0}, total {1} parameters", mergedParamsCount, origParamsCount);

                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine();
                }
            }
        }

        public virtual XElement GetHeurekaProductsParameters(bool onlyNew = false)
        {
            XElement result = null;

            if (Feed == null)
                LoadFeed();

            if (Feed == null)
                return null;

            Heureka heureka = new Heureka(this);

            List<string> existsExtParametrsList = new List<string>();
            XElement existsExtParameters = null;
            if (onlyNew)
            {
                existsExtParameters = LoadHeurekaProductsParameters();
                if (existsExtParameters != null)
                {
                    foreach (XElement extParamater in existsExtParameters.Descendants("SHOPITEM"))
                    {
                        existsExtParametrsList.Add(extParamater.Attribute("CODE").Value);
                    }
                }
            }

            string supplierCode;
            foreach (XElement inShopItem in Feed.Descendants("SHOPITEM"))
            {
                supplierCode = inShopItem.Element("CODE")?.Value;

                // pro DEBUG
                if (supplierCode == "#END#")
                    break;

                if ((supplierCode == null) || (onlyNew && existsExtParametrsList.Contains(supplierCode)))
                    continue;

                heureka.SearchList.Add(
                    new Heureka.SearchObject
                    {
                        SupplierCode = supplierCode,
                        EAN = inShopItem.Element("EAN")?.Value,
                        PN = inShopItem.Element("PART_NUMBER")?.Value,
                        Manufacturer = inShopItem.Element("MANUFACTURER")?.Value
                    }
                );
            }

            result = heureka.GetProductsParameters();

            if (onlyNew && existsExtParameters != null)
            {

                XElement outShopItems = new XElement("SHOP");
                outShopItems.Add(existsExtParameters.Descendants("SHOPITEM"));
                outShopItems.Add(result.Descendants("SHOPITEM"));
                result = outShopItems;
            }

            return result;
        }

        public virtual void SaveConfig(ImportConfiguration ic)
        {
            try
            {
                using (FileStream fsw = File.OpenWrite(@"Data/test-out.xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ImportConfiguration));
                    serializer.Serialize(fsw, ic);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("XML config serialize exception: {0}", ex.Message);
                log.ErrorFormat("XML config serialize exception: {0}", ex.Message);
            }
        }


        //---------------------------------------------------------------------------------

        protected void Initialize(SupplierSettings supplierSetiings)
        {
            SupplierSettings = supplierSetiings;
        }

        public string FullFileName(string fileName, string extension)
        {
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            string fullPathFileName = Path.Combine(dataFolder, String.Concat(SupplierSettings.SupplierCode, "-", fileName, ".", extension));
            return fullPathFileName;
        }

        private void ReadImportConfigRulesFromGD(FeedImportSetting setting)
        {
            gd.DownloadFile(setting.GoogleDriveFileId, FullFileName(setting.Name, "xml"), setting.MimeType);
            ImportConfig = LoadConfig();
        }

        private ImportConfiguration LoadConfig()
        {
            ImportConfiguration ic = null;
            try
            {
                using (FileStream fsr = File.OpenRead(FullFileName(importConfigFileName, "xml")))
                {
                    #region Oprava BOM (potrebuje se pouze pro JSON deserialize)
                    // JSON musi byt UTF-8 !!!
                    // https://cs.wikipedia.org/wiki/Byte_order_mark
                    //if (fsr.ReadByte() == 0xEF)
                    //    // preskocit BOM ! JSON deserializer umi jen UTF-8 bez BOM
                    //    fsr.Position = 3; 
                    //else
                    //    // neni B0M, cteme od 0
                    //    fsr.Position = 0;
                    #endregion

                    XmlSerializer serializer = new XmlSerializer(typeof(ImportConfiguration));
                    ic = (ImportConfiguration)serializer.Deserialize(fsr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("XML config deserialize exception: {0}", ex.Message);
                log.ErrorFormat("XML config deserialize exception: {0}", ex.Message);
            }
            return ic;
        }

        private void ReadImportConfigWlBlFromGD(FeedImportSetting setting)
        {
            // White/Black listy
            string line;
            List<string> list = new List<string>();

            gd.DownloadFile(setting.GoogleDriveFileId, FullFileName(setting.Name, "txt"), setting.MimeType);

            try
            {
                using (TextReader tr = File.OpenText(FullFileName(setting.Name, "txt")))
                {
                    while ((line = tr.ReadLine()) != null)
                    {
                        if (!list.Contains(line))
                            list.Add(line);
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("ReadImportConfigurationFromGD Exception:{0}", exc.Message);
                log.ErrorFormat("ReadImportConfigurationFromGD Exception:{0}", exc.Message);
            }

            switch (setting.Name)
            {
                case categoryBLfileName:
                    CategoryBlackList = list;
                    break;
                case categoryWLfileName:
                    CategoryWhiteList = list;
                    break;
                case productBLfileName:
                    ProductBlackList = list;
                    break;
                case productWLfileName:
                    ProductWhiteList = list;
                    break;
                default:
                    break;
            }

        }

        private bool ChechProductByWBList(XElement item)
        {

            bool enabled = true;
            foreach (XElement itemCategory in item.Descendants("CATEGORY"))
            {
                enabled = true;
                foreach (string categoryBL in CategoryBlackList)
                {
                    if (itemCategory.Value.StartsWith(categoryBL,
                        StringComparison.CurrentCultureIgnoreCase))
                    {
                        enabled = false;
                        // prvni zakaz staci, dalsi neoverujeme
                        break;
                    }
                }


                // WhiteList overime jen pokud je polozka zakazana podle BL
                // protoze povoleni podle WL ma vyssi prioritu nez BL. 
                // Pokud neni zakazana podle BL, je zbytecne ji znovu povolovat
                if (enabled == false)
                {
                    foreach (string categoryWL in CategoryWhiteList)
                    {
                        if (itemCategory.Value.StartsWith(categoryWL,
                                StringComparison.CurrentCultureIgnoreCase))
                        {
                            enabled = true;
                            // prvni povoleni staci, dalsi neoverujeme
                            break;
                        }
                    }
                }
            }

            string code = item.Element("CODE").Value;
            if (!String.IsNullOrEmpty(code))
            {
                if (ProductBlackList.Contains(code))
                    enabled = false;

                // WhiteList overime jen pokud je polozka zakazana
                // Pokud je povolena, je zbytecne ji znovu povolovat
                if (enabled == false)
                {
                    if (ProductWhiteList.Contains(code))
                        enabled = true;
                }
            }

            return enabled;
        }


        /// <summary>
        /// Zpracuje všechna pravidla konfigurace import pro danou položky
        /// </summary>
        /// <param name="item">položka feedu</param>
        /// <returns>
        /// Pokud jsou pravidla pro zákaz/povolení položky, vrací výsledek podmínek. Pro 
        /// jiná pravidla vrací true jako default.
        /// </returns>
        private bool ProcessItemByRules(XElement item)
        {
            // nastavuje se pouze pro pravdila typu enable/disable item
            // pro ostatn� pravidla (nap�.zm�na hodnoty) je v�dy true 
            bool isEnabbled = true;

            foreach (Rule rule in ImportConfig.Rules)
            {
                switch (rule.RuleType.ToLower())
                {
                    case "enable-disable-item":

                        if (rule.Conditions == null)
                            break;

                        for (int i = 0; i < rule.Conditions.Count; i++)
                        {
                            RuleCondition condition = rule.Conditions[i];
                            XElement conditionElement = item.XPathSelectElement(condition.Element);
                            if (conditionElement == null)
                            {
                                // Pokud polozka nema element potrebny k vyhodoceni podmky, 
                                // povazujeme pro tento typ pravidla vyzledek za TRUE
                                isEnabbled = true;
                            }
                            else
                            {
                                isEnabbled = EvaluateRule(condition.Operator, conditionElement.Value, condition.Value, true);
                                // Toto pravidlo musi mit jednu pomdminku
                                // TODO - ovetrit a vyvolat vyjimky pro pripad vice pravidel u stock-amount
                            }
                        }

                        // splnena podminka ma pro tuto akci ma "negativni" ucinek a obratime ji (polozka zakazana)
                        isEnabbled = !isEnabbled;
                        break;

                    case "change-item-value":
                        string previousConditionOperator = null;
                        bool tmpCondResult = true;
                        bool tmpAllCondsResult = tmpCondResult;

                        // Vyhodnotime vsechny podminky
                        for (int i = 0; i < rule.Conditions.Count; i++)
                        {
                            RuleCondition condition = rule.Conditions[i];
                            XElement conditionElement = item.XPathSelectElement(condition.Element);
                            if (conditionElement == null)
                            {
                                // Pokud polozka nema element potrebny k vyhodoceni podmky
                                // povzaujeme pro tento typ pravidla za FALSE
                                tmpCondResult = false;
                            }
                            else
                            {
                                tmpCondResult = EvaluateRule(condition.Operator, conditionElement.Value, condition.Value, false);
                            }

                            if (previousConditionOperator != null)
                            {
                                // zpracujeme operator z predchozi a soucasne podminky
                                switch (previousConditionOperator)
                                {
                                    case null:
                                        // prvni podminka, ale existuje dalsi
                                        tmpAllCondsResult = isEnabbled;
                                        break;
                                    case "and":
                                        tmpAllCondsResult = tmpAllCondsResult && isEnabbled;
                                        break;
                                    case "or":
                                        tmpAllCondsResult = tmpAllCondsResult || isEnabbled;
                                        break;
                                    default:
                                        throw new Exception(
                                            String.Format("Unknown condition operator {0} for rule {1}",
                                            previousConditionOperator, rule.RuleType));
                                }
                            }

                            if (condition.NextCondition == null)
                                // byla to posledni podminka
                                break;
                            else
                            {
                                // bude dalsi podminka
                                previousConditionOperator = condition.NextCondition;
                                // v pripade vice podminek je po zpracovani prvni podminky
                                // celkovy vysledek jako podle vysledku prvni podminky
                                if (i == 0)
                                    tmpAllCondsResult = tmpCondResult;
                            }
                        }

                        // Pokud jsou vsechny podminky splnene, zmenime hodnotu podle pravidla
                        if (tmpAllCondsResult)
                        {
                            if (rule.NewValue != null)
                            {
                                XElement valueForChange = item.XPathSelectElement(rule.Element);
                                if (valueForChange != null)
                                    valueForChange.Value = rule.NewValue;
                            }
                        }

                        break;

                    default:
                        break;
                }
            }
            return isEnabbled;
        }

        /// <summary>
        /// Metoda prorovn� hodnoty 1/2, podle oper�toru ur��, zda se porovnaj� jako ��sla, nebo text
        /// </summary>
        /// <param name="conditionOperator">"Typ operatoru vetsi, mmensi, == !=, equals, contains, startwith, endwith)"</param>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="unresolved">V�sledek, pokud se nepoda�� porovnat</param>
        /// <returns></returns>
        private bool EvaluateRule(string conditionOperator, string value1, string value2, bool unresolved)
        {
            bool result = unresolved;

            switch (conditionOperator.ToLower())
            {
                case "equals":
                case "contains":
                case "startwith":
                case "endwith":
                    result = EvaluateTextRule(conditionOperator, value1, value2, unresolved);
                    break;
                case "==":
                case "!=":
                case "<":
                case ">":
                    result = EvaluateNumericRule(conditionOperator, value1, value2, unresolved);
                    break;
                default:
                    break; ;
            }
            return result;
        }

        /// <summary>
        /// Metoda prorovn� textov� hodnoty 1/2 jako ��sla typu decimal 
        /// </summary>
        /// <param name="conditionOperator">Typ operatoru</param>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="unresolved">V�sledek, pokud se nepoda�� provonat</param>
        /// <returns></returns>
        private bool EvaluateNumericRule(string conditionOperator, string value1, string value2, bool unresolved)
        {
            decimal? number1 = DecimalParseInvariant(value1);
            decimal? number2 = DecimalParseInvariant(value2);
            bool result = unresolved;

            if (number1.HasValue && number2.HasValue)
            {
                switch (conditionOperator)
                {
                    case "<":
                        result = number1.Value < number2.Value;
                        break;
                    case ">":
                        result = number1.Value > number2.Value;
                        break;
                    case "==":
                        result = number1.Value == number2.Value;
                        break;
                    case "!=":
                        result = number1.Value != number2.Value;
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        /// <summary>
        /// Metoda prorovn� textov� hodnoty 1/2 jako typ text 
        /// </summary>
        /// <param name="conditionOperator">Typ oper�toru (equals, contains, startwith, endwith)</param>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="unresolved">V�sledek, pokud se nepoda�� porovnat</param>
        /// <returns></returns>
        private bool EvaluateTextRule(string conditionOperator, string value1, string value2, bool unresolved)
        {
            bool result = unresolved;

            switch (conditionOperator.ToLower())
            {
                case "equals":
                    result = (value1 == value2);
                    break;
                case "contains":
                    result = value1.Contains(value2);
                    break;
                case "startwith":
                    result = value1.StartsWith(value2, scomp);
                    break;
                case "endwith":
                    result = value1.EndsWith(value2, scomp);
                    break;
                default:
                    break;
            }
            return result;
        }

        /// <summary>
        /// P�evede text na desetinn� ��slo bez ohledu na typ odd�lova�e (.,)
        /// </summary>
        /// <param name="svalue"></param>
        /// <returns></returns>
        private decimal? DecimalParseInvariant(string svalue)
        {
            if (decimal.TryParse(svalue, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal dvalue))
            {
                return dvalue;
            }
            else
            {
                return null;
            }
        }

        private ImportConfiguration SampleData()
        {
            RuleCondition icrc1 = new RuleCondition
            {
                Element = "SHOP/SHOPITEM/STOCK/AMOUNT",
                Operator = "==",
                Value = "50",
                NextCondition = "and"
            };

            RuleCondition icrc2 = new RuleCondition
            {
                Element = "SHOP/SHOPITEM/CATEGORIES/CATEGORY",
                Operator = "Contains",
                Value = "Spot�ebn� materi�l &gt; Pap�r",
                NextCondition = null
            };

            Rule icr = new Rule
            {
                RuleType = "item-disable",
                Element = "element",
                NewValue = "100",
                Conditions = new List<RuleCondition>() { icrc1, icrc2 }
            };

            ImportConfiguration ic = new ImportConfiguration
            {
                Version = "1.0",
                Rules = new List<Rule>() { icr }
            };

            return ic;
        }



    }
}

