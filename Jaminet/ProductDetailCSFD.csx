using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Net;

using Eternal.Business.Discussion;
using Eternal.CacheCore;
using Eternal.WebCore;
using Eternal.Web.Handlers;
using Iesi.Collections.Generic;

using log4net;

namespace Eternal.Web.Product
{
	public class ProductDetailCSFD : ProductDetailBase, IEseDataCacheable
	{

		private static readonly ILog _log = LogManager.GetLogger(typeof(ProductDetailCSFD));

		// Tøídy
		//==========================================================================

		/// <summary>
		/// Tøída pro stažení obsahu URL s možností zadání TimeOut
		/// </summary>
		private class WebDownload : WebClient
		{
			/// <summary>
			/// Time in milliseconds
			/// </summary>
			public int Timeout { get; set; }

			public WebDownload() : this(60000) { }

			public WebDownload(int timeout)
			{
				this.Timeout = timeout;
			}

			protected override WebRequest GetWebRequest(Uri address)
			{
				var request = base.GetWebRequest(address);
				if (request != null)
				{
					request.Timeout = this.Timeout;
				}
				return request;
			}
		}

		// Atributy
		//==========================================================================

		string averageRattng;
		string bestOfPosition;
		string popularityPosition;
		string itemUrl;

		// Accesssory
		//==========================================================================

		string _attributeReleased;
		[EspAttribute("attribute-released")]
		public string AttributeReleased
		{
			get { return _attributeReleased; }
			set { _attributeReleased = value; }
		}

		string _removeFromName;
		[EspAttribute("remove-from-name")]
		public string RemoveFromName
		{
			get { return _removeFromName; }
			set { _removeFromName = value; }
		}

		int _attrReleasedId;
		[EspAttribute("attribute-released-id")]
		public int AttrReleasedId
		{
			get { return _attrReleasedId; }
			set { _attrReleasedId = value; }
		}

		int _attrSetId;
		[EspAttribute("attribute-set-id")]
		public int AttrSetId
		{
			get { return _attrSetId; }
			set { _attrSetId = value; }
		}

		int _timeOut;
		[EspAttribute("timeout")]
		public int Timeout
		{
			get { return _timeOut; }
			set { _timeOut = value; }
		}

		// Konstrukce
		//==========================================================================

		public ProductDetailCSFD()
		{
		}

		// Metody
		//==========================================================================

		/// <summary>
		/// Vrátí data z elementu jako XML.
		/// </summary>
		/// <param name="outputStream"></param>
		public override void GetData(Stream stream)
		{
			if (ProductId == null)
				return;

			XmlWriter xmlWriter = XmlWriter.Create(stream);
			xmlWriter.WriteStartDocument();
			xmlWriter.WriteStartElement("product-detail-csfd");

			Eternal.Business.Discussion.Product product = null;
			Option option = null;

			GetProduct(ref product, ref option);
			if (product == null)
				return;

			// podivame se, zda produktu uz nema link na CSFD v attachments
			string csfdUrlAttachment = null;

			if (product.ProductAttachments != null && product.ProductAttachments.Count > 0)
			{
				foreach (Eternal.Business.Discussion.ProductAttachement att in product.ProductAttachments)
				{
					if (att.Name != null && att.Name == "ÈSFD")
					{
						// ulozime si url na CSFD z priloh produktu a nasledne ji pouzijeme pro vycteni hodnoceni
						csfdUrlAttachment = att.Path;
					}
				}
			}

			if (Timeout == 0)
				Timeout = 1000; // default 1s, pokud neni v atributu ese

			// Info hledame jen pokud jsou zadane nutna definice sady a atributu (Filmy | Natocen v roce)
			if (_attrReleasedId != 0 && _attrSetId != 0)
			{
				try
				{
					GetCSFDRating(product, csfdUrlAttachment, Timeout);
				}
				catch (WebException wex)
				{
					if (wex.Status != WebExceptionStatus.Timeout)
					{
						if (_log.IsErrorEnabled)
							_log.ErrorFormat("Chyba pøi stažení ÈSFD. " + wex.Message);
					}
				}
				catch (Exception ex)
				{
					if (_log.IsErrorEnabled)
						_log.ErrorFormat("Chyba pøi stažení a analýze stránky ÈSFD. " + ex.ToString());
				}
			}

			// Pokud jsme ziskali analyzou hledanim na csfd url k titulu a jeste jsme ji nemeli
			// pridame ji jako attachment produktu
			if (!String.IsNullOrEmpty(itemUrl) && String.IsNullOrEmpty(csfdUrlAttachment))
			{
				ProductAttachement pa = new ProductAttachement();
				pa.Type = (byte)ProductAttachementType.URL;
				pa.Name = "ÈSFD";
				pa.Lang = "CZ";
				pa.Description = String.Empty;
				pa.DisplayedOrder = String.Empty;
				pa.Length = String.Empty;
				pa.DownloadName = String.Empty;
				pa.Path = itemUrl;
				pa.OrderableItem = product;
				ProductAttachementService pas = Scope.CreateService<ProductAttachementService>();
				pas.SaveProductAttachement(pa);
				product.ProductAttachments.Add(pa);

			}

			xmlWriter.WriteElementString("product-name", product.Name);
			if (option != null)
				xmlWriter.WriteElementString("option-name", option.Name);

			xmlWriter.WriteStartElement("csfd");
			if (!String.IsNullOrEmpty(averageRattng))
				xmlWriter.WriteAttributeString("average-rating", averageRattng);
			if (!String.IsNullOrEmpty(bestOfPosition))
				xmlWriter.WriteAttributeString("best-of-position", bestOfPosition);
			if (!String.IsNullOrEmpty(popularityPosition))
				xmlWriter.WriteAttributeString("popularity-position", popularityPosition);
			if (!String.IsNullOrEmpty(itemUrl))
				xmlWriter.WriteAttributeString("item-url", itemUrl);
			xmlWriter.WriteEndElement();


			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndDocument();
			xmlWriter.Flush();
			xmlWriter.Close();
		}

		//--------------------------------------------------------------------------

		private void GetCSFDRating(Eternal.Business.Discussion.Product product, string csfdAttachmentUrl, int timeOut)
		{
			String csfdAnalysedUrl = null;
			String releaseYearAttr = null;
			bool releaseYearMatch = false;

			AttributeService attrService = Scope.CreateService<AttributeService>();
			AttributeValue attrValue = attrService.GetOrderableAttributeValue(product.Id, _attrReleasedId, _attrSetId);
			if (attrValue != null && !String.IsNullOrEmpty(attrValue.TextValue))
			{
				releaseYearAttr = attrValue.TextValue.Trim();
				releaseYearAttr = releaseYearAttr.Substring(releaseYearAttr.Length - 4);
			}
			else
			{
				// Pokud nema titul atrubut Natoceno v roce, nezkousime ani hledat pouze podle nazvu
				return;
			}

			#region Vyhledani hodnotu attributu podle nazvu atributu a sady (nepouziva se)
			//	const string attSetName = "Filmy"; // hodnotu prevzi z ese atributu
			//	const string attReleaseName = "Natoèeno v roce"; // hodnotu prevzi z ese atributu
			//	bool attrFound = false;
			//  int attrId = -1;
			//  foreach (ProductAttributeSet set in product.AttributeSets)
			//  {
			//    if (set.AttributeSet.Name == attSetName)
			//    {
			//      foreach (AttributeDefinition attr in set.AttributeSet.Attributes)
			//      {
			//        if (attr.GetName((int)product.Category.Region) == attReleaseName)
			//        {
			//          //attr.Get
			//          attrId = attr.Id;
			//          foreach (AttributeValue attrValue in set.Values)
			//          {
			//            if (attrValue.Attribute.Id == attrId && !String.IsNullOrEmpty(attrValue.TextValue))
			//            {
			//              releaseYearAttr = attrValue.TextValue.Trim();
			//              releaseYearAttr = releaseYearAttr.Substring(releaseYearAttr.Length - 4);
			//              attrFound = true;
			//              break;
			//            }
			//          }
			//        }
			//        if (attrFound)
			//          break;
			//      }
			//    }
			//    if (attrFound)
			//      break;
			//  }
			#endregion

			// Najdeme hodnoceni na CSFD, hledame podle nazvu v UPPER podobe
			string ratingDivContent = null;
			string productName = product.Name.ToUpper();

			// z nazvu odstranime zadane fraze ([DVD] atd)
			if (!String.IsNullOrEmpty(RemoveFromName))
			{
				string[] forRemove = RemoveFromName.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < forRemove.Length; i++)
					productName = productName.Replace(forRemove[i], "");
			}
			productName = productName.Trim();

			string pageHTML = null;
			XmlDocument htmlXmlSegment = new XmlDocument();

