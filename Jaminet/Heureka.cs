using System;
using System.Linq;
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

        private string htmlPage;

        string ItemCode { get; set; }
        string ItemName { get; set; }
        string PartNumber { get; set; }
        string EAN { get; set; }

        public Heureka()
        {
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
            Downloader downloader = new Downloader();
            string search = searchText.Replace(" ", "+");
            htmlPage = downloader.GetPage(searchURL + search);
            if (!String.IsNullOrEmpty(htmlPage))
            {
                int searchPos1 = htmlPage.IndexOf(searchMark1, 0, StringComparison.OrdinalIgnoreCase);
                if (searchPos1 > 0)
                {
                    string produktSpecificationUrl = RelevantPairedProductProductUrl(htmlPage, producer);
                    if (produktSpecificationUrl.Length > 0)
                    {
                        htmlPage = downloader.GetPage(produktSpecificationUrl);
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
                                XElement panParValue = valueTd.Descendants("span")
                                    .Where(span => span.Attribute("id")
                                    .Value.StartsWith("param-value")).Single();
                                paramValue = panParValue.Value;

                                XElement spanParValue = valueTd.Descendants("span")
                                    .Where(span => span.Attribute("id")
                                    .Value.StartsWith("param-unit")).Single();

                                paramUnit = spanParValue.Value;
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
                            paramValue = String.Concat(paramName, " ", paramUnit);
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