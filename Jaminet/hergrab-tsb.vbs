' Retezec znaku, podle kterych se hledaji ve strance potrebne casti

Const comparePriceTest = "<span>Porovnat ceny</span>"  
Const searchMark1 = "<div class=""wherebuy"">"  
Const searchMark2 = "<a href=""http"  
Const searchMark3 = "<table id=""product-parameters"""
Const searchMark4 = "</table>"  

'----------------------------------------------------------------------------------------------------
Const debugMode = False
Const searchPauseCount = 1000000
Const searchPauseTimeSec = 10
Const ForReading = 1, ForWriting = 2
'----------------------------------------------------------------------------------------------------

' SCRIPT START

Set WshShell = WScript.CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
Set stream = CreateObject("ADODB.Stream")
stream.Open
stream.Charset = "utf-8"
stream.Type = 2


Set objArgs = WScript.Arguments
If (objArgs.Count < 2) Then
	WScript.Echo "Nejsou zadane parametry: <vstupnixmlsoubor> <vystupnixmlsoubor>" 
	WScript.Quit
End If

If (Not(debugMode)) Then On Error Resume Next

tsbProducts = objArgs.Item(0)

Set msxmlo = CreateObject("MSXML2.ServerXMLHTTP.3.0")
'Set msxmlo = CreateObject("Msxml2.ServerXMLHTTP.6.0") 
lResolve = 5 * 1000
lConnect = 5 * 1000  
lSend = 15 * 1000  
lReceive = 15 * 1000


Set srcXml = CreateObject("MSXML2.DOMDocument") 
Set outXml = CreateObject("MSXML2.DOMDocument") 

Set xmlHeader  = outXml.createProcessingInstruction ("xml","version='1.0'")  
Set outRoot = outXml.createElement("SHOPITEMS")
outXml.appendChild xmlHeader
outXml.appendChild outRoot

' fso nepodporuje UTF, jen ASCII
Set xmlFile = fso.OpenTextFile(currentDirectory & tsbProducts, ForReading)
srcXmlText = xmlFile.ReadAll
REM Dim srcXmlText
REM stream.Open
REM stream.CharSet = "utf-8"
REM stream.Type = 2
REM stream.LoadFromFile currentDirectory & tsbProducts
REM WScript.echo "Loaded file"
REM srcXmlText = stream.ReadText
REM WScript.echo "Loaded  stream"

srcOffset = 1
totalCount = 1
searchCount = 0

Do While (InStr(srcOffset, srcXmlText, "<SHOPITEM>", 1) > 0)
	searchPos1 = InStr(srcOffset, srcXmlText, "<SHOPITEM>", 1)
	srcOffset = searchPos1 + 1
	searchPos2 = InStr(searchPos1, srcXmlText, "</SHOPITEM>", 1)   
    If (searchPos2 > 0) Then 
        srcOffset = searchPos2
		shopItem = Mid(srcXmlText, searchPos1, searchPos2 - searchPos1)
		
		itemCode = ProductElementValue(shopItem, "CODE")
		WScript.echo vbCrLf
		WScript.echo "Zpracov·v·m polozku (CODE): " & itemCode & " (" & totalCount & ")"
		
		'itemName = ProductElementValue(shopItem, "NAME")
		'itemName = UTF8(itemName)
		
		itemProducer = ProductElementValue(shopItem, "MANUFACTURER")
		
		'Vynucene ukonceni po X polozkach, staci vlozit fake polozku
		'<SHOPITEM><CODE>#END#</CODE></SHOPITEM>
		If (itemCode = "#END#") Then Exit Do
		
		found = False
		ean = ProductElementValue(shopItem, "EAN")
        partnumber = ProductElementValue(shopItem, "PART_NUMBER")
		If (Len(ean) > 0) Then
			SearchPhrase ean
        ElseIf (Not(found) and Len(partnumber) > 0) Then    
			SearchPhrase partnumber
		End If
		
		If Not(found) Then
			WScript.echo "Nenalezeno nebo nesp·rov·no..."
		End If
		
		If (searchCount > searchPauseCount) Then
			Wscript.echo "Cekam na dalsi hledani... " & searchPauseTimeSec & "sec" & vbCrLf
			WScript.Sleep searchPauseTimeSec * 1000
			searchCount = 0
		End If
		
	End If
	totalCount = totalCount + 1
Loop

outXml.Save currentDirectory & objArgs.Item(1)

stream.close
Set stream = Nothing
Set WshShell = Nothing
Set fso = Nothing
Set msxmlo = Nothing

WScript.Quit

' SCRIPT END

'----------------------------------------------------------------------------------------------------

' SUBs

