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

            TSBohemia tsb = new TSBohemia();

            //tsb.ReadImportConfiguration();

            //tsb.GetAndSaveFeed();

            tsb.LoadFeed();
            
            XElement extParameters = tsb.GetHeurekaProductsParameters(true);
            tsb.SaveHeurekaProductsParameters(extParameters);

            //extParameters = tsb.LoadExternalParameteFilers();
            //tsb.MergeFeedWithExtParameters(extParameters);

            //tsb.SaveFeed(true);

            Console.WriteLine("Press any key...");
            Console.Read();
        }
    }
}
