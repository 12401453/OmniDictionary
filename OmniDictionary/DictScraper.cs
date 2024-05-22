using HtmlAgilityPack;
using System.Net;
using System.Web;

namespace OmniDictionary
{
    public class DictScraper
    {
        private static readonly HttpClient httpClient = new();
        static readonly List<string> dict_urls = ["https://de.pons.com/%C3%BCbersetzung/", "https://en.wiktionary.org/wiki/", "https://sozdik.kz/translate/kk/ru/", "https://www.dict.cc/?s=", "MR_glossary.url"];

        private string html = "";
        private string url = "";
        private int dict_index = 3;


        //for some reason setting the .Text property of these pre-built Spans and adding them to the FormattedString messes up in a completely inscrutable way; creating the spans on the fly must be inefficient as hell but no other way seems to work properly
        private Span header_span_main = new Span { Text = "", FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontAttributes = FontAttributes.Bold, FontSize = 16 };
        private Span header_span_aux = new Span { Text = "", FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontSize = 14 };

        public DictScraper()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
        }

        private async Task<string> GetHTMLAsync()
        {
            return await httpClient.GetStringAsync(url);

        }

        private void NoResultsFound()
        {
            dict_results.Add(new DictResult(true, false, "No results found", ""));
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

            /*string extractHeaderText(HtmlNodeCollection node_list)
            {
                string text = "";
                bool strong_inserted = false;
                foreach(HtmlNode node in node_list)
                {
                    if(node.NodeType == HtmlNodeType.Element && !node.HasClass("headword_attributes") && !strong_inserted && !node.HasClass("separator"))
                    {
                        text += "<strong>";
                        strong_inserted = true;
                    }
                    else if(node.HasClass("separator"))
                    {
                        text = text.Substring(0, text.Length - 1);
                        text += node.InnerText.Trim();
                    }
                    else if(node.NodeType == HtmlNodeType.Text)
                    {
                        text += node.InnerText.Trim() + " ";
                    }
                    else
                    {
                        text += node.InnerText.Trim() + " ";
                    }
                }
                text += "</strong>";
                return text;
            }*/
            FormattedString extractHeaderText(HtmlNodeCollection node_list)
            {
                FormattedString formatted_text = new FormattedString();
                string text = "";
                foreach(HtmlNode node in node_list)
                {
                    if(node.NodeType == HtmlNodeType.Element && node.Name == "span" && !node.MatchesAnyClass(["headword_attributes", "headword", "headword_spelling"]))
                    {
                        text += "<i>" + node.InnerText + "</i>";
                        header_span_aux.Text = HttpUtility.HtmlDecode(node.InnerText.NormaliseWhitespace());
                        //formatted_text.Spans.Add(header_span_aux);
                        formatted_text.Spans.Add(new Span { Text = HttpUtility.HtmlDecode(node.InnerText.NormaliseWhitespace()), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontSize = 14 });
                    }
                    else if(node.NodeType == HtmlNodeType.Element && node.Name == "span" && node.HasClass("headword_attributes"))
                    {
                        text += node.InnerText; //this block is to get the title= attribute to show HTML title tooltip for certain headwords, but I doubt it will work in MAUI and it is doubtful to be worth implementing a proper tooltip for it so really this whole block is unneccessary
                        header_span_main.Text = node.InnerText.NormaliseWhitespace();
                        formatted_text.Spans.Add(header_span_main);
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
                                break;
                            }
                        }
                        header_span_main.Text = headword_text.NormaliseWhitespace();
                        //formatted_text.Spans.Add(header_span_main);
                        formatted_text.Spans.Add(new Span { Text = headword_text.NormaliseWhitespace(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontAttributes = FontAttributes.Bold, FontSize = 16 });
                    }
                    else if(node.NodeType == HtmlNodeType.Text)
                    {
                        text += node.InnerText;
                        header_span_main.Text = node.InnerText.NormaliseWhitespace();
                        //formatted_text.Spans.Add(header_span_main);
                        formatted_text.Spans.Add(new Span { Text = node.InnerText.NormaliseWhitespace(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontAttributes = FontAttributes.Bold, FontSize = 16 });
                    }
                }

                //return text.Trim().NormaliseWhitespace();
                return formatted_text;
            }

            FormattedString extractH3Text(HtmlNodeCollection node_list)
            {
                FormattedString formatted_text = new FormattedString();

                //if (node_list.Count < 2) return ""; //sometimes the <h3> nodes can be just plain text so get skipped, but in these cases nothing interesting is said anyway
                if (node_list.Count < 2) {
                    //return node_list[0].InnerText.Trim().NormaliseWhitespace();
                    formatted_text.Spans.Add(new Span { Text = node_list[0].InnerText.Trim().NormaliseWhitespace(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontSize = 14 });
                    return formatted_text;
                }
                string text = "";
                foreach( HtmlNode node in node_list)
                {
                    if(node.MatchesAnyClass(["info", "style"]))
                    {
                        text += "<abbr>" + node.InnerText.Trim() + "</abbr> "; //convert to FormattedString and make the text greyer to match the website
                        formatted_text.Spans.Add(new Span { Text = node.InnerText.Trim().NormaliseWhitespace(), FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4be"), FontSize = 14, FontAttributes = FontAttributes.Italic });
                    }
                    else
                    {
                        text += node.InnerText.Trim() + " ";
                        formatted_text.Spans.Add(new Span { Text = HttpUtility.HtmlDecode(node.InnerText.Trim().NormaliseWhitespace()) + " ", FontFamily = "IBMPlexSans", TextColor = Color.FromRgba("#cbd9f4"), FontSize = 14 });
                    }
                }
                //return text;
                return formatted_text;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var PONS_page = doc.DocumentNode;

            HtmlNodeCollection meaning_sections = PONS_page.GetElementsByClassName("rom"); //the XPath "*[@class=\'rom\']" fails if multiple classes are assigned as in this case; the fact that HtmlAgilityPack doesn't by default replicate exactly the functionality of the JS querySelectors is pretty stupid
            int rom_lngth = meaning_sections.Count;

            //this is for when PONS probably doesn't have an exact entry for the word but does have the word included in other example sentences/entries
            if(rom_lngth == 0)
            {
                HtmlNodeCollection results_sections = PONS_page.GetElementsByClassName("results");//SelectNodes("//*[contains(concat(' ', normalize-space(@class), ' '), ' results ')]");
                if(results_sections.Count == 0) 
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

        private List<DictResult> dict_results = new();

        public void UrlMaker(int lang_id, int _dict_index, string dict_query)
        {
            dict_index = _dict_index;
            string url_base = dict_urls[dict_index];
            if(dict_index == 0)
            {
                string PONS_lang = "";
                switch(lang_id)
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
                if (lang_id == 5) PONS_lang += "englisch/";
                else PONS_lang += "deutsch/";

                url = url_base + PONS_lang + Uri.EscapeDataString(dict_query);
                return;
            }
            else if(dict_index == 3)
            {
                url = "https://www.dict.cc/?s=" + Uri.EscapeDataString(dict_query);
            }
        }

      


        public async Task<List<DictResult>> GetDictResultsAsync()
        {   dict_results = new();
            html = await GetHTMLAsync();
            
            switch(dict_index)
            {
                case 0:
                    ParsePONS();
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
