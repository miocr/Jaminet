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

        private const string feedFileName = "feed";
        private const string feedMergedFileName = "feed-merged";

        private const string extParametersFileName = "ext-products-parameters";

        protected SupplierSettings SupplierSettings { get; set; }
        
        public XElement Feed { get; set; }

        public List<string> CategoryWL { get; set; }
        public List<string> CategoryBL { get; set; }

        public List<string> ProductWL { get; set; }
        public List<string> ProductBL { get; set; }

        public Supplier()
        {
        }

        protected void Initialize(SupplierSettings supplierSetiings)
        {
            SupplierSettings = supplierSetiings;
        }

        public virtual void GetAndSaveFeed()
        {
            Downloader downloader = new Downloader(3600);
            long content = downloader.Download(FullFileName(feedFileName,"xml"), SupplierSettings.FeedUrl, 
                SupplierSettings.FeedUrlLogin, SupplierSettings.FeedUrlPassword);
        }

        public virtual XElement LoadFeed()
        {
            Feed = null;
            try
            {
                Console.WriteLine("Loading feed from file '{0}'", FullFileName(feedFileName,"xml"));
                using (FileStream fs = new FileStream(FullFileName(feedFileName,"xml"), FileMode.Open, FileAccess.Read))
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

        public virtual XElement LoadExternalParameters()
        {
            XElement extParameters = null;
            try
            {
                using (FileStream fs = new FileStream(FullFileName(extParametersFileName,"xml"), FileMode.Open, FileAccess.Read))
                {
                    extParameters = XElement.Load(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
            }
            return extParameters;
        }

        public virtual void SaveFeed(bool isFeedMerged)
        {
            if (Feed != null)
            {
                try
                {
                    string fileName = isFeedMerged ? feedMergedFileName : feedFileName;
                    using (FileStream fs = new FileStream(FullFileName(fileName,"xml"),
                        FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        Feed.Save(fs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: {0}", ex.Message);
                }
            }
            else
            {
                Console.WriteLine("SaveFeed error - Feed is empty");
            }
        }

        public virtual void SaveHeurekaProductsParameters(XElement extParameters)
        {
            XDocument outDoc = new XDocument();
            outDoc.Add(extParameters);

            try
            {
                using (FileStream fs = new FileStream(FullFileName(extParametersFileName,"xml"),
                    FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    outDoc.Save(fs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
            }
        }

        public void ReadImportConfiguration()
        {
            Console.ForegroundColor = ConsoleColor.Red;
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

            gd.DownloadFile(setting.GoogleDriveFileId,FullFileName(setting.Name,"txt"),setting.MimeType);

            try
            {
                using (TextReader tr = File.OpenText(FullFileName(setting.Name,"txt")))
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
                Console.WriteLine(exc.Message);
            }

            switch (setting.Name)
            {
                case categoryBLfileName:
                    CategoryBL = list;
                    break;
                case categoryWLfileName:
                    CategoryWL = list;
                    break;
                case productBLfileName:
                    ProductBL = list;
                    break;
                case productWLfileName:
                    ProductWL = list;
                    break;
            }


        }

        public virtual void MergeFeedWithExtParameters(XElement extParamaters)
        {
            if (Feed == null || extParamaters == null)
                return;

            List<string> extParametersProductCodes = new List<string>();
            foreach (XElement extItemParameters in extParamaters.Descendants("SHOPITEM"))
            {
                extParametersProductCodes.Add(extItemParameters.Attribute("CODE").Value);
            }

            string supplierProductCode;

            int itemCounter = 0;
            foreach (XElement origItem in Feed.Descendants("SHOPITEM"))
            {
                supplierProductCode = origItem.Element("CODE").Value;

                if (!extParametersProductCodes.Contains(supplierProductCode))
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

        public virtual XElement GetHeurekaProductsParameters()
        {
            throw new Exception("GetHeurekaProductsParameters not implemented for SupplierCode " + SupplierSettings.SupplierCode);
        }

        public virtual void InitRemoteSetttings(List<FeedImportSetting> settings)
        {

        }

        private string FullFileName(string fileName, string extension)
        {
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            string fullPathFileName = Path.Combine(dataFolder,String.Concat(SupplierSettings.SupplierCode,"-", fileName,".", extension));
            return fullPathFileName;
            //return String.Concat(SupplierSettings.SupplierCode, "-", fileName,".", extension);
        }
    }
}

