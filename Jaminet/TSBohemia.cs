using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using log4net;

namespace Jaminet
{
    public class TSBohemia : Supplier
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(TSBohemia));

        public TSBohemia() : base()
        {
            SupplierSettings supplierSettings = new SupplierSettings()
            {
                SupplierCode = "TSB",
                // FeedFull https://cdn1.tsbohemia.cz/export/ab3cbae0337bead9c7fdfbc97657653b/products.xml
                // FeedOnlyStock https://cdn1.tsbohemia.cz/export/bbfa6e1221297b3f439a4a08eb13be11/products.xml
                FeedUrl = "https://cdn1.tsbohemia.cz/export/ab3cbae0337bead9c7fdfbc97657653b/products.xml",
                FeedUrlLogin = null,
                FeedUrlPassword = null
            };

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //mio https://drive.google.com/open?id=1rkJ6MI99MMfCMPn-Z19zbqNmhS2fcTxJRwGJANPE7r0
                //https://drive.google.com/open?id=1ND7wZjB4eJBvZtpnq7QD-rIqfuIYJrU3R5KGZKhjZVo
                Name = categoryBLfileName, //"category-bl",
                GoogleDriveFileId = "1ND7wZjB4eJBvZtpnq7QD-rIqfuIYJrU3R5KGZKhjZVo",
                MimeType = "text/plain"
            });

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //mio https://drive.google.com/open?id=1pfYxfdaLzrlEhmeHxmpHqqLDU4Me7jx2aQ-EwtxD8-o
                //https://drive.google.com/open?id=1ozQPdSY_iyeyxk8UfLLpThKjedZxvDO8P0xVzV8_4H0
                Name = categoryWLfileName,// "category-wl",
                GoogleDriveFileId = "1ozQPdSY_iyeyxk8UfLLpThKjedZxvDO8P0xVzV8_4H0",
                MimeType = "text/plain"
            });

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //mio https://drive.google.com/open?id=1x6lJNYaZinT2n8RqTVVF_l4BVqRi5ZpFR6AibmJqUW8
                //https://drive.google.com/open?id=1jvVIdcEycU_kza1PEdwbL-2ZFNXTIAiaQUebMhRNj8Y
                Name = productBLfileName, //"product-bl",
                GoogleDriveFileId = "1jvVIdcEycU_kza1PEdwbL-2ZFNXTIAiaQUebMhRNj8Y",
                MimeType = "text/plain"
            });

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //mio https://drive.google.com/open?id=15ZISrCdQZkrl1coJ0L4oOXnEdeJNqjXmK3z-HEbnv2c
                //https://drive.google.com/open?id=1--_pCyn0Xjxp68FpYr-3NtRSRJbTWX7VyTWOi1W5r5I
                Name = productWLfileName, //"product-wl",
                GoogleDriveFileId = "1--_pCyn0Xjxp68FpYr-3NtRSRJbTWX7VyTWOi1W5r5I",
                MimeType = "text/plain"
            });

            supplierSettings.FeedImportSettings.Add(new FeedImportSetting
            {
                //mio https://drive.google.com/open?id=10eFpeOSpsFN6Dpr8h6jDW1b_nWx31SYzGkGFZ3iKFnQ
                //https://drive.google.com/open?id=1ULQKJfrrsRHvErZjLjUlqdtotEuwjG6Jtt0A-HO_Tbc
                Name = importConfigFileName, //"config",
                GoogleDriveFileId = "1ULQKJfrrsRHvErZjLjUlqdtotEuwjG6Jtt0A-HO_Tbc",
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

    }


}
