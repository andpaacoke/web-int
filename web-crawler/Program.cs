using System;

namespace web_crawler
{
    class Program
    {
        static void Main(string[] args)
        {
            WebCrawler wb = new WebCrawler("ekstrabladet.dk");

            wb.StartCrawlerAsync().Wait();
        }
    }
}