Sub SearchPhrase (searchText)
	stoken = searchText
    searchCount = searchCount + 1
    'WScript.echo "searchCount: " & searchCount
	WScript.echo "Hledam fr·zi: '" & stoken & "'"  

	'stokenEncoded = CP1250ToLatin2(Replace(LCase(stoken)," ","+"))
	stokenEncoded = Replace(stoken," ","+")
	currentDirectory = left(WScript.ScriptFullName,(Len(WScript.ScriptFullName))-(len(WScript.ScriptName)))
	hrSearchUrl = "https://www.heureka.cz/?h[fraze]=" & stokenEncoded
	html = GetPage(hrSearchUrl)
	searchPos1 = InStr(1, html, comparePriceTest, 1) 
	If (searchPos1 > 0) Then
		hrProduktUrl = RelevantPairedProductProductUrl (html)
		If (Len(hrProduktUrl) > 0) Then
			html = GetPage(hrProduktUrl)
			searchPos1 = InStr(1, html, searchMark3, 1)
			If (searchPos1 > 0) Then 
				searchPos2 = InStr(searchPos1, html, searchMark4, 1)
				If (searchPos2 > 0) Then
					found = True
					specification = Mid(html, searchPos1, searchPos2 + Len(searchMark4) - searchPos1)
					filePathName = currentDirectory & "/Output/" & NormalizedFileName(stoken) & "_specifikace.html"
					'ExportSpecification filePathName, specification
					ParseXml outXml, specification
				End If
			End If
		End If
	End If

End Sub

'---------------------------------------------------------------------------------------------------

