using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.IO;
using System.Collections.Generic;

namespace Jaminet
{
    public class Heureka
    {
        private const string searchURL = "https://www.heureka.cz/?h[fraze]=";
        private const string comparePriceTest = "<span>Porovnat ceny</span>";
        private const string searchMark1 = "<div class=\"wherebuy\">";
        private const string searchMark2 = "<a href=\"http";
        private const string searchMark3 = "<table id=\"product-parameters\"";
        private const string searchMark4 = "</table>";

        private static readonly Regex htmlAMPValidateRegex = new Regex(@"(?<=>.*)&(?=.*<\/)", RegexOptions.Compiled);
        private static readonly Regex htmlGTValidateRegex = new Regex(@"(?<=>.*)>(?=.*<\/)", RegexOptions.Compiled);
        private static readonly Regex htmlLTValidateRegex = new Regex(@"(?<=>.*)<(?=.*<\/)", RegexOptions.Compiled);
        private string htmlPage;

        public class SearchObject
        {
            public string SupplierCode { get; set; }
            public string Manufacturer { get; set; }
            public string EAN { get; set; }
            public string PN { get; set; }
            public string ISBN { get; set; }
            public string Text { get; set; }
        }

        public List<SearchObject> SearchList { get; set; }
        public Supplier CurrentSupplier { get; set; }

        public Heureka(Supplier supplier)
        {
            CurrentSupplier = supplier;
            SearchList = new List<SearchObject>();
        }


        public XElement GetProductsParameters()
        {
            Console.WriteLine("Start searching Heureka.cz for products parameters...");

            DateTime startTime = DateTime.Now;
            int itemsCounter = 0;
            int foundItemsCounter = 0;
            int notFoundItemsCounter = 0;
            //int totalParamsGrabbed = 0;
            //int paramsGrabbed;

            FileStream fs = new FileStream("temp.xml", FileMode.Create, FileAccess.ReadWrite);
            XmlWriter xw = XmlWriter.Create(fs);

            xw.WriteStartElement("SHOP");

            XElement productParameters;
            bool foundByEAN = false;
            bool foundByPN = false;

            foreach (SearchObject search in SearchList)
            {
                if (itemsCounter % 100 == 0)
                {
                    //Console.Clear();
                    GC.Collect();
                    Console.Write("           Processed items: {0} [found:{1} | not found:{2}]\r",
                        itemsCounter, foundItemsCounter, notFoundItemsCounter);
                }

                productParameters = null;
                try
                {
                    foundByPN = false;
                    foundByEAN = false;

                    itemsCounter++;

                    //Console.Write("{0} searching for: {1} > ", itemCounter.ToString("000000"), search.SupplierCode.PadRight(10));

                    if (search.PN != null)
                    {
                        productParameters = GetProductParameters(search.PN, search.Manufacturer);
                        foundByPN = (productParameters != null);
                    }

                    if (!foundByPN && search.EAN != null)
                    {
                        productParameters = GetProductParameters(search.EAN, search.Manufacturer);
                        foundByEAN = (productParameters != null);
                    }

                    if (foundByPN || foundByEAN)
                    {
                        foundItemsCounter++;
                        //XElement outShopItem = new XElement("SHOPITEM");
                        xw.WriteStartElement("SHOPITEM");

                        //outShopItem.Add(new XAttribute();
                        xw.WriteAttributeString("CODE", search.SupplierCode);

                        //outShopItem.Add(productParameters);
                        productParameters.WriteTo(xw);

                        //outShopItems.Add(outShopItem);
                        xw.WriteEndElement(); //SHOPITEM

                        //paramsGrabbed = productParameters.Descendants("INFORMATION_PARAMETER").Count();
                        //totalParamsGrabbed += paramsGrabbed;
                        /*
                        Console.Write("found {0} parameters ", paramsCount);
                        if (foundByEAN)
                            Console.WriteLine("by EAN:{0}", search.EAN);
                        else if (foundByPN)
                            Console.WriteLine("by PN:{0}", search.PN);
                        else
                            Console.WriteLine();
                        */
                        //Console.Write("+");
                    }
                    else
                    {
                        notFoundItemsCounter++;
                        /*
                        Console.WriteLine("not found");
                        */
                        //Console.Write("-");
                    }

                    Console.Write(">");
                    if (itemsCounter % 10 == 0)
                        Console.Write("\r");

                    //switch (itemsCounter % 4)
                    //{
                    //    case 0: Console.Write("\r/"); break;
                    //    case 1: Console.Write("\r-"); break;
                    //    case 2: Console.Write("\r\\"); break;
                    //    case 3: Console.Write("\r|"); break;
                    //}

                    productParameters = null;
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Exception with SHOPITEM code:{0}, Exception:{1}",
                        search.SupplierCode, ex.Message);
                }
                
            }

            xw.WriteEndElement();

            xw.Flush();
            fs.Flush();

            xw.Dispose();
            fs.Dispose();

            TimeSpan span = DateTime.Now - startTime;
            Console.WriteLine("Finished! Grabbed {0} products, total time: {1}",
                itemsCounter, span.ToString());

            //return outShopItems;
            return XElement.Load("temp.xml");
        }

        public XElement XGetProductsParameters()
        {

            Console.WriteLine("Start searching Heureka.cz for products parameters...");

            DateTime startTime = DateTime.Now;
            int itemsCounter = 0;
            int foundItemsCounter = 0;
            int notFoundItemsCounter = 0;
            int totalParamsGrabbed = 0;
            int paramsGrabbed;
            XElement outShopItems = new XElement("SHOP");
            XElement productParameters;
            bool foundByEAN = false;
            bool foundByPN = false;

            foreach (SearchObject search in SearchList)
            {
                productParameters = null;
                try
                {
                    foundByPN = false;
                    foundByEAN = false;

                    itemsCounter++;

                    //Console.Write("{0} searching for: {1} > ", itemCounter.ToString("000000"), search.SupplierCode.PadRight(10));

                    if (search.EAN != null)
                    {
                        productParameters = GetProductParameters(search.EAN, search.Manufacturer);
                        foundByEAN = (productParameters != null);
                    }

                    if (!foundByEAN && search.PN != null)
                    {
                        productParameters = GetProductParameters(search.PN, search.Manufacturer);
                        foundByPN = (productParameters != null);
                    }

                    if (foundByPN || foundByEAN)
                    {
                        foundItemsCounter++;
                        XElement outShopItem = new XElement("SHOPITEM");
                        outShopItem.Add(new XAttribute("CODE", search.SupplierCode));
                        outShopItem.Add(productParameters);

                        outShopItems.Add(outShopItem);

                        paramsGrabbed = productParameters.Descendants("INFORMATION_PARAMETER").Count();
                        totalParamsGrabbed += paramsGrabbed;
                        /*
                        Console.Write("found {0} parameters ", paramsCount);
                        if (foundByEAN)
                            Console.WriteLine("by EAN:{0}", search.EAN);
                        else if (foundByPN)
                            Console.WriteLine("by PN:{0}", search.PN);
                        else
                            Console.WriteLine();
                        */
                        Console.Write("+");
                    }
                    else
                    {
                        notFoundItemsCounter++;
                        //Console.WriteLine("not found");
                        Console.Write("-");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Exception with SHOPITEM code:{0}, Exception:{1}",
                        search.SupplierCode, ex.Message);
                }

                if (itemsCounter % 100 == 0)
                {
                    //Console.Clear();
                    Console.WriteLine("Processed items {0} [found:{1}, not found:{2}]",itemsCounter, foundItemsCounter, notFoundItemsCounter);
                    GC.Collect();
                }
            }

            TimeSpan span = DateTime.Now - startTime;
            Console.WriteLine("Finished! Grabbed {0} products, total time: {1}",
                itemsCounter, span.ToString());

            return outShopItems;
        }

        public XElement GetProductParameters(string searchText, string producer)
        {
            XElement parameters = null;
            string paramString = GetParameters(searchText, producer);
            if (!String.IsNullOrEmpty(paramString))
            {
                parameters = ParseToXml(paramString);
            }
            return parameters;
        }

        private string GetParameters(string searchText, string producer)
        {
            string result = null;
            string search = searchText.Replace(" ", "+");
            htmlPage = Downloader.GetPage(searchURL + search);
            if (!String.IsNullOrEmpty(htmlPage))
            {
                int searchPos1 = htmlPage.IndexOf(searchMark1, 0, StringComparison.OrdinalIgnoreCase);
                if (searchPos1 > 0)
                {
                    string produktSpecificationUrl = RelevantPairedProductProductUrl(htmlPage, producer);
                    if (produktSpecificationUrl.Length > 0)
                    {
                        htmlPage = Downloader.GetPage(produktSpecificationUrl);
                        searchPos1 = htmlPage.IndexOf(searchMark3, 0, StringComparison.OrdinalIgnoreCase);
                        if (searchPos1 > 0)
                        {
                            int searchPos2 = htmlPage.IndexOf(searchMark4, searchPos1, StringComparison.OrdinalIgnoreCase);
                            if (searchPos2 > 0)
                            {
                                result = htmlPage.Substring(searchPos1, searchPos2 + searchMark4.Length - searchPos1);
                            }
                        }
                    }
                }
            }
            return result;
        }

        private string RelevantPairedProductProductUrl(string htmlPage, string producer)
        {
            int maxRelevantSearchs = 10;
            bool relevantProducerFound = false;
            int offset = 1;
            int pairedCounter = 0;
            string hrProduktUrl = null;
            string result = String.Empty;


            producer = producer.ToLower().Replace(" ", "-");

            for (int i = 0; i < maxRelevantSearchs; i++)
            {
                int searchPos1 = htmlPage.IndexOf(searchMark1, offset, StringComparison.OrdinalIgnoreCase);
                if (searchPos1 > 0)
                {
                    offset = searchPos1 + 1;
                    pairedCounter++;
                    int searchPos2 = htmlPage.IndexOf(searchMark2, searchPos1);
                    if (searchPos2 > 0)
                    {
                        int searchPos2E = htmlPage.IndexOf("\"", searchPos2 + 10);
                        if (searchPos2E > 0)
                        {
                            hrProduktUrl = htmlPage.Substring(searchPos2 + 9, (searchPos2E - searchPos2 - 9));
                            if (hrProduktUrl.IndexOf(producer, 0) > 0)
                            {
                                relevantProducerFound = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (relevantProducerFound)
            {
                result = hrProduktUrl + "specifikace/";
                //if (pairedCounter > 1)
                //{ 
                //    // Nalezeno vice sparovanych produktu 
                //    Console.WriteLine("Multiple results, using by producer match '{0}'", producer);
                //}
            }

            return result;
        }

        private XElement ParseToXml(string htmlProductSpecs)
        {
            htmlProductSpecs = htmlAMPValidateRegex.Replace(htmlProductSpecs, "&amp;");
            htmlProductSpecs = htmlGTValidateRegex.Replace(htmlProductSpecs, "&gt;");
            htmlProductSpecs = htmlLTValidateRegex.Replace(htmlProductSpecs, "&lt;");
            XDocument srcXml = XDocument.Parse(htmlProductSpecs);
            IEnumerable<XElement> trs = srcXml.XPathSelectElements("table/tbody/tr");

            //string groupName = null;
            string paramName = null;
            string paramValue = null;
            string paramUnit = null;
            //string paramHelp = null;

            XElement outRoot = new XElement("INFORMATION_PARAMETERS");

            try
            {
                foreach (XElement tr in trs)
                {
                    paramName = null;
                    paramValue = null;
                    paramUnit = null;
                    //paramHelp = null;

                    // Shoptet feed nepodporuje skupiny parametrů
                    //try
                    //{
                    //    XElement th = tr.Descendants("th").Single();
                    //    groupName = th.Value;
                    //    continue;
                    //}
                    //catch (InvalidOperationException exc) { };

                    if (!tr.Descendants("td").Any())
                    {
                        continue;
                    }

                    try
                    {
                        XElement tdParName = tr.Descendants("td").Where(td => td.Attribute("class")
                          .Value.Contains("table__cell--param-name")).Single();
                        IEnumerable<XElement> tdSpans = tdParName.Descendants("span");
                        paramName = tdParName.Value;

                        // TR s výrobcem ignorujeme
                        if (paramName == "Výrobce")
                            continue;
                    }
                    catch (InvalidOperationException) { };

                    try
                    {
                        XElement valueTd = tr.Descendants("td").Where(td => td.Attribute("class")
                        .Value.Contains("table__cell--param-value")).Single();

                        if (valueTd != null)
                        {
                            if (valueTd.Descendants("span").Attributes("id").Any())
                            {
                                XElement tdSpanParValue = valueTd.Descendants("span")
                                    .Where(span => span.Attribute("id")
                                    .Value.StartsWith("param-value")).Single();
                                paramValue = tdSpanParValue.Value;

                                XElement tdSpanParUnit = valueTd.Descendants("span")
                                    .Where(span => span.Attribute("id")
                                    .Value.StartsWith("param-unit")).Single();
                                paramUnit = tdSpanParUnit.Value;
                            }
                            else
                            {
                                paramValue = valueTd.Value;
                                paramUnit = null;
                            }
                        }
                    }
                    catch (InvalidOperationException) { };


                    // Shoptet feed nepodporuje popis parametru
                    //try
                    //{
                    //    XElement tdParHelp = tr.Descendants("td").Where(td => td.Attribute("class")
                    //    .Value.Contains("table__cell--help")).Single();
                    //    if (tdParHelp != null)
                    //    {
                    //        XElement spanParHelp = tdParHelp.Descendants("span")
                    //               .Where(span => span.Value == "?").Single();
                    //        if (spanParHelp != null)
                    //        {
                    //            paramHelp = spanParHelp.Attribute("longdesc").Value;
                    //        }
                    //    }
                    //}
                    //catch (InvalidOperationException exc) { };

                    if (paramName != null && paramValue != null)
                    {
                        XElement outItem = new XElement("INFORMATION_PARAMETER");
                        outItem.Add(new XElement("NAME", paramName));

                        // Shoptet feed nepodruje unit, pridame ho do hodnoty
                        if (!String.IsNullOrEmpty(paramUnit))
                            paramValue = String.Concat(paramValue, " ", paramUnit);
                        outItem.Add(new XElement("VALUE", paramValue));

                        // Shoptet feed nepodporuje popis parametru
                        //if (!String.IsNullOrEmpty(paramHelp))
                        //    param.Add(new XElement("HELP", paramHelp));

                        outRoot.Add(outItem);
                    }

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception {0}", ex.Message);
            }

            return outRoot;
        }


    }
}