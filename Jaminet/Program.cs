using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Threading.Tasks;
using System.Linq;

namespace Jaminet
{
    class Program
    {
        static void Main(string[] args)
        {
<<<<<<< HEAD
            //GoogleDriveAPI gd = new GoogleDriveAPI();
            //gd.DownloadFile("1rkJ6MI99MMfCMPn-Z19zbqNmhS2fcTxJRwGJANPE7r0", "exp.txt", "text/plain");
=======
            TSBohemia tsb = new TSBohemia();
            XElement feed = tsb.ReadFeed("products1.xml");

            if (feed != null)
            {
                DateTime startTime = DateTime.Now;
                int itemCounter = 0;

                string code;
                string ean;
                string pn;
                string manufacturer;
                Heureka heureka = new Heureka();
                XDocument outDoc = new XDocument();
                XElement outShopItems = new XElement("SHOPITEMS");

                foreach (XElement inShopItem in feed.Descendants("SHOPITEM"))
                {
                    code = inShopItem.Element("CODE").Value;

                    if (code == "#END#")
                        break;

                    ean = inShopItem.Element("EAN")?.Value;
                    pn = inShopItem.Element("PART_NUMBER")?.Value;
                    manufacturer = inShopItem.Element("MANUFACTURER")?.Value;

                    XElement productParameters = null;

                    try
                    {
                        Console.Write("{0} > Processing item code: {1}", 
                            (itemCounter++).ToString("000000"), code.PadRight(10));
>>>>>>> 8c3171722f3967f869e43d6064e33565e9825dab

            TSBohemia tsb = new TSBohemia();
            tsb.ReadImportConfiguration();

            //tsb.GetAndSaveFeed();

            //tsb.LoadFeed();

            //XElement extParameters = tsb.GetHeurekaProductsParameters();
            //tsb.SaveHeurekaProductsParameters(extParameters);

            //XElement extParameters = tsb.LoadExternalParameters();
            //tsb.MergeFeedWithExtParameters(extParameters);

            //tsb.SaveFeed();
        }
    }
}
