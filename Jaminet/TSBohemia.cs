using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Text;

namespace Jaminet
{
    public class TSBohemia : Supplier
    {
        public TSBohemia() : base()
        {
            SupplierSettings supplierSettings = new SupplierSettings()
            {
                SupplierCode = "TSB",
                FeedUrl = "https://cdn1.tsbohemia.cz/export/ab3cbae0337bead9c7fdfbc97657653b/products.xml",
                FeedUrlLogin = null,
                FeedUrlPassword = null
            };

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //https://drive.google.com/open?id=1rkJ6MI99MMfCMPn-Z19zbqNmhS2fcTxJRwGJANPE7r0
                Name = categoryBLfileName, //"category-bl",
                GoogleDriveFileId = "1rkJ6MI99MMfCMPn-Z19zbqNmhS2fcTxJRwGJANPE7r0",
                MimeType = "text/plain"
            });

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //https://drive.google.com/open?id=1pfYxfdaLzrlEhmeHxmpHqqLDU4Me7jx2aQ-EwtxD8-o
                Name = categoryWLfileName,// "category-wl",
                GoogleDriveFileId = "1pfYxfdaLzrlEhmeHxmpHqqLDU4Me7jx2aQ-EwtxD8-o",
                MimeType = "text/plain"
            });

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //https://drive.google.com/open?id=1x6lJNYaZinT2n8RqTVVF_l4BVqRi5ZpFR6AibmJqUW8
                Name = productBLfileName, //"product-bl",
                GoogleDriveFileId = "1x6lJNYaZinT2n8RqTVVF_l4BVqRi5ZpFR6AibmJqUW8",
                MimeType = "text/plain"
            });

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //https://drive.google.com/open?id=15ZISrCdQZkrl1coJ0L4oOXnEdeJNqjXmK3z-HEbnv2c
                Name = productWLfileName, //"product-wl",
                GoogleDriveFileId = "15ZISrCdQZkrl1coJ0L4oOXnEdeJNqjXmK3z-HEbnv2c",
                MimeType = "text/plain"
            });

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //https://drive.google.com/open?id=10eFpeOSpsFN6Dpr8h6jDW1b_nWx31SYzGkGFZ3iKFnQ
                Name = importConfigFileName, //"config",
                GoogleDriveFileId = "10eFpeOSpsFN6Dpr8h6jDW1b_nWx31SYzGkGFZ3iKFnQ",
                MimeType = "text/plain"
            });

            base.Initialize(supplierSettings);
        }


        public void GetCategories()
        {
            List<string> categories = new List<string>();
            Feed = LoadFeed();
            foreach (XElement shopItem in Feed.Descendants("SHOPITEM"))
            {
                foreach (XElement itemCategory in shopItem.Descendants("CATEGORY"))
                {
                    if (!String.IsNullOrEmpty(itemCategory.Value) && !categories.Contains(itemCategory.Value))
                    {
                        categories.Add(itemCategory.Value);
                    }
                }
            }
            categories.Sort();

            using (TextWriter catListFile = File.CreateText(SupplierSettings.SupplierCode + "-categories.txt"))
            {
                foreach (string category in categories)
                {
                    catListFile.WriteLine(category);
                }
            }
        }

        public override XElement GetHeurekaProductsParameters(bool onlyNew)
        {
            XElement result = null;

            if (Feed == null)
                return result;

            Heureka heureka = new Heureka(this);

            XElement extParameters = LoadExternalParameters();
            List<string> existsExtParametrs = new List<string>();
            if (onlyNew && extParameters != null)
            {

                foreach (XElement extParamater in extParameters.Descendants("SHOPITEM"))
                {
                    existsExtParametrs.Add(extParamater.Attribute("CODE").Value);
                }
            }

            foreach (XElement inShopItem in Feed.Descendants("SHOPITEM"))
            {
                if (inShopItem.Element("CODE")?.Value == "#END#")
                    break;

                Heureka.SearchObject searchObject = new Heureka.SearchObject();
                searchObject.SupplierCode = inShopItem.Element("CODE")?.Value;
                searchObject.EAN = inShopItem.Element("EAN")?.Value;
                searchObject.PN = inShopItem.Element("PART_NUMBER")?.Value;
                searchObject.Manufacturer = inShopItem.Element("MANUFACTURER")?.Value;

                heureka.SearchList.Add(searchObject);
            }

            result = heureka.GetProductsParameters(true);

            return result;
        }
    }


}