			// Url produkt nema jako atachment, tak ji zkusime zjistit vyhledanim titulu na CSFD...
			if (String.IsNullOrEmpty(csfdAttachmentUrl))
			{
				using (WebDownload webClient = new WebDownload())
				{
					webClient.Timeout = timeOut;
					webClient.Encoding = Encoding.UTF8;
					pageHTML = webClient.DownloadString("http://www.csfd.cz/hledat/?q=" + productName).ToLower();
				}
				if (!String.IsNullOrEmpty(pageHTML))
				{
					int searchDivPos = pageHTML.IndexOf("<div id=\"search-films\"");
					if (searchDivPos > 0)
					{
						int listUlPosStart = pageHTML.IndexOf("<ul class=\"ui-image-list js-odd-even\">", searchDivPos);
						if (listUlPosStart > 0)
						{
							int listUlPosEnd = pageHTML.IndexOf("</ul>", listUlPosStart);
							if (listUlPosStart > 0 && listUlPosEnd > 0 && (listUlPosEnd > listUlPosStart))
							{
								string listUlCon = pageHTML.Substring(listUlPosStart - 1, (listUlPosEnd - listUlPosStart) + 6);
								// Z HTML segmentu si udelame pracovni XML document pro snazsi prochazeni...
								htmlXmlSegment.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + listUlCon);
								foreach (XmlNode titul in htmlXmlSegment.SelectNodes("ul/li"))
								{
									XmlNode h3 = titul.SelectSingleNode("h3");
									if (h3 != null)
									{
										XmlNode h3a = h3.SelectSingleNode("a");
										if (h3a != null)
										{
											// Kontrola shody nazvu
											if (!String.IsNullOrEmpty(h3a.InnerText) && h3a.InnerText.ToUpper().Trim() == productName &&
													h3a.Attributes.Count > 0 && h3a.Attributes["href"] != null)
											{
												csfdAnalysedUrl = "http://www.csfd.cz" + h3a.Attributes["href"].InnerText;
												// Kontrola shody roku vydani 
												XmlNode firstp = titul.SelectSingleNode("p");
												if (firstp != null && !String.IsNullOrEmpty(firstp.InnerText))
												{
													if (firstp.InnerText.Contains(releaseYearAttr))
													{
														releaseYearMatch = true;
														break;
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				// pro vycteni hodnoceni titulu pouzijeme url jiz ulozenou jako attachment produktu
				csfdAnalysedUrl = csfdAttachmentUrl;
				// pokud je url na csfd z attachmentu produktu, uz neoverujeme rok, protoze url je spravna
				releaseYearMatch = true;
			}

			// pokud mame url na titul v csfd  a je shoda roku natoceni, 
			// povazujeme to za shodny nalezeny titul a vycteme aktualni hodnoceni 
			if (!String.IsNullOrEmpty(csfdAnalysedUrl) && releaseYearMatch)
			{
				itemUrl = csfdAnalysedUrl;
				// stahneme stranku titulu 
				pageHTML = String.Empty;
				using (WebDownload webClient = new WebDownload())
				{
					webClient.Timeout = timeOut;
					webClient.Encoding = Encoding.UTF8;
					pageHTML = webClient.DownloadString(itemUrl).ToLower();
				}
				int ratingDivStart = pageHTML.IndexOf("<div id=\"rating\">");
				int ratingDivEnd = pageHTML.IndexOf("</div>", ratingDivStart);
				if (ratingDivStart > 0 && ratingDivEnd > 0 && (ratingDivEnd > ratingDivStart))
				{
					ratingDivContent = pageHTML.Substring(ratingDivStart - 1, (ratingDivEnd - ratingDivStart) + 7);
					htmlXmlSegment.LoadXml(ratingDivContent);
					XmlNode rating = htmlXmlSegment.SelectSingleNode("div/h2");
					if (rating != null && !String.IsNullOrEmpty(rating.InnerText))
					{
						// Vycteme prumerne hodnoceni;
						averageRattng = System.Text.RegularExpressions.Regex.Match(rating.InnerText, @"\d+").Value;
					}

					foreach (XmlNode chart in htmlXmlSegment.SelectNodes("div/p/a"))
					{
						if (chart.InnerText.Contains("nejlepší"))
						{
							// Vycteme pozici nejlepsi film
							bestOfPosition = System.Text.RegularExpressions.Regex.Match(chart.InnerText, @"\d+").Value;
						}
						if (chart.InnerText.Contains("nejoblíbenìjší"))
						{
							// Vycteme pozici nejoblibenejsi film
							popularityPosition = System.Text.RegularExpressions.Regex.Match(chart.InnerText, @"\d+").Value;
						}
					}
				}
			}
		}

		//--------------------------------------------------------------------------


	}
}
