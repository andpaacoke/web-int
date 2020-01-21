using System;

namespace web_crawler
{
    class Program
    {
        static void Main(string[] args)
        {
            WebCrawler wc = new WebCrawler("bt.dk");

            // wc.StartCrawlerAsync2().Wait();

            wc.ParseText();

            while (true)
            {
                Console.WriteLine("Please write a query: ");
                string userQuery = Console.ReadLine();
                if(userQuery != "") {
                    wc.HandleUserQuery(userQuery);
                } else {
                    Console.WriteLine("Query must contain words!");
                }
            }

            
        }

        
    }
}
