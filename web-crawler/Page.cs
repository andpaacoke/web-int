using System.Collections.Generic;
using HtmlAgilityPack;

namespace web_crawler
{
    public class Page
    {
        static int counter;
        public string Html { get; set; }
        public int Id { get; set; }
        public string Url { get; set; }
        public List<string> OutDegreeLinks { get; set; }


        public Page(string html, string url, List<string> outDegreeLinks) {

            OutDegreeLinks = outDegreeLinks;
            Html = html;
            Url = url;
            Id = ++counter;
        }
    }
}