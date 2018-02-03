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
            //GoogleDriveAPI gd = new GoogleDriveAPI();
            //gd.DownloadFile("1rkJ6MI99MMfCMPn-Z19zbqNmhS2fcTxJRwGJANPE7r0", "exp.txt", "text/plain");

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
