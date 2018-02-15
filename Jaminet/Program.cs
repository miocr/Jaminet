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

            ProccessCommandLine(args);
            //TSBohemia tsb = new TSBohemia();

            //tsb.ReadImportConfiguration();

            //tsb.GetAndSaveFeed();

            //tsb.LoadFeed();

            //XElement extParameters = null;

            //extParameters = tsb.GetHeurekaProductsParameters();
            //tsb.SaveHeurekaProductsParameters(extParameters);
            //extParameters = tsb.LoadHeurekaProductsParameters();

            //tsb.FilterFeed();

            //tsb.MergeFeedWithExtParameters(extParameters);

            //tsb.SaveFeed(true);

            Console.WriteLine("Press any key...");
            Console.Read();
        }

        private static void ProccessCommandLine(string[] args)
        {
            Supplier supplier = null;
            XElement extParams = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-S":
                        if (args.Length > i)
                        {
                            string supplicerCode = args[++i];
                            switch (supplicerCode.ToLower())
                            {
                                case "tsb":
                                    supplier = new TSBohemia();
                                    supplier.ReadImportConfiguration();
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    case "-DF":
                        if (supplier != null)
                            supplier.GetAndSaveFeed();
                        break;
                    case "-PF":
                        if (supplier != null)
                        {
                            supplier.FilterFeed();
                            supplier.SaveFeed(false);
                        }

                        break;
                    case "-MEP":
                        if (supplier != null)
                        {
                            extParams = supplier.LoadHeurekaProductsParameters();
                            supplier.MergeFeedWithExtParameters(extParams);
                            supplier.SaveFeed(true);
                        }
                        break;
                    case "-GHPF":
                        extParams = supplier.GetHeurekaProductsParameters(false);
                        if (extParams != null)
                            supplier.SaveHeurekaProductsParameters(extParams);
                        break;
                    case "-GHPN":
                        if (supplier != null)
                        {
                            extParams = supplier.GetHeurekaProductsParameters(true);
                            if (extParams != null)
                                supplier.SaveHeurekaProductsParameters(extParams);
                        }
                        break;

                    default:
                        break;
                }
            }
        }

    }
}
