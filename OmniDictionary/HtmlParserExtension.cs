﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OmniDictionary
{
    public static class HtmlParserExtension
    {
        public static HtmlNodeCollection SelectNodesOrEmpty(this HtmlNode parent_node, string xpath)
        {
            HtmlNodeCollection nodes = parent_node.SelectNodes(xpath);
            return nodes ?? new HtmlNodeCollection(parent_node);
        }

        public static HtmlNodeCollection GetElementsByClassName(this HtmlNode parent_node, string className)
        {
            string xpath =".//*[contains(concat(' ', normalize-space(@class), ' '), ' " + className + " ')]";
            HtmlNodeCollection nodes = parent_node.SelectNodes(xpath);
            return nodes ?? new HtmlNodeCollection(parent_node);
        }

        public static bool MatchesAnyClass(this HtmlNode parent_node, List<string> classNames)
        {
            foreach (string className in classNames)
            {
                if (parent_node.HasClass(className)) return true;
            }
            return false;
        }

        //stolen from the internet but it's pretty trivial
        public static string NormaliseWhitespace(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            StringBuilder output = new StringBuilder();
            bool skipped = false;

            foreach (char c in input)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!skipped)
                    {
                        output.Append(" ");
                        skipped = true;
                    }
                }
                else
                {
                    skipped = false;
                    output.Append(c);
                }
            }
            return output.ToString();
        }
    }
}
