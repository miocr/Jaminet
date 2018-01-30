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
#if DEBUG
                    if (code == "#END#")
                        break;
#endif
                    ean = inShopItem.Element("EAN")?.Value;
                    pn = inShopItem.Element("PART_NUMBER")?.Value;
                    manufacturer = inShopItem.Element("MANUFACTURER")?.Value;

                    XElement productParameters = null;

                    try
                    {
                        Console.Write("{0} > Processing item code: {1}", 
                            (itemCounter++).ToString("000000"), code.PadRight(10));

                        if (ean != null)
                            productParameters = heureka.GetProductParameters(ean, manufacturer);

                        if (productParameters == null && pn != null)
                            productParameters = heureka.GetProductParameters(pn, manufacturer);

                        if (productParameters != null)
                        {
                            XElement outShopItem = new XElement("SHOPITEM");
                            outShopItem.Add(new XAttribute("CODE", code));
                            outShopItem.Add(productParameters);

                            outShopItems.Add(outShopItem);

                            int paramsCount = productParameters.Descendants("INFORMATION_PARAMETER").Count();

                            Console.WriteLine("... found {0} parameters", paramsCount);
                        }
                        else
                        {
                            Console.WriteLine("... not found");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Exception with SHOPITEM code:{0}, Exception:{1}", 
                            code, ex.Message);
                    }
                }

                outDoc.Add(outShopItems);

                TimeSpan span = DateTime.Now - startTime;
                Console.WriteLine("Time: {0}", span.ToString());

                try
                {
                    using (FileStream fs = new FileStream("out.xml", FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        outDoc.Save(fs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: {0}", ex.Message);
                }

            }
        }
    }
}
