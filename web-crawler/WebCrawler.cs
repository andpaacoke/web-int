using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace web_crawler
{
    public class WebCrawler
    {
        private string _seed { get; set; }
        public List<Page> Pages { get; set; }
        public WebCrawler(string seed) {
            string url = String.Format("http://{0}", seed);
            _seed = url;
            Pages = new List<Page>();
        }

        public async Task<string> StartCrawlerAsync()
        {
            // Loads initial html from the seed provided
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(_seed);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            // Creates a baseUrl for comparison purposes for the queue so that we know to not enter that page again
            string baseUrl = new Uri(_seed).Host;
            List<string> restrictions = await ParseRobotstxt(baseUrl, "BingBangBot");
            List<string> hrefs = new List<string>();

            // When a new baseurl is encountered we add it to a list that should be crawled
            List<string> UrlsToCrawl = new List<string>();

            // seedUrl is added to list and we start index at 1 such that the loop will start from the next baseurl.
            UrlsToCrawl.Add(new Uri(_seed).Host);
            int UrlsToCrawlIndex = 1;


            while (Pages.Count < 1000)
            {
                // This loop ensures that we traverse the list of baseUrls to crawl after we finished the seed. 
                for (; UrlsToCrawlIndex < UrlsToCrawl.Count; UrlsToCrawlIndex++)
                {
                    try
                    {
                        restrictions = await ParseRobotstxt(new Uri(UrlsToCrawl[UrlsToCrawlIndex]).Host, "BingBangBot");
                        if (IsAllowedToCrawl(UrlsToCrawl[UrlsToCrawlIndex], restrictions))
                        {
                            // If allowed to crawl the url we load the html and increment the list.
                            html = await httpClient.GetStringAsync(UrlsToCrawl[UrlsToCrawlIndex]);
                            htmlDocument.LoadHtml(html);
                            UrlsToCrawlIndex++;
                            break;
                        }

                    }
                    catch (Exception ex)
                    {
                        // We will occasionally encounter exceptions such as when a url does not have a robots file. This continues.
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(UrlsToCrawl[UrlsToCrawlIndex]);
                        continue;
                    }
                }

                // This finds all links in the html
                var links = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
                hrefs = new List<string>();

                //If we find links we go thorugh them and add the ones that are usable to a new list
                if (links != null)
                {
                    foreach (HtmlNode hn in links)
                    {
                        if (hn.Attributes["href"].Value.StartsWith("http"))
                        {
                            hrefs.Add(hn.Attributes["href"].Value);
                        }
                    }
                    Console.WriteLine("Loaded new href links!");
                }

                //FOr the loaded links we iterate through them all.
                foreach (string href in hrefs)
                {
                    try
                    {
                        Console.WriteLine(href);

                        //We make a tempUrl to compare our baseUrl with
                        string tempBaseUrl = new Uri(href).Host;
                        if(tempBaseUrl.StartsWith("www.")) {
                            tempBaseUrl = tempBaseUrl.Replace("www.", "");
                        }
                        if(baseUrl != tempBaseUrl) {
                            // If they differ we know we have encountered a new site. We then format the string to work, and add it to the list to be crawled.
                            baseUrl = tempBaseUrl;
                            var formattedString = String.Format("{0}{1}", "https://", baseUrl);
                            if(!UrlsToCrawl.Contains(formattedString)) {
                                UrlsToCrawl.Add(formattedString);
                            }
                            restrictions = await ParseRobotstxt(baseUrl, "BingBangBot");
                            
                        }

                        if (IsAllowedToCrawl(href, restrictions))
                        {
                            html = await httpClient.GetStringAsync(href);
                            htmlDocument.LoadHtml(html);
                            //IF we can crawl we store the pages html and url.
                            Pages.Add(new Page(htmlDocument, href));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(href);
                        continue;
                    }
                }
            }
            
            WriteToFile();
            return html;
        }



        private bool IsAllowedToCrawl(string url, List<string> restrictions) {
            // First check for sites that are allowed
            foreach(string rule in restrictions) {
                if(rule.StartsWith("allow")) {
                    var subString = rule.Substring(rule.LastIndexOf(':') + 1).Trim();
                    if(subString.StartsWith('/')) {
                        if(url.Contains(subString))  {
                            return true;
                        }
                    }
                }
            }

            // Check for sites that are disallowed
            foreach(string rule in restrictions) {
                if(rule.StartsWith("disallow")) {
                    var subString = rule.Substring(rule.LastIndexOf(':') + 1).Trim();

                    if(subString.StartsWith("/*")) {
                        subString = subString.Substring(subString.LastIndexOf('*') + 1).Trim();
                        if(url.EndsWith(subString))  {
                            return false;
                        }
                    } else if(subString.StartsWith("/")) {
                        if(url.Contains(subString))  {
                            return false;
                        }
                    }        
                }
            }

            return true;
        }


        // Fra lektion 2
        private async Task<List<string>> ParseRobotstxt(string baseUrl, string botName)
        {
            var robotsUrl = String.Format("http://{0}/robots.txt", baseUrl);
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(robotsUrl);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
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

            return restrictions;
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


        public Dictionary<string, int> ParseText()
        {
            List<Term> terms = new List<Term>();
            Dictionary<string, int> docIdDictionary = new Dictionary<string, int>();
            
            string[] lines = System.IO.File.ReadAllLines(Directory.GetCurrentDirectory() + "//html.txt");
            int Id = 1;

            foreach(string line in lines) {
                if(line.StartsWith("Id: " + Id.ToString())) {
                    Id++;
                    continue;
                }
                string currentLine = Regex.Replace(line,"[^A-Za-z]"," ").ToLower();
                string[] lineSplit = currentLine.Split(" ");

                foreach(string s in lineSplit) {
                    if(!String.IsNullOrWhiteSpace(s)){
                        terms.Add(new Term(s, Id - 1));
                    }
                }
            }

            var sortedTerms = terms.OrderBy(x => x.Word ).ThenBy(term => term.Id).ToList();
            
            for (int i = 0; i < sortedTerms.Count; i++) {
                // If the dictionary contains the term, we want to increment for each document it appears on.
                // We do this by comparing the id to the id of the previous element. If they differ we increment.
                if (docIdDictionary.ContainsKey(sortedTerms[i].Word)) {
                    if (sortedTerms[i].Id != sortedTerms[i-1].Id) {
                        docIdDictionary[sortedTerms[i].Word]++;
                    }
                }
                else {
                    docIdDictionary.Add(sortedTerms[i].Word, 1);
                }
            }

            return docIdDictionary;

        }

        

        private void WriteToFile() {
        using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(Directory.GetCurrentDirectory() + "//html.txt"))
            {
                foreach(Page p in Pages) {
                    file.WriteLine("Id: " + p.Id + " Url: " + p.Url);
                    file.WriteLine(p.HtmlDoc.DocumentNode.SelectSingleNode("//body").InnerText);
                    file.WriteLine();
                }
            }
        }
    }
}