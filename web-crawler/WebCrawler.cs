using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Net;

namespace web_crawler
{
    public class WebCrawler
    {
        private string _seed { get; set; }
        public WebCrawler(string seed) {
            string url = String.Format("http://{0}", seed);
            _seed = url;
        }

        public async Task<string> StartCrawlerAsync()
        {
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(_seed);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            List<string> hrefs = new List<string>();


            for (int i = 0; i <= 5; i++)
            {

                var links = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
                foreach (HtmlNode hn in links)
                {
                    hrefs.Add(hn.Attributes["href"].Value);
                }

                foreach (string href in hrefs)
                {
                    await Task.Delay(2000);
                    try
                    {
                        if (href.StartsWith("http") || href.StartsWith("//www"))
                        {
                            html = await httpClient.GetStringAsync(href);
                            htmlDocument.LoadHtml(html);
                            
                        }
                        else
                        {
                            var formattedString = String.Format("{0}{1}", _seed, href);
                            html = await httpClient.GetStringAsync(formattedString);
                            htmlDocument.LoadHtml(html);
                        }
                        Console.WriteLine("Ez clap");

                        using (System.IO.StreamWriter file =
                       new System.IO.StreamWriter(Directory.GetCurrentDirectory() + "//html.txt"))
                        {
                            Console.WriteLine(href);
                            file.WriteLine(html);
                            file.WriteLine("----------------------------------------------------");
                        }

                    }

                    catch (WebException ex)
                    {
                        Console.WriteLine(ex.Message);
                        HttpWebResponse errorResponse = ex.Response as HttpWebResponse;
                        if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                        {
                            hrefs.Remove(href);
                        }

                        
                    }


                }

            }

            return html;

            ParseRobotstxt(htmlDocument, "BingBangBot");

        }
        

        // Fra lektion 2
        private async void ParseRobotstxt(HtmlDocument htmlDocument, string botName)
        {
            var listOfRobotstxt = htmlDocument.Text.ToLower().Split("\n").ToList();
            List<string> restrictions = new List<string>();

            for (int i = 0; i < listOfRobotstxt.Count; i++)
            {
                if (listOfRobotstxt[i].StartsWith("user-agent: " + botName) || listOfRobotstxt[i].StartsWith("user-agent: *"))
                {
                    i++;
                    while (!string.IsNullOrWhiteSpace(listOfRobotstxt[i]))
                    {
                        restrictions.Add(listOfRobotstxt[i]);
                        i++;
                    }
                }
            }
            foreach (var item in restrictions)
            {
                Console.WriteLine(item);
            }
        }


        // Fra lektion 1
        public void NearDuplicate(string stringOne, string stringTwo, int shingle){

            var stringOneWords = stringOne.Split();
            var stringTwoWords = stringTwo.Split();

            List<string> shingleSetOne = new List<string>();
            List<string> shingleSetTwo = new List<string>();
            
            string temp = "";

            for(int i = 0; i < stringOneWords.Length - shingle +1; i++){
                for(int j = 0; j < shingle; j++) {
                    if(i + j < stringOneWords.Length) {
                        temp += stringOneWords[i + j] + " ";
                    }
                }
                shingleSetOne.Add(temp);
                temp = "";
            }

            for(int i = 0; i < stringTwoWords.Length - shingle +1; i++){
                for(int j = 0; j < shingle; j++) {
                    if(i + j < stringTwoWords.Length) {
                        temp += stringTwoWords[i + j] + " ";
                    }
                }
                shingleSetTwo.Add(temp);
                temp = "";
            }

            Console.WriteLine(Jaccard(shingleSetOne.ToArray(), shingleSetTwo.ToArray()).ToString());
        }

        // Fra lektion 1
        public double Jaccard(string[] a, string[] b){

            IEnumerable<string> union = a.Union(b);
            IEnumerable<string> overlap = a.Intersect(b);
            
            var simi =  (double) overlap.ToList().Count / (double)union.ToList().Count;
            return simi;
        }
    }
}