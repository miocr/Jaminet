using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.IO;
using System.Collections.Generic;

namespace HeurekaGrab
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

    string ItemCode {get;set;}
    string ItemName {get;set;}
    string PartNumber {get;set;}
    string EAN {get;set;}
 
    public Heureka()
    {
    }

    public void GetProductParameters(string searchText, string producer)
    {
        string paramString = GetParameters(searchText, producer);
        if (!String.IsNullOrEmpty(paramString))
        {
          ParseToXml(paramString);
        }


    }
    private string GetParameters(string searchText, string producer)
    {
      string result = null;
      Downloader downloader = new Downloader();
      string search = searchText.Replace(" ","+");
      htmlPage = downloader.GetPage(searchURL + search);
      if (!String.IsNullOrEmpty(htmlPage))
      {
        int searchPos1 = htmlPage.IndexOf(searchMark1,0,StringComparison.OrdinalIgnoreCase);
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

    private string RelevantPairedProductProductUrl (string htmlPage, string producer)
    {
        int maxRelevantSearchs = 5;
      	int offset = 1;
	      int pairedCounter = 0;
        string hrProduktUrl = String.Empty;
	      string result = String.Empty; 

	      producer = producer.ToLower().Replace(" ","-");

	      for (int i = 0; i < maxRelevantSearchs; i++)
            {   
          int searchPos1 = htmlPage.IndexOf(searchMark1,offset,StringComparison.OrdinalIgnoreCase);
          if (searchPos1 > 0) 
          { 
            offset = searchPos1 + 1;
            pairedCounter++;
            int searchPos2 = htmlPage.IndexOf(searchMark2, searchPos1);
            if  (searchPos2 > 0) 
            {
              int searchPos2E = htmlPage.IndexOf( "\"", searchPos2 + 10);
              if (searchPos2E > 0) 
              {
                hrProduktUrl = htmlPage.Substring(searchPos2 + 9, (searchPos2E - searchPos2 - 9));
                if ( hrProduktUrl.IndexOf(producer,0) > 0)
                {
                  break;
                }
              }
            }
          }
        }


        if (hrProduktUrl.Length > 0 && pairedCounter == 1)
        {
            result  =  hrProduktUrl + "specifikace/";
        }
        if (hrProduktUrl.Length > 0 && pairedCounter > 1) 
        {
          Console.WriteLine("Více shod! Zpracována pozice : " 
                      + pairedCounter + " (" + producer + ")");
        }
        
        return result;
    }

    private XmlNode ParseToXml (string htmlProductSpecs)
    {
      XmlDocument srcXml = new XmlDocument();
      XmlDocument destXml = new XmlDocument();
      srcXml.LoadXml(htmlProductSpecs);
      XDocument x = XDocument.Parse(htmlProductSpecs);
      IEnumerable<XElement> trs = x.XPathSelectElements("table/tbody/tr");
      
      string groupName;
      string paramName;
      string paramValue;
      string paramHelp;
      XElement tmp;


      foreach (XElement tr in trs)
      {
        try 
        {
          XElement th = tr.Descendants("th").Single();
          groupName = th.Value;
        } catch {}

        try
        {
        XElement tdParName = tr.Descendants("td").Where(td => td.Attribute("class")
          .Value.Contains("table__cell--param-name")).Single();
          IEnumerable<XElement> tdSpans = tdParName.Descendants("span");
          paramName = tdParName.Value;
        }
        catch {}
      
        try{
        XElement tdParValue = tr.Descendants("td").Where(td => td.Attribute("class")
        .Value.Contains("table__cell--param-value")).Single();
        } catch {}

        try {
        XElement tdParHelp = tr.Descendants("td").Where(td => td.Attribute("class")
        .Value.Contains("table__cell--help")).Single();
        } catch {}

      }
      
      
      //childList = x.Descendants("td").Where(t => t.Attribute("class").Value.Contains("table__cell--param-value"));


      //IEnumerable<XElement> childList = from el in x.Descendants().Elements("tr") select el;  
 
  
      /*
	    XmlElement shopitem = destXml.CreateElement("SHOPITEM");
      

      //shopitem.SetAttribute("name", itemName);
    	//shopitem.SetAttribute("code", itemCode);
	    //shopitem.SetAttribute("pn", partnumber);
	    //shopitem.SetAttribute("ean", ean);
      

	    bool groupactive = false;
      string specName = null;
      string specValue = null;
    	int specValuesCount = 0;
      string groupHeader = String.Empty;
	
      foreach (XmlNode tr in trs)
      {
        foreach (XmlNode trChild in tr.ChildNodes)
        {
          if (trChild.Name == "th")
          {
            foreach (XmlNode thChild in trChild.ChildNodes)
            {
              if (!groupactive || thChild.Name == "class")
              {
                string thclassValue = thChild.Value;
                if (thclassValue.IndexOf("__table__head",0) > 0)
                {
                  groupHeader = tr.Value;
                }
                else
                {
                  groupHeader = "Obecné";
                }

                XmlNode specGroup = destXml.CreateElement("PARAMGROUP");
                XmlAttribute attr = destXml.CreateAttribute("name");
                attr.Value = groupHeader;
      			    specGroup.Attributes.Append(attr);

      			    shopitem.Append(specGroup);
			          groupactive = true;
              }
            }
          }

          if (trChild.Name == "td")
          {
            XmlNode specPar = destXml.CreateElement("PARAMETER");
            foreach (XmlAttribute tdAttr in trChild.Attributes) 
            {
              if (tdAttr.Name == "class" && tdAttr.Value.Contains("table__cell--param-name"))
              {
                foreach (XmlNode tdChild in trChild.ChildNodes)
                {
                  if (tdChild.Name == "span")
                  {
                    specValue = tdChild.Value;
                    break;
                  }
                }
              }
              else if (tdAttr.Name == "class" && tdAttr.Value.Contains("table__cell--param-value"))
              {
                foreach (XmlNode tdChild in trChild.ChildNodes)
                {
                  if (tdChild.Name == "span")
                  {
                    specValue = tdChild.Value;
                    break;
                  }
                }
              }
              else if (tdAttr.Value.Contains("--help"))
              {

              
              }
            }
          }
        }
	*/

	return null;
  }


  }
}