using System;

namespace web_crawler
{
    class Program
    {
        static void Main(string[] args)
        {
            WebCrawler wc = new WebCrawler("bt.dk");

            // wb.StartCrawlerAsync().Wait();

            wc.ParseText();
        }
    }
}
