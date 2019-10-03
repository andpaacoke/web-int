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
        // How many times a term appear in total in all docs
        public Dictionary<string, int> TermFrequency {get; set;}
        // How many times each term appears per document
        public List<Dictionary<string, int>> TermDocFrequency {get; set;}
        // List of documents that contain the specific term
        public Dictionary<string, List<int>> PostingList {get; set;}
        // Count for how many different documents a term appears in
        public Dictionary<string, int> DocumentFrequency {get; set;}
        // A measure of the informativeness of the each term.
        public Dictionary<string, double> InverseDocumentFrequency {get; set;}
        public List<Dictionary<string, double>> TfIdfWeight {get; set;}

        
        public WebCrawler(string seed) {
            string url = String.Format("http://{0}", seed);
            _seed = url;
            Pages = new List<Page>();
            TermFrequency = new Dictionary<string, int>();
            TermDocFrequency = new List<Dictionary<string, int>>();
            PostingList = new Dictionary<string, List<int>>();
            DocumentFrequency = new Dictionary<string, int>();
            InverseDocumentFrequency = new Dictionary<string, double>();
            TfIdfWeight = new List<Dictionary<string, double>>();
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

                        //IF we can crawl we store the pages html and url.
                        if (IsAllowedToCrawl(href, restrictions))
                        {
                            html = await httpClient.GetStringAsync(href);
                            htmlDocument.LoadHtml(html);
                            string parsedHtml = htmlDocument.DocumentNode.SelectSingleNode("//body").InnerText;
                            parsedHtml = parsedHtml.Replace("\n", " ");
                            // Removes ekstra whitespace
                            parsedHtml = Regex.Replace(parsedHtml, @"\s+", " ");
                            
                            if(IsPageUnique(parsedHtml)) {
                                Pages.Add(new Page(parsedHtml, href));
                                Console.WriteLine("Added page number " + Pages.Count);
                            }
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
        private bool NearDuplicate(string stringOne, string stringTwo, int shingle){

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

            double jaccardValue= Jaccard(shingleSetOne.ToArray(), shingleSetTwo.ToArray());
            return jaccardValue > 0.8 ? true : false;
        }

        // Fra lektion 1
        private double Jaccard(string[] a, string[] b){

            IEnumerable<string> union = a.Union(b);
            IEnumerable<string> overlap = a.Intersect(b);
            
            var simi =  (double) overlap.ToList().Count / (double)union.ToList().Count;
            return simi;
        }

        private bool IsPageUnique(string parsedHtml)
        {
            foreach (Page p in Pages)
            {
                if(NearDuplicate(parsedHtml, p.Html, 4)) {
                    return false;
                }
            }
            return true;
        }

        public void ParseText()
        {
            List<Term> terms = new List<Term>();
            
            
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

            PopulateOverallTermFrequency(sortedTerms);
            PopulatePostingList(sortedTerms);
            PopulateTermDocFrequency(sortedTerms);
            PopulateDocumentFrequency(sortedTerms);
            PopulateInverseDocumentFrequency();
            CalculateTfIdfWeight();

        }

        private void PopulateDocumentFrequency(List<Term> sortedTerms) {

            for (int i = 0; i < sortedTerms.Count; i++)
            {
                if(DocumentFrequency.ContainsKey(sortedTerms[i].Word)) {
                    if (sortedTerms[i].Id == sortedTerms[i - 1].Id) {
                        continue;
                    }
                    else {
                        DocumentFrequency[sortedTerms[i].Word]++;
                    }
                }
                else
                {
                    DocumentFrequency.Add(sortedTerms[i].Word, 1);
                }
            }
        }

        private void PopulateInverseDocumentFrequency(){
            var docCount = GetDocCount();

            foreach (KeyValuePair<string, int> entry in DocumentFrequency)
            {
                var initValue = docCount/entry.Value;
                var idf = Math.Log10(initValue);
                InverseDocumentFrequency.Add(entry.Key, idf);
            }
        }

        private void PopulateOverallTermFrequency(List<Term> sortedTerms)
        {
            for (int i = 0; i < sortedTerms.Count; i++)
            {
                // If the dictionary contains the term, we want to increment for each document it appears on.
                // We do this by comparing the id to the id of the previous element. If they differ we increment.
                if (TermFrequency.ContainsKey(sortedTerms[i].Word))
                {
                    TermFrequency[sortedTerms[i].Word]++;
                }
                else
                {
                    TermFrequency.Add(sortedTerms[i].Word, 1);
                }
            }
        }

        private void PopulatePostingList(List<Term> sortedTerms)
        {
            for (int i = 0; i < sortedTerms.Count; i++)
            {
                if (PostingList.ContainsKey(sortedTerms[i].Word))
                {
                    if (!PostingList[sortedTerms[i].Word].Contains(sortedTerms[i].Id))
                    {
                        PostingList[sortedTerms[i].Word].Add(sortedTerms[i].Id);
                    }
                }
                else
                {
                    PostingList.Add(sortedTerms[i].Word, new List<int>() { sortedTerms[i].Id });
                }
            }
        }

        private void PopulateTermDocFrequency(List<Term> sortedTerms) {
            int docCount = GetDocCount();

            for(int i = 0; i < docCount; i++) {
                TermDocFrequency.Add(new Dictionary<string, int>());
            }

            for (int j = 0; j < sortedTerms.Count - 1; j++)
            {
                if(TermDocFrequency[sortedTerms[j].Id - 1].ContainsKey(sortedTerms[j].Word)){
                    TermDocFrequency[sortedTerms[j].Id - 1][sortedTerms[j].Word]++;
                } 
                else {
                    TermDocFrequency[sortedTerms[j].Id - 1].Add(sortedTerms[j].Word, 1);
                }
            }
        }

        private void CalculateTfIdfWeight()
        {
            int docCount = GetDocCount();

            for(int i = 0; i < docCount; i++) {
                TfIdfWeight.Add(new Dictionary<string, double>());
            }

            for (int i = 0; i < TermDocFrequency.Count - 1; i++)
            {
                foreach (KeyValuePair<string, int> entry in TermDocFrequency[i])
                {
                    var tfidf = entry.Value * InverseDocumentFrequency[entry.Key];
                    TfIdfWeight[i].Add(entry.Key, tfidf);
                }
            }
        }

        private int GetDocCount () {
            string[] lines = System.IO.File.ReadAllLines(Directory.GetCurrentDirectory() + "//html.txt");
            var index = lines.Length - 1;

            while(true) {
                if(lines[index].StartsWith("Id: ")) {
                    var lineSplit = lines[index].Split(" ");
                    return Convert.ToInt32(lineSplit[1]);
                }
                else {
                    index--;
                }
            }
        }
       
        private void WriteToFile() {
        using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(Directory.GetCurrentDirectory() + "//html.txt"))
            {
                foreach(Page p in Pages) {
                    file.WriteLine("Id: " + p.Id + " Url: " + p.Url);
                    file.WriteLine(p.Html);
                    file.WriteLine();
                }
            }
        }
    }
}