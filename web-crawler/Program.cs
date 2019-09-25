using System;

namespace web_crawler
{
    class Program
    {
        static void Main(string[] args)
        {
            WebCrawler wb = new WebCrawler("bt.dk");

            //wb.StartCrawlerAsync().Wait();

            wb.ParseText();
        }
    }
}
