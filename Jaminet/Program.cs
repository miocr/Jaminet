using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

using log4net;
using log4net.Config;
using log4net.Repository;

namespace Jaminet
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(@".\log4net.config"));
            log.InfoFormat("Starting application with args: '{0}'", String.Join("",args));

            ProcessCommandLine(args);

            //Console.WriteLine("Press any key...");
            //Console.Read();
        }

        private static void ProcessCommandLine(string[] args)
        {
            Supplier supplier = null;
            XElement extParams = null;
            if (args.Length == 0 || args[0] != "-S" || args[0] == "-?" || args[0] == "-help")
            {
                #region Help
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(@"
Zpracovani feed produktu pro Jaminet.cz 
Pouziti: dotnet Jaminet -S kod akce1 akce2 akce3 ...

- S kod : povinny prvni parametr, kod je kod dodavatele (napr. 'TSB')

Akce (lze zadat vice, provedeny jsou v poradi zadani):
- DFF   : download a ulozeni hlavniho plneho feedu dodavatele
- DFU   : download a ulozeni aktualizacniho feedu dodavatele (sklad, ceny...)
- UPF   : aktualizace feedu a zpracovani pravidel, white/black listu
- PBF   : publikace zpracovaneho feedu do www 
- MEP   : pridani parametru produktu ziskanych z ext.zdroje
- GHPF  : ziskani parametru k vsem produktum ve feedu z Heureka.cz 
- GHPN  : ziskani parametru k novym produktum ve feedu z Heureka.cz
- ALL   : provede postupne DFF,DFU,UPF,PBF

Priklady:

dotnet Jaminet -S TSB -DF -PF -MEP 

Provede se download feedu dodavatele TSB, feed se zpracuje podle pravidel, 
pripoji se parametry produktu ziskane z externiho zdroje (pokud jsou pripravene)
a vysledny feed se ulozi na disk pro stazeni z Jaminet.cz

dotnet Jaminet -S TSB -DF -GHPF

Provede se download feedu dodavatele TSB a nasledne se pro vsechny produkty
pokusi ziskat parametry z Heureka.cz. Pozor, tato akce trva cca 5 hod.

");
                Console.ResetColor();
                return;
                #endregion
            }

            Console.Clear();

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
                    case "-DFF":
                        if (supplier != null)
                            supplier.GetAndSaveFeed(Supplier.FeedType.FullOriginal);
                        break;
                    case "-DFU":
                        if (supplier != null)
                            supplier.GetAndSaveFeed(Supplier.FeedType.UpdateOriginal);
                        break;
                    case "-UPF":
                        if (supplier != null)
                        {
                            supplier.ProcessFeed(true);
                            supplier.SaveFeed(Supplier.FeedType.Processed);
                        }
                        break;
                    case "-PF":
                        if (supplier != null)
                        {
                            supplier.ProcessFeed(false);
                            supplier.SaveFeed(Supplier.FeedType.Processed);
                        }
                        break;
                    case "-PBF":
                        supplier.PublishFeed();
                        break;
                    case "-MEP":
                        if (supplier != null)
                        {
                            extParams = supplier.LoadHeurekaProductsParameters();
                            supplier.MergeFeedWithExtParameters(extParams);
                            supplier.SaveFeed(Supplier.FeedType.Processed);
                        }
                        break;
                    case "-GHPF":
                        if (supplier != null)
                        {
                            extParams = supplier.GetHeurekaProductsParameters(false);
                            if (extParams != null)
                                supplier.SaveHeurekaProductsParameters(extParams);
                        }
                        break;
                    case "-GHPN":
                        if (supplier != null)
                        {
                            extParams = supplier.GetHeurekaProductsParameters(true);
                            if (extParams != null)
                                supplier.SaveHeurekaProductsParameters(extParams);
                        }
                        break;
                    case "-ALL":
                        if (supplier != null)
                        {
                            supplier.GetAndSaveFeed(Supplier.FeedType.FullOriginal);
                            supplier.GetAndSaveFeed(Supplier.FeedType.UpdateOriginal);
                            supplier.ProcessFeed(true);
                            supplier.SaveFeed(Supplier.FeedType.Processed);
                            supplier.PublishFeed();
                        }
                        break;
                    default:
                        break;
                }
            }
        }

    }
}
