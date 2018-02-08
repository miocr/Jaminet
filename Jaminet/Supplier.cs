using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Net.Http;

namespace Jaminet
{
    public class SupplierSettings
    {
        public string SupplierCode { get; set; }
        public string FeedUrl { get; set; }
        public string FeedUrlLogin { get; set; }
        public string FeedUrlPassword { get; set; }
        public List<FeedImportSetting> FeedImportSettings { get; set; }

        public SupplierSettings()
        {
            FeedImportSettings = new List<FeedImportSetting>();
        }
    }

    public class FeedImportSetting
    {
        public string Name { get; set; }
        public string GoogleDriveFileId { get; set; }
        public string MimeType { get; set; }
    }

    public class Supplier
    {
        private const string dataFolder = @"./Data";

        public const string categoryWLfileName = "categories-WL";
        public const string categoryBLfileName = "categories-BL";
        public const string productWLfileName = "products-WL";
        public const string productBLfileName = "products-BL";
        public const string importConfigFileName = "import-config";

        public const string feedFileName = "feed";
        public const string feedMergedFileName = "feed-merged";

        public const string ExtParametersFileName = "ext-products-parameters";

        protected SupplierSettings SupplierSettings { get; set; }

        public XElement Feed { get; set; }
        public XElement FeedFiltered { get; set; }

        public List<string> CategoryWhiteList { get; set; }
        public List<string> CategoryBlackList { get; set; }

        public List<string> ProductWhiteList { get; set; }
        public List<string> ProductBlackList { get; set; }

        public Supplier()
        {
        }

        protected void Initialize(SupplierSettings supplierSetiings)
        {
            SupplierSettings = supplierSetiings;
        }


        public void GetAndSaveFeed()
        {
            Downloader downloader = new Downloader(SupplierSettings.FeedUrlLogin, SupplierSettings.FeedUrlPassword);
            long content = downloader.DownloadFile(SupplierSettings.FeedUrl, FullFileName(feedFileName, "xml"));
        }

        public virtual XElement LoadFeed()
        {
            Feed = null;
            try
            {
                Console.WriteLine("Loading feed from file '{0}'", FullFileName(feedFileName, "xml"));
                using (FileStream fs = new FileStream(FullFileName(feedFileName, "xml"), FileMode.Open, FileAccess.Read))
                {
                    Feed = XElement.Load(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
            }
            return Feed;
        }



        public virtual void SaveFeed(bool isFeedMerged)
        {
            if (Feed != null)
            {
                try
                {
                    string fileName = isFeedMerged ? feedMergedFileName : feedFileName;
                    using (FileStream fs = new FileStream(FullFileName(fileName, "xml"),
                        FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        Feed.Save(fs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SaveFeed Exception: {0}", ex.Message);
                }
            }
            else
            {
                Console.WriteLine("SaveFeed error - Feed is empty");
            }
        }

        public virtual XElement LoadHeurekaProductsParameters()
        {
            XElement extParameters = null;
            if (File.Exists(FullFileName(ExtParametersFileName, "xml")))
            {
                try
                {
                    using (FileStream fs = new FileStream(FullFileName(ExtParametersFileName, "xml"), FileMode.Open, FileAccess.Read))
                    {
                        extParameters = XElement.Load(fs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("LoadHeurekaProductsParameters Exception: {0}", ex.Message);
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
                using (FileStream fs = new FileStream(FullFileName(ExtParametersFileName, "xml"),
                    FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    outDoc.Save(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveHeurekaProductsParameters Exception: {0}", ex.Message);
            }
        }

        public void ReadImportConfiguration()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Reading import configurations ...");
            Console.ResetColor();
            foreach (FeedImportSetting importSetting in SupplierSettings.FeedImportSettings)
            {
                ReadImportConfigurationFromGD(importSetting);
            }
        }

        private void ReadImportConfigurationFromGD(FeedImportSetting setting)
        {
            string line;
            List<string> list = new List<string>();

            GoogleDriveAPI gd = new GoogleDriveAPI();

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
            }
        }

        public virtual void FilterFeed()
        {
            if (CategoryBlackList == null || CategoryWhiteList == null ||
                ProductWhiteList == null || ProductBlackList == null)
            {
                Console.WriteLine("Eror - incomplete configuration.");
                return;
            }

            bool itemEnabled;

            FeedFiltered = new XElement("SHOP");

            int filteredCount = 0;
            foreach (XElement origItem in Feed.Descendants("SHOPITEM"))
            {
                itemEnabled = ChechProductByWBList(origItem);
                if (itemEnabled)
                {
                    filteredCount++;
                    FeedFiltered.Add(origItem);
                }
            }
        }

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

        private bool ChechProductByWBList(XElement item)
        {
            bool enabled = true;
            foreach (XElement itemCategory in item.Descendants("CATEGORY"))
            {
                enabled = true;
                foreach (string categoryBL in CategoryBlackList)
                {
                        if (itemCategory.Value.StartsWith(categoryBL))
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
                        if (itemCategory.Value.StartsWith(categoryWL))
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
                enabled = true;
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

        public virtual XElement GetHeurekaProductsParameters(bool onlyNew = false)
        {
            //throw new Exception("GetHeurekaProductsParameters not implemented for SupplierCode " + SupplierSettings.SupplierCode);

            XElement result = null;

            if (Feed == null)
                return result;

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

        public virtual void InitRemoteSetttings(List<FeedImportSetting> settings)
        {

        }

        public string FullFileName(string fileName, string extension)
        {
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            string fullPathFileName = Path.Combine(dataFolder, String.Concat(SupplierSettings.SupplierCode, "-", fileName, ".", extension));
            return fullPathFileName;
        }
    }
}

