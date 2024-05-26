using HtmlAgilityPack;
using System.Net;

namespace OmniDictionary
{
    public class DictScraper
    {
        private static readonly HttpClient httpClient = new();
        static readonly List<string> dict_urls = ["https://de.pons.com/%C3%BCbersetzung/", "https://en.wiktionary.org/wiki/", "https://sozdik.kz/translate/kk/ru/", "https://www.dict.cc/?s=", "MR_glossary.url"];

        private string html = "";
        private string url = "";
        //private int dict_index = 3;
        public string LangName { get; set; } = "German";
        public int LangId { get; set; } = 5;
        public int DictIndex { get; set; } = 3;


        //for some reason setting the .Text property of these pre-built Spans and adding them to the FormattedString messes up in a completely inscrutable way; creating the spans on the fly must be inefficient as hell but no other way seems to work properly
        private Span header_span_main = new Span { Text = "", FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontAttributes = FontAttributes.Bold, FontSize = 16 };
        private Span header_span_aux = new Span { Text = "", FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontSize = 14 };

        public DictScraper()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        }

        private async Task<string> GetHTMLAsync()
        {
            
            try { return await httpClient.GetStringAsync(url); }
            catch (HttpRequestException)
            {
                return "404 not found";
            }

        }

        private void NoResultsFound(string msg="No results found")
        {
            dict_results.Add(new DictResult(true, false, msg, ""));
        }
        private void ScraperUnavailable()
        {
            dict_results.Add(new DictResult(true, false, "This dictionary doesn't work yet", ""));
        }

        private static bool HasStyleValues(string styleAttribute, string stylePropertyName, string stylePropertyValue)
        {
            foreach (string style in styleAttribute.Split(';'))
            {
                var cssPair = style.Split(":");
                if (cssPair.Length == 2)
                {
                    string cssProperty = cssPair[0];
                    string cssValue = cssPair[1];

                    if (cssProperty == stylePropertyName && cssValue == stylePropertyValue) return true;
                    //else return false;
                }
                else return false;
            }
            return false;
        }

