using HtmlAgilityPack;

namespace web_crawler
{
    public class Page
    {
        static int counter;
        public string Html { get; set; }
        public int Id { get; set; }
        public string Url { get; set; }


        public Page(string html, string url) {
            
            Html = html;
            Url = url;
            Id = ++counter;
        }
    }
}