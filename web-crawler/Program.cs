using System;

namespace web_crawler
{
    class Program
    {
        static void Main(string[] args)
        {
            WebCrawler wc = new WebCrawler("bt.dk");

            //wc.StartCrawlerAsync().Wait();

            wc.ParseText();

            Console.WriteLine("Please write a query: ");
            string userQuery = Console.ReadLine();
            wc.HandleUserQuery(userQuery);

            
        }
    }
}