        private void ParseDictCC()
        {    
            string extractText(HtmlNode element)
            {
                string txt = "";
                foreach(HtmlNode child in element.ChildNodes)
                {
                    //Debug.WriteLine(child.GetAttributeValue("style", ""));
                    if (child.Name == "div" && child.Id.StartsWith("elliwrap"))
                    {
                        txt += child.InnerText;
                    }
                    
                    else if (child.Name == "span" && HasStyleValues(child.GetAttributeValue("style", ""), "top", "-3px"))
                    {
                        //the <sup> tags dont work in MAUI so I am just going to use square-brackets
                        txt += " [" + child.InnerText + "]";
                    }
                    else if(child.InnerText == "Unverified" && HasStyleValues(child.GetAttributeValue("style", ""), "background-color", "red"))
                    {
                        txt += "<i>unverified</i>";
                    }
                    else if (child.Name == "sup")
                    {
                        txt += "[<i>" + child.InnerText + "</i>]"; //for the Dat. or Akk. superscripts indicating the case of 'sich' and such
                    }
                    else if(child.Name != "dfn" &&  child.Name != "div")
                    {
                        txt += child.InnerText;
                    }
                    
                }
                return txt.Trim();
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var dictcc_pg = doc.DocumentNode;

            var result_rows = dictcc_pg.SelectNodes("//tr");
            int result_count = 0;

            foreach(HtmlNode result_row in result_rows)
            {
                if(result_row.Id == "")
                {   
                    if(result_row.FirstChild.HasClass("td6"))
                    {
                        string header_text = WebUtility.HtmlDecode(result_row.FirstChild.InnerText);
                        dict_results.Add(new DictResult((result_count % 2 == 0), true, header_text, ""));
                        result_count++;
                    }        
                    continue;
                }

                if (result_row.SelectNodes("*[@class=\'td7nl\']") != null)
                {
                    var cell_pair = result_row.SelectNodes("*[@class=\'td7nl\']");
                    string lefthand_text = WebUtility.HtmlDecode(extractText(cell_pair[0]));
                    string righthand_text = WebUtility.HtmlDecode(extractText(cell_pair[1]));
                    dict_results.Add(new DictResult((result_count % 2 == 0), false, lefthand_text, righthand_text));
                    result_count++;
                }                    
            }
            if(result_count == 0)
            {
                //dict_results.Add(new DictResult(false, true, "No results found", ""));
                NoResultsFound();
                return;
            }
        }

        static private bool MatchesAnyClass(List<string> classNames, HtmlNode node)
        {
            foreach(string className in classNames)
            {
                if (node.HasClass(className)) return true;
            }
            return false;
        }

        private void ParsePONS()
        {
            string extractText(HtmlNodeCollection node_list)
            {
                string text = "";

                foreach(HtmlNode node in node_list)
                {

                    if(node.NodeType == HtmlNodeType.Element && MatchesAnyClass(["case", "info", "rhetoric", "genus", "style", "topic", "restriction", "complement", "region", "explanation"], node))
                    {
                        text += "[" + node.InnerText.Trim() + "]";
                    }
                    else if(node.NodeType == HtmlNodeType.Element && node.HasClass("collocator"))
                    {
                        text += "(" + node.InnerText.Trim() + ")";
                    }
                    /*else if(Regex.IsMatch(node.InnerText, "^\\s+$"))
                    {
                        text += " ";
                    }*/
                    else
                    {
                        text += node.InnerText;
                    }
                }
                return text.Trim().NormaliseWhitespace();
            }

            FormattedString extractHeaderText(HtmlNodeCollection node_list)
            {
                FormattedString formatted_text = new FormattedString();
                string text = "";
                foreach(HtmlNode node in node_list)
                {
                    if(node.NodeType == HtmlNodeType.Element && node.Name == "span" && !node.MatchesAnyClass(["headword_attributes", "headword", "headword_spelling"]))
                    {
                        text += "<i>" + node.InnerText + "</i>";
                        header_span_aux.Text = node.InnerText.NormaliseWhitespace().HtmlDecode();
                        //formatted_text.Spans.Add(header_span_aux);
                        formatted_text.Spans.Add(new Span { Text = node.InnerText.NormaliseWhitespace().HtmlDecode(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#e6e6e6"), FontSize = 14 });
                    }
                    else if(node.NodeType == HtmlNodeType.Element && node.Name == "span" && node.HasClass("headword_attributes"))
                    {
                        text += node.InnerText; //this block is to get the title= attribute to show HTML title tooltip for certain headwords, but I doubt it will work in MAUI and it is doubtful to be worth implementing a proper tooltip for it so really this whole block is unneccessary
                        //header_span_main.Text = node.InnerText.NormaliseWhitespace();
                        formatted_text.Spans.Add(new Span { Text = node.InnerText.NormaliseWhitespace().HtmlDecode(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontAttributes = FontAttributes.Bold, FontSize = 16 });
                    }
                    else if(node.HasClass("headword"))
                    {
                        string headword_text = "";
                        foreach(HtmlNode child_node in node.ChildNodes)
                        {
                            if(child_node.NodeType == HtmlNodeType.Text)
                            {
                                text += child_node.InnerText;
                                headword_text += child_node.InnerText;
                                //break;
                            }
                        }
                        header_span_main.Text = headword_text.NormaliseWhitespace().HtmlDecode();
                        //formatted_text.Spans.Add(header_span_main);
                        formatted_text.Spans.Add(new Span { Text = headword_text.NormaliseWhitespace().HtmlDecode(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontAttributes = FontAttributes.Bold, FontSize = 16 });
                    }
                    else if(node.NodeType == HtmlNodeType.Text)
                    {
                        text += node.InnerText;
                        header_span_main.Text = node.InnerText.NormaliseWhitespace().HtmlDecode();
                        //formatted_text.Spans.Add(header_span_main);
                        formatted_text.Spans.Add(new Span { Text = node.InnerText.NormaliseWhitespace().HtmlDecode(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontAttributes = FontAttributes.Bold, FontSize = 16 });
                    }
                }

                //return text.Trim().NormaliseWhitespace();
                return formatted_text;
            }

            FormattedString extractH3Text(HtmlNodeCollection node_list)
            {
                FormattedString formatted_text = new FormattedString();

                if (node_list.Count < 2) {
                    //return node_list[0].InnerText.Trim().NormaliseWhitespace();
                    if(node_list.Count > 0) formatted_text.Spans.Add(new Span { Text = node_list[0].InnerText.Trim().NormaliseWhitespace().HtmlDecode(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#e6e6e6"), FontSize = 14 });
                    return formatted_text;
                }
                string text = "";
                foreach( HtmlNode node in node_list)
                {
                    if(node.MatchesAnyClass(["info", "style", "reflection"]))
                    {
                        text += "<abbr>" + node.InnerText.Trim() + "</abbr> "; //convert to FormattedString and make the text greyer to match the website
                        formatted_text.Spans.Add(new Span { Text = node.InnerText.Trim().NormaliseWhitespace().HtmlDecode(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4be"), FontSize = 14, FontAttributes = FontAttributes.Italic });
                    }
                    else
                    {
                        text += node.InnerText.Trim() + " ";
                        formatted_text.Spans.Add(new Span { Text = node.InnerText.Trim().NormaliseWhitespace().HtmlDecode() + " ", FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#e6e6e6"), FontSize = 14 });
                    }
                }
                //return text;
                return formatted_text;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var PONS_page = doc.DocumentNode;

            if(LangId == 4)
            {
                HtmlNodeCollection results_sections = PONS_page.GetElementsByClassName("results");
                if(results_sections.Count == 0)
                {
                    NoResultsFound();
                    return;
                }
                HtmlNodeCollection entry_sections = results_sections[0].GetElementsByClassName("entry");
                for(int i = 0; i < entry_sections.Count; i++)
                {
                    //dict_results.Add(new DictResult(false, true, "", "", extractHeaderText(entry_sections[i].SelectSingleNode(".//h2").ChildNodes)));

                    HtmlNodeCollection translations = entry_sections[i].GetElementsByClassName("translations");
                    for(int j = 0; j < translations.Count; j++)
                    {
                        HtmlNode? previous_sibling = translations[j].PreviousSibling;
                        if(previous_sibling != null && previous_sibling.HasClass("rom"))
                        {
                            HtmlNode? h2_header_node = previous_sibling.SelectSingleNode(".//h2");
                            if(h2_header_node != null) dict_results.Add(new DictResult(false, true, "", "", extractHeaderText(h2_header_node.ChildNodes)));
                        }

                        if (translations[j].SelectSingleNode(".//h3").InnerText.Trim() == "Wendungen:")
                        {
                            dict_results.Add(new DictResult(false, true, "Wendungen", ""));

                            HtmlNodeCollection entries_left = translations[j].SelectNodesOrEmpty(".//*[@class=\'dt-inner\']/*[@class=\'source\']");
                            HtmlNodeCollection entries_right = translations[j].SelectNodesOrEmpty(".//*[@class=\'dd-inner\']/*[@class=\'target\']");

                            for(int k = 0; k < entries_left.Count; k++)
                            {
                                dict_results.Add(new DictResult((k % 2 == 0), false, extractText(entries_left[k].ChildNodes), extractText(entries_right[k].ChildNodes)));
                            }
                        }
                        else
                        {
                            var formatted_h3_text = extractH3Text(translations[j].SelectSingleNode(".//h3").ChildNodes);
                            if(formatted_h3_text.ToString() != "") dict_results.Add(new DictResult(false, true, "", "", formatted_h3_text));

                            HtmlNodeCollection entries_left = translations[j].SelectNodesOrEmpty(".//*[@class=\'dt-inner\']/*[@class=\'source\']");
                            HtmlNodeCollection entries_right = translations[j].SelectNodesOrEmpty(".//*[@class=\'dd-inner\']/*[@class=\'target\']");

                            for (int k = 0; k < entries_left.Count; k++)
                            {
                                dict_results.Add(new DictResult((k % 2 == 0), false, extractText(entries_left[k].ChildNodes), extractText(entries_right[k].ChildNodes)));
                            }
                        }
                    }
                }
                if(entry_sections.Count == 0)
                {
                    HtmlNodeCollection dl_sections = results_sections[0].SelectNodesOrEmpty(".//dl");
                    for(int i = 0; i < dl_sections.Count; i++)
                    {
                        HtmlNodeCollection entries_left = dl_sections[i].SelectNodesOrEmpty(".//*[@class=\'dt-inner\']/*[@class=\'source\']");
                        HtmlNodeCollection entries_right = dl_sections[i].SelectNodesOrEmpty(".//*[@class=\'dd-inner\']/*[@class=\'target\']");

                        for (int j = 0; j < entries_left.Count; j++)
                        {
                            dict_results.Add(new DictResult((j % 2 == 0), false, extractText(entries_left[j].ChildNodes), extractText(entries_right[j].ChildNodes)));
                        }
                    }
                }
                return;
            }

            HtmlNodeCollection meaning_sections = PONS_page.GetElementsByClassName("rom");
            int rom_lngth = meaning_sections.Count;

            //this is for when PONS probably doesn't have an exact entry for the word but does have the word included in other example sentences/entries
            if(rom_lngth == 0)
            {
                HtmlNodeCollection results_sections = PONS_page.GetElementsByClassName("results");//SelectNodes("//*[contains(concat(' ', normalize-space(@class), ' '), ' results ')]");
                if(results_sections.Count == 0 || (results_sections.Count == 1 && results_sections[0].ParentNode.HasClass("catalog-browse"))) 
                {
                    NoResultsFound();
                    return;
                }
                for(int i = 0; i < results_sections.Count && i < 2; i++)
                {

                    HtmlNodeCollection entries_left = results_sections[i].SelectNodesOrEmpty(".//*[@class=\'dt-inner\']/*[@class=\'source\']");
                    HtmlNodeCollection entries_right = results_sections[i].SelectNodesOrEmpty(".//*[@class=\'dd-inner\']/*[@class=\'target\']");

                    for(int j = 0; j < entries_left.Count; j++)
                    {
                        dict_results.Add(new DictResult((j % 2 == 0), false, extractText(entries_left[j].ChildNodes), extractText(entries_right[j].ChildNodes)));
                    }
                }
                return;
            }

            for(int i = 0; i < rom_lngth; i++)
            {
                if(meaning_sections[i].SelectSingleNode(".//*[contains(concat(' ', normalize-space(@class), ' '), ' signature-od ')]") ==  null)
                {
                    HtmlNode h2_header_node = meaning_sections[i].SelectSingleNode(".//h2");
                    if (h2_header_node != null)
                    {
                        //dict_results.Add(new DictResult(false, true, extractHeaderText(h2_header_node.ChildNodes)));
                        dict_results.Add(new DictResult(false, true, "", "", extractHeaderText(h2_header_node.ChildNodes)));
                    }

                    HtmlNodeCollection blocks = meaning_sections[i].GetElementsByClassName("translations");
                    for(int j = 0; j < blocks.Count; j++)
                    {
                        if (blocks[j].SelectSingleNode(".//h3").InnerText.Trim() == "Wendungen:")
                        {
                            dict_results.Add(new DictResult(false, true, "Wendungen", ""));
                            HtmlNodeCollection entries_left = blocks[j].SelectNodesOrEmpty(".//*[@class=\'dt-inner\']/*[@class=\'source\']");
                            HtmlNodeCollection entries_right = blocks[j].SelectNodesOrEmpty(".//*[@class=\'dd-inner\']/*[@class=\'target\']");

                            for(int k = 0; k < entries_left.Count; k++)
                            {
                                dict_results.Add(new DictResult((k % 2 == 0), false, extractText(entries_left[k].ChildNodes), extractText(entries_right[k].ChildNodes)));
                            }

                        }
                        else
                        { 
                            HtmlNode h3_header_node = blocks[j].SelectSingleNode(".//h3");
                            if (h3_header_node != null && h3_header_node.InnerText.Trim() != "")
                            {
                                dict_results.Add(new DictResult(false, true, "", "", extractH3Text(h3_header_node.ChildNodes)));
                            }

                            HtmlNodeCollection entries_left = blocks[j].SelectNodesOrEmpty(".//*[@class=\'dt-inner\']/*[@class=\'source\']");
                            HtmlNodeCollection entries_right = blocks[j].SelectNodesOrEmpty(".//*[@class=\'dd-inner\']/*[@class=\'target\']");

                            for(int k = 0; k < entries_left.Count ; k++)
                            {
                                dict_results.Add(new DictResult((k % 2 == 0), false, extractText(entries_left[k].ChildNodes), extractText(entries_right[k].ChildNodes)));
                            }


                        }
                    }
                }
            }

            HtmlNode beispielsatz_block = PONS_page.SelectSingleNode(".//*[contains(concat(' ', normalize-space(@class), ' '), ' results-dict-examples ')]");
            if(beispielsatz_block != null )
            {
                dict_results.Add(new DictResult(false, true, "Beispielsätze", ""));

                HtmlNodeCollection beispiele_left = beispielsatz_block.SelectNodesOrEmpty(".//*[@class=\'dt-inner\']/*[@class=\'source\']");
                HtmlNodeCollection beispiele_right = beispielsatz_block.SelectNodesOrEmpty(".//*[@class=\'dd-inner\']/*[@class=\'target\']");

                for(int i = 0; i < beispiele_left.Count ; i++)
                {
                    dict_results.Add(new DictResult((i % 2 == 0), false, extractText(beispiele_left[i].ChildNodes), extractText(beispiele_right[i].ChildNodes)));
                }
            }

        }

        //this Wiktionary-scraper is dogshit but a decent one would require an extremely complicated layout because the structure of Wiktionary entries is hugely variable
        private void ParseWiktionary()
        {
            Dictionary<string, List<string>> wiki_results = new();
            string wiki_langName = String.Join("_", LangName.Split(" "));
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var wiki_page = doc.DocumentNode;

            if(wiki_page.SelectSingleNode("//*[@id=\'"+wiki_langName+"\']") == null)
            {
                NoResultsFound("No " + LangName + " definitions found");
                return;
            }
            else
            {
                string pos = "";
                bool langFlag = true;
                HtmlNode? element = wiki_page.SelectSingleNode("//*[@id=\'" + wiki_langName + "\']").ParentNode.NextElementSibling();

                int[] pos_counters =  [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
                int pos_index = 0;

                while(element != null && langFlag) 
                {
                    if (element.Name != "h2")
                    {
                        if (element.Name == "h4" || element.Name == "h3")
                        {
                            pos = element.SelectSingleNode(".//*[@class=\'mw-headline\']").InnerText;
                            if (pos.Contains("Noun")) { pos_index = 0; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Verb")) { pos_index = 1; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Adverb")) { pos_index = 2; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Adjective")) { pos_index = 3; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Conjunction")) { pos_index = 4; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Preposition")) { pos_index = 5; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Interjection")) { pos_index = 6; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Particle")) { pos_index = 7; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Determiner")) { pos_index = 8; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Pronoun")) { pos_index = 9; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Participle")) { pos_index = 10; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Postposition")) { pos_index = 11; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Letter")) { pos_index = 12; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Predicative")) { pos_index = 13; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Prefix")) { pos_index = 14; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Numeral")) { pos_index = 15; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Article")) { pos_index = 16; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                            else if (pos.Contains("Contraction")) { pos_index = 17; pos_counters[pos_index] = pos_counters[pos_index] + 1; }
                        }

                        if (element.Name == "ol")
                        {
                            List<string> definition_array = [];

                            HtmlNode? element_child = element.FirstElementChild();
                            while(element_child != null)
                            {
                                string def = "";
                                
                                foreach(HtmlNode node in element_child.ChildNodes)
                                {
                                    if (node.Name == "dl" || node.HasClass("nyms-toggle") || node.Name == "ul" || node.HasClass("HQToggle") || node.Name == "ol") {; }
                                    else if(node.HasClass("use-with-mention"))
                                    {
                                        def += "[" + node.InnerText + "]";
                                    }
                                    else
                                    {
                                        def += node.InnerText;
                                    }
                                }
                                def = def.Trim();
                                if(def != "") definition_array.Add(def);
                                element_child = element_child.NextElementSibling();
                            }
                            if(!wiki_results.TryAdd(pos, definition_array))
                            {
                                wiki_results.Add(pos + pos_counters[pos_index].ToString(), definition_array);
                            }
                        }

                    }
                    else
                    {
                        langFlag = false;
                    }
                    element = element.NextElementSibling();                   
                }
                foreach(KeyValuePair<string, List<string>> wiki_result in wiki_results)
                { FormattedString header_text = new();
                    header_text.Spans.Add(new Span { Text = wiki_result.Key, FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontAttributes = FontAttributes.Bold, FontSize = 16 });
                    dict_results.Add(new DictResult(false, true, "", "", header_text));
                    for(int i = 0; i < wiki_result.Value.Count; i++)
                    {
                        dict_results.Add(new DictResult((i % 2 == 0), false, wiki_result.Value[i]));
                    }
                }

            }
        }

        private List<DictResult> dict_results = new();

        public void UrlMaker(/*int lang_id,*/ /*int _dict_index,*/ string dict_query)
        {
            //dict_index = _dict_index;
            string url_base = dict_urls[DictIndex];
            if(DictIndex == 0)
            {
                string PONS_lang = "";
                switch(LangId)
                {
                    case 1:
                        PONS_lang = "russisch-";
                        break;
                    case 3:
                        PONS_lang = "polnisch-";
                        break;
                    case 4:
                        PONS_lang = "bulgarisch-";
                        break;
                    case 5:
                        PONS_lang = "deutsch-";
                        break;
                    case 6:
                        PONS_lang = "schwedisch-";
                        break;
                    case 7:
                        PONS_lang = "t%C3%BCrkisch-";
                        break;
                    case 8:
                        PONS_lang = "d%C3%A4nisch-";
                        break;
                }
                if (LangId == 5) PONS_lang += "englisch/";
                else PONS_lang += "deutsch/";

                url = url_base + PONS_lang + Uri.EscapeDataString(dict_query);
                return;
            }
            else if(DictIndex == 3)
            {
                url = "https://www.dict.cc/?s=" + Uri.EscapeDataString(dict_query);
            }
            else if(DictIndex == 1)
            {
                url = "https://en.wiktionary.org/wiki/" + Uri.EscapeDataString(dict_query);
            }
        }

      


        public async Task<List<DictResult>> GetDictResultsAsync()
        {   dict_results = new();
            html = await GetHTMLAsync();
            
            switch(DictIndex)
            {
                case 0:
                    ParsePONS();
                    break;
                case 1:
                    ParseWiktionary();
                    break;
                case 3:
                    ParseDictCC();
                    break;
                default:
                    ScraperUnavailable();
                    break;
            }
            return dict_results;
        }
    }
}
