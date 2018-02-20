using System.Collections.Generic;

namespace Jaminet
{
    public class FeedImportSetting
    {
        public string Name { get; set; }
        public string GoogleDriveFileId { get; set; }
        public string MimeType { get; set; }
    }

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

   
}