Function RelevantPairedProductProductUrl (html)
	offset = 1
	pairedCounter = 0
	RelevantPairedProductProductUrl = ""
	urlItemProducer = Replace(LCase(itemProducer)," ","-")
	For i = 0 To 5 Step 1
	searchPos1 = InStr(offset, html, searchMark1, 1)
	If (searchPos1 > 0) Then 
		offset = searchPos1 + 1
		pairedCounter = pairedCounter + 1
		searchPos2 = InStr(searchPos1, html, searchMark2, 1)
		If (searchPos2 > 0) Then 
			searchPos2E = InStr(searchPos2 + 10, html, """", 1)
			If (searchPos2E > 0) Then
				hrProduktUrl = Mid(html, searchPos2 + 9, (searchPos2E - searchPos2 - 9)) & "specifikace/"
				If (InStr(1,LCase(hrProduktUrl),urlItemProducer) > 0) Then
					RelevantPairedProductProductUrl = hrProduktUrl
					Exit For
				End If
			End If
		End If
	End If
	Next		
	If (Len(RelevantPairedProductProductUrl) = 0 and pairedCounter = 1) Then
		RelevantPairedProductProductUrl = hrProduktUrl
	End If	
	If (Len(RelevantPairedProductProductUrl) > 0 and pairedCounter > 1) Then
		WScript.echo "VÌce shod! Zpracov·na pozice : " & pairedCounter & " (" & itemProducer & ")"
	End If
End Function

' FUNCs

Function ProductElementValueRegEx (productNode, pattern)
	Set re = new regexp
    re.Pattern = pattern
    re.Global = True
	If re.test(productNode) Then
        Set matches = re.Execute(productNode)
        ProductElementValue = matches(0).SubMatches(0)
    End If
End Function

Function ProductElementValue (productNode, elementName)
    searchPos1 = InStr(1,productNode,"<" & elementName & ">",1)
    If (searchPos1 > 0) Then
        searchPos2 = InStr(searchPos1,productNode,"</" & elementName & ">",1)
        If (searchPos2 > 0) Then
            valueStart = searchPos1 + Len(elementName) + 2
            value = Mid(productNode, valueStart, searchPos2 - valueStart)
			ProductElementValue = Replace(value,"&","")
        End If
    End If
End Function

' Prevod znaku do Latin2 (google umi v url jako hledany retez kodovani pouze Latin2 nebo utf8)
Function CP1250ToLatin2 (Text)
	For I = 1 To Len(Text)
		If Asc(Mid(Text, I, 1)) = 154  Then
			Temp = Temp & Chr(185) 'ö
		ElseIf Asc(Mid(Text, I, 1)) = 138  Then
			Temp = Temp & Chr(169) 'ä
		ElseIf Asc(Mid(Text, I, 1)) = 158  Then
			Temp = Temp & Chr(190) 'û
		ElseIf Asc(Mid(Text, I, 1)) = 142  Then
			Temp = Temp & Chr(174) 'é
		ElseIf Asc(Mid(Text, I, 1)) = 157  Then
			Temp = Temp & Chr(187) 'ù
		ElseIf Asc(Mid(Text, I, 1)) = 141  Then
			Temp = Temp & Chr(171) 'ç
		Else
			Temp = Temp & Mid(Text, I, 1)
		End If
	Next
	CP1250ToLatin2 = Temp
End Function

' Zruseni nepovolenych znaku v nazvu souboru
Function NormalizedFileName (fileName)
	Set re = new regexp
    re.Pattern = "[^\w :\\\.]"
    re.Global = True
    NormalizedFileName = re.Replace(fileName, "_")
End Function

Sub ExportSpecification (filePathName, specification)
	
	WScript.echo "Nalezeno - ukladam specifikaci do souboru", vbCrLf
	Set fileDesc = fso.CreateTextFile(filePathName, True)
	fileDesc.Write "<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">"
	fileDesc.Write "<head><meta http-equiv=""content-type"" content=""text/html; charset=cp1250""/>"
	fileDesc.Write "<link href=""https://im9.cz/css-v2/pages/product-detail-cart.css?4a845dfb"" rel=""stylesheet"" media=""screen,projection"" />"
	fileDesc.Write "</head>"
	fileDesc.Write "<body><h1>" & stoken & "</h1>" & vbCrLf & vbCrLf
	fileDesc.Write specification & vbCrLf & vbCrLf
	fileDesc.Write "</body>"
	fileDesc.Close
	Set fileDesc = Nothing
End Sub

Function GetPage (url)
	Err.Clear 
	msxmlo.setTimeouts lResolve, lConnect, lSend, lReceive
	msxmlo.open "GET", url, False
	If (Err.Number <> 0) Then 
		WScript.echo "Chyba pri stahovani stranky."
		WScript.StdOut.Write Chr(7)
		Err.Clear
	End If
	msxmlo.send()
	If (Err.Number = 0 and msxmlo.Status = 200) Then
		GetPage = msxmlo.ResponseText
		If (Err.Number <> 0) Then 
			WScript.echo "Chyba pri stahovani stranky."
			WScript.StdOut.Write Chr(7)
			Err.Clear
		End If
	End If
End Function

Function ParseXml (outXml, content)
	If (debugMode) Then
		Set fileDesc = fso.CreateTextFile(currentDirectory & "/Output/" & NormalizedFileName(itemCode), True)
		fileDesc.Write "<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">"
		fileDesc.Write "<head><meta http-equiv=""content-type"" content=""text/html; charset=utf-8""/></head>"
		fileDesc.Write "<body><h1>" & itemCode & "</h1>" & vbCrLf & vbCrLf
		fileDesc.Write content & vbCrLf & vbCrLf
		fileDesc.Write "</body>"
		fileDesc.Close
	End If
	Err.Clear
	srcXml.loadXML(content) 
	If (Err.Number <> 0) Then 
		WScript.echo "Nepodarilo se ziskat data specifikaci."
		WScript.StdOut.Write Chr(7)
		Exit Function
	End If
	srcXml.SetProperty "SelectionLanguage", "XPath"
	Set trs = srcXml.getElementsByTagName("tr")
	Set shopitem = outXml.createElement("SHOPITEM") 
	'shopitem.setAttribute "name", itemName
	shopitem.setAttribute "code", itemCode
	shopitem.setAttribute "pn", partnumber
	shopitem.setAttribute "ean", ean
	groupactive = False
	specValuesCount = 0
	
	For Each tr In trs

		Set headers = tr.getElementsByTagName("th")
		If (headers.Length > 0 or Not(groupactive))  Then
			thclass = headers.Item(0).getAttribute("class")
			If (InStr(1, thclass, "__table__head") > 0) Then
				headervalue = headers.Item(0).Text
			Else 
				headervalue = "ObecnÈ"
			End If
			Set specGroup = outXml.createElement("PARAMGROUP")	
			specGroup.setAttribute "name", headervalue
			shopitem.appendChild specGroup
			groupactive = True
		End If
	
		Set tds = tr.getElementsByTagName("td")
		If (tds.Length > 0) Then
			Set spec = outXml.createElement("PARAMETER")
			For Each td in tds
				If (Len(td.Text) > 0) Then 
					specvalue = ""
					tdclass = td.getAttribute("class")
					If (InStr(1, tdclass, "table__cell--param-name") > 0) Then
						Set nameSpans = td.getElementsByTagName("span")
						If (nameSpans.Length > 0) Then
							specvalue = nameSpans.Item(0).Text
						Else 
							specvalue = "N/A"
						End If
						Set specItem = outXml.createElement("NAME")
					ElseIf (InStr(1, tdclass, "table__cell--param-value") > 0) Then
						Set nameSpans = td.getElementsByTagName("span")
						If (nameSpans.Length > 0) Then
							For Each nameSpan In nameSpans
								specvalue = specvalue & nameSpan.Text & " " 
							Next
						Else 
							specvalue = "N/A"
						End If
						Set specItem = outXml.createElement("VALUE")
					ElseIf (InStr(1, tdclass, "--help") > 0) Then
						Set descSpans = td.getElementsByTagName("span[.='?']")
						If (descSpans.Length = 1 and Len(descSpans.Item(0).getAttribute("longdesc")) > 0) Then
							specvalue = descSpans.Item(0).getAttribute("longdesc")
							Set specItem = outXml.createElement("DESCRIPTION")
						End If
					End If
					If (Len(Trim(specvalue)) > 0) Then
						specItem.Text = Trim(specvalue)
						spec.appendChild specItem
					End If
				End If
			Next
			
			If (Len(Trim(spec.Text)) > 0) Then
				specValuesCount = specValuesCount + 1
				specGroup.appendChild spec
			End If

		End If
	
	Next

	outRoot.appendChild shopitem
	WScript.echo "Nalezeno, zpracov·no parametru: " & specValuesCount
	
End Function

Function UTF8( asciitext )
    stream.Position = 0
    stream.WriteText asciitext
    UTF8 = stream.ReadText
End Function