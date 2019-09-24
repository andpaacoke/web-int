using HtmlAgilityPack;

namespace web_crawler
{
    public class Page
    {
        static int counter;
        public HtmlDocument HtmlDoc { get; set; }
        public int Id { get; set; }

        public string Url { get; set; }


        public Page(HtmlDocument htmlDoc, string url) {
            
            HtmlDoc = htmlDoc;
            Url = url;
            Id = counter++;
        }
    }
}