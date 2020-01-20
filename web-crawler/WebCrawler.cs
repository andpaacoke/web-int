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
using MathNet.Numerics.LinearAlgebra;

namespace web_crawler
{
    // Shortcuts: We generally do not consider stop words. 
    // Delay in robots.txt is not considered

    public class WebCrawler
    {
        private string _seed { get; set; }
        public List<Page> Pages { get; set; }
        // How many times a term appear in total in all docs
        public Dictionary<string, int> TermFrequency { get; set; }
        // How many times each term appears per document
        public List<Dictionary<string, int>> TermDocFrequency { get; set; }
        // List of documents that contain the specific term
        public Dictionary<string, List<int>> PostingList { get; set; }
        // Count for how many different documents a term appears in
        public Dictionary<string, int> DocumentFrequency { get; set; }
        // A measure of the informativeness of the each term.
        public Dictionary<string, double> InverseDocumentFrequency { get; set; }
        // List containing each document's tfidf vector
        public List<Dictionary<string, double>> TfIdfWeight { get; set; }
        // Dictionary containing the query's tfidf vector
        public Dictionary<string, double> QueryTfIdfWeight { get; set; }
        public List<string> VisitedHrefs { get; set; }
        public List<string> UrlsToVisit = new List<string>();
        // List containing each document's normalized tfidf vector
        public List<Dictionary<string, double>> NormalizedTfIdfDocumentVectors { get; set; }


        public WebCrawler(string seed)
        {
            _seed = String.Format("http://{0}", seed);
            Pages = new List<Page>();
            TermFrequency = new Dictionary<string, int>();
            TermDocFrequency = new List<Dictionary<string, int>>();
            PostingList = new Dictionary<string, List<int>>();
            DocumentFrequency = new Dictionary<string, int>();
            InverseDocumentFrequency = new Dictionary<string, double>();
            TfIdfWeight = new List<Dictionary<string, double>>();
            QueryTfIdfWeight = new Dictionary<string, double>();

            VisitedHrefs = new List<string>();
            NormalizedTfIdfDocumentVectors = new List<Dictionary<string, double>>();
        }

        public async Task StartCrawlerAsync()
        {
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(_seed);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            // Creates a baseUrl for comparison purposes for the queue so that we know to not enter that page again
            string baseUrl = new Uri(_seed).Host;
            List<string> restrictions = await ParseRobotstxt(baseUrl, "BingBangBot");
            List<string> hrefs = new List<string>();


            // seedUrl is added to list and we start index at 1 such that the loop will start from the next baseurl.
            UrlsToVisit.Add(_seed);
            int UrlsToCrawlIndex = 0;

            List<Task> tasks = new List<Task>();
            int threadsStarted = 0;
            while (Pages.Count < 1000)
            {
                if (UrlsToVisit.Count > threadsStarted)
                {
                    threadsStarted++;
                    tasks.Add(Run(UrlsToVisit[UrlsToCrawlIndex]));
                    UrlsToCrawlIndex++;
                }
            }
            Task.WaitAll(tasks.ToArray());
            System.Console.WriteLine("done with all");
            WriteToFile();
        }

        private async Task Run(string url)
        {
            var httpClient = new HttpClient();
            var htmlDocument = new HtmlDocument();
            string baseUrl;
            string html;
            List<string> restrictions = new List<string>();
            try
            {
                baseUrl = new Uri(url).Host;
            }
            catch (System.Exception)
            {
                return;
            }
            try
            {
                restrictions = await ParseRobotstxt(baseUrl, "BingBangBot");
                if (IsAllowedToCrawl(url, restrictions))
                {
                    // If allowed to crawl the url we load the html and increment the list.
                    html = await httpClient.GetStringAsync(url);
                    htmlDocument.LoadHtml(html);
                }

            }
            catch (Exception ex)
            {
                // We will occasionally encounter exceptions such as when a url does not have a robots file. This continues.
                Console.WriteLine(ex.Message);
                Console.WriteLine(url);
            }
            // This finds all links in the html
            var links = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
            var hrefs = new List<string>();

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
            }

            //FOr the loaded links we iterate through them all.
            foreach (string href in hrefs)
            {
                if (VisitedHrefs.Contains(href))
                {
                    continue;
                }
                try
                {
                    if (!UrlsToVisit.Contains(href))
                    {
                        UrlsToVisit.Add(href);
                    }
                    //We make a tempUrl to compare our baseUrl with
                    string tempBaseUrl = new Uri(href).Host;
                    if (tempBaseUrl.StartsWith("www."))
                    {
                        tempBaseUrl = tempBaseUrl.Replace("www.", "");
                    }
                    if (baseUrl != tempBaseUrl)
                    {
                        // If they differ we know we have encountered a new site. We then format the string to work, and add it to the list to be crawled.
                        baseUrl = tempBaseUrl;
                        var formattedString = String.Format("{0}{1}", "https://", baseUrl);

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
                        if (Pages.Count > 1000)
                        {
                            return;
                        }
                        if (IsPageUnique(parsedHtml))
                        {
                            var pageLinks = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
                            List<string> OutDegreeLinks = new List<string>();
                            foreach (HtmlNode hn in pageLinks)
                            {
                                OutDegreeLinks.Add(hn.Attributes["href"].Value);
                            }
                            Pages.Add(new Page(parsedHtml, href, hrefs));
                            Console.WriteLine("Added page number " + Pages.Count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(href);
                    VisitedHrefs.Add(href);
                    continue;
                }

                VisitedHrefs.Add(href);
                System.Console.WriteLine("thread done");
            }
        }


        private bool IsAllowedToCrawl(string url, List<string> restrictions)
        {
            // First check for sites that are allowed
            foreach (string rule in restrictions)
            {
                if (rule.StartsWith("allow"))
                {
                    var subString = rule.Substring(rule.LastIndexOf(':') + 1).Trim();
                    if (subString.StartsWith('/'))
                    {
                        if (url.Contains(subString))
                        {
                            return true;
                        }
                    }
                }
            }

            // Check for sites that are disallowed
            foreach (string rule in restrictions)
            {
                if (rule.StartsWith("disallow"))
                {
                    var subString = rule.Substring(rule.LastIndexOf(':') + 1).Trim();

                    if (subString.StartsWith("/*"))
                    {
                        subString = subString.Substring(subString.LastIndexOf('*') + 1).Trim();
                        if (url.EndsWith(subString))
                        {
                            return false;
                        }
                    }
                    else if (subString.StartsWith("/"))
                    {
                        if (url.Contains(subString))
                        {
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
        private bool NearDuplicate(string stringOne, string stringTwo, int shingle)
        {

            var stringOneWords = stringOne.Split();
            var stringTwoWords = stringTwo.Split();

            List<string> shingleSetOne = new List<string>();
            List<string> shingleSetTwo = new List<string>();

            string temp = "";

            for (int i = 0; i < stringOneWords.Length - shingle + 1; i++)
            {
                for (int j = 0; j < shingle; j++)
                {
                    if (i + j < stringOneWords.Length)
                    {
                        temp += stringOneWords[i + j] + " ";
                    }
                }
                shingleSetOne.Add(temp);
                temp = "";
            }

            for (int i = 0; i < stringTwoWords.Length - shingle + 1; i++)
            {
                for (int j = 0; j < shingle; j++)
                {
                    if (i + j < stringTwoWords.Length)
                    {
                        temp += stringTwoWords[i + j] + " ";
                    }
                }
                shingleSetTwo.Add(temp);
                temp = "";
            }

            double jaccardValue = Jaccard(shingleSetOne.ToArray(), shingleSetTwo.ToArray());
            return jaccardValue > 0.8 ? true : false;
        }

        // Fra lektion 1
        private double Jaccard(string[] a, string[] b)
        {

            IEnumerable<string> union = a.Union(b);
            IEnumerable<string> overlap = a.Intersect(b);

            var simi = (double)overlap.ToList().Count / (double)union.ToList().Count;
            return simi;
        }

        //Use Jaccard to find simliar pages
        private bool IsPageUnique(string parsedHtml)
        {
            foreach (Page p in Pages)
            {
                if (NearDuplicate(parsedHtml, p.Html, 4))
                {
                    return false;
                }
            }
            return true;
        }

        // Parse the textfile to find unique terms
        public void ParseText()
        {
            List<Term> terms = new List<Term>();


            string[] lines = System.IO.File.ReadAllLines(Directory.GetCurrentDirectory() + "//html.txt");
            int Id = 1;

            foreach (string line in lines)
            {
                if (line.StartsWith("Id: " + Id.ToString()))
                {
                    Id++;
                    continue;
                }
                string currentLine = Regex.Replace(line, "[^A-Za-z]", " ").ToLower();
                string[] lineSplit = currentLine.Split(" ");

                foreach (string s in lineSplit)
                {
                    if (!String.IsNullOrWhiteSpace(s))
                    {
                        terms.Add(new Term(s, Id - 1));
                    }
                }
            }

            var sortedTerms = terms.OrderBy(x => x.Word).ThenBy(term => term.Id).ToList();

            /*             //Terms in all documents
                        PopulateOverallTermFrequency(sortedTerms);

                        //Save a list of document id's that each term is precent in
                        PopulatePostingList(sortedTerms);

                        // Number of times a term appears on a single document
                        PopulateTermDocFrequency(sortedTerms);

                        // Number of documents a term appears in
                        PopulateDocumentFrequency(sortedTerms);

                        // Logarithm applied on docfreq df
                        PopulateInverseDocumentFrequency();

                        //Calculates the tfIdf weights for the terms in the pages
                        CalculateTfIdfWeight();

                        // Normalizes document vectors so they can be compared to the query
                        NormaliseDocumentVectors();
             */
            ReadAllPages();
            //GetSiteOutDegree();
            ReadOutDegreeLinks();
            var transitionProbabilityMatrix = CreateTransitionProbabilityMatrix();
            Vector<double> probabilityDistribution = Vector<double>.Build.Dense(transitionProbabilityMatrix.RowCount);
            probabilityDistribution[0] = 1.0;
            double sum = 0;
            double totalSum = 0;
            for (int j = 0; j < transitionProbabilityMatrix.RowCount; j++)
            {
                sum = transitionProbabilityMatrix.Row(j).Sum();
                if(sum < 0.98) {
                    Console.WriteLine($"Row {j} har sum {sum}");
                }
                totalSum += sum;
            }   
            Console.WriteLine(totalSum / transitionProbabilityMatrix.RowCount);
            for(int i = 0; i < 20; i++) {
                probabilityDistribution = ComputeTransition(transitionProbabilityMatrix, probabilityDistribution);
            }
            Console.WriteLine(probabilityDistribution.Sum());
            
        }

        private void PopulateDocumentFrequency(List<Term> sortedTerms)
        {
            // We check for each term whether or not it is in the document. If the id of the document is the same as the id of the previous element we skip as we only count unique documents.
            for (int i = 0; i < sortedTerms.Count; i++)
            {
                if (DocumentFrequency.ContainsKey(sortedTerms[i].Word))
                {
                    if (sortedTerms[i].Id == sortedTerms[i - 1].Id)
                    {
                        continue;
                    }
                    else
                    {
                        DocumentFrequency[sortedTerms[i].Word]++;
                    }
                }
                else
                {
                    DocumentFrequency.Add(sortedTerms[i].Word, 1);
                }
            }
        }

        private void PopulateInverseDocumentFrequency()
        {
            var docCount = GetDocCount();

            foreach (KeyValuePair<string, int> entry in DocumentFrequency)
            {
                var initValue = docCount / entry.Value;
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
            // We store the list of ID's containing a term key.
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

        private void PopulateTermDocFrequency(List<Term> sortedTerms)
        {
            int docCount = GetDocCount();

            for (int i = 0; i < docCount; i++)
            {
                TermDocFrequency.Add(new Dictionary<string, int>());
            }

            for (int j = 0; j < sortedTerms.Count - 1; j++)
            {
                if (TermDocFrequency[sortedTerms[j].Id - 1].ContainsKey(sortedTerms[j].Word))
                {
                    TermDocFrequency[sortedTerms[j].Id - 1][sortedTerms[j].Word]++;
                }
                else
                {
                    TermDocFrequency[sortedTerms[j].Id - 1].Add(sortedTerms[j].Word, 1);
                }
            }
        }

        private void CalculateTfIdfWeight()
        {
            int docCount = GetDocCount();

            for (int i = 0; i < docCount; i++)
            {
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

        private int GetDocCount()
        {
            string[] lines = System.IO.File.ReadAllLines(Directory.GetCurrentDirectory() + "//html.txt");
            var index = lines.Length - 1;

            while (true)
            {
                if (lines[index].StartsWith("Id: "))
                {
                    var lineSplit = lines[index].Split(" ");
                    return Convert.ToInt32(lineSplit[1]);
                }
                else
                {
                    index--;
                }
            }
        }

        private void WriteToFile()
        {
            using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(Directory.GetCurrentDirectory() + "//pages.txt"))
            {
                foreach (Page p in Pages)
                {
                    file.WriteLine("Id: " + p.Id + " Url: " + p.Url);
                    file.WriteLine(p.Html);
                    file.WriteLine();
                }
            }
        }

        // Split the query and count the terms.
        private Dictionary<string, int> CalculateQueryTermFrequency(string userQuery)
        {
            var splitQuery = userQuery.Split(" ");
            Dictionary<string, int> queryTermFrequency = new Dictionary<string, int>();
            foreach (string word in splitQuery)
            {
                if (queryTermFrequency.ContainsKey(word))
                {
                    queryTermFrequency[word]++;
                }
                else
                {
                    queryTermFrequency.Add(word, 1);
                }
            }
            return queryTermFrequency;
        }

        // We use the term frequency of the query and the IDF value we previously calculated.
        private void CalculateQueryTfIdf(string userQuery)
        {
            QueryTfIdfWeight = new Dictionary<string, double>();
            var queryTermFrequency = CalculateQueryTermFrequency(userQuery);
            foreach (KeyValuePair<string, int> entry in queryTermFrequency)
            {
                if (InverseDocumentFrequency.ContainsKey(entry.Key))
                {
                    var tfidf = entry.Value * InverseDocumentFrequency[entry.Key];
                    QueryTfIdfWeight.Add(entry.Key, tfidf);
                }
                else
                {
                    QueryTfIdfWeight.Add(entry.Key, 0);
                }
            }
        }

        private Dictionary<string, double> NormaliseQueryVector()
        {
            double vectorLength = 0;
            Dictionary<string, double> normalisedQueryVector = new Dictionary<string, double>();

            foreach (KeyValuePair<string, double> entry in QueryTfIdfWeight)
            {
                vectorLength += (entry.Value * entry.Value);
            }
            vectorLength = Math.Sqrt(vectorLength);

            foreach (KeyValuePair<string, double> entry in QueryTfIdfWeight)
            {
                normalisedQueryVector.Add(entry.Key, entry.Value / vectorLength);
            }

            return normalisedQueryVector;
        }

        public void HandleUserQuery(string userQuery)
        {
            userQuery = userQuery.ToLower();
            CalculateQueryTfIdf(userQuery);
            var normalisedQueryVector = NormaliseQueryVector();
            var sortedCosineScore = CalculateCosineSimilarity(normalisedQueryVector, NormalizedTfIdfDocumentVectors);
            var topKPages = TakeTopKResults(sortedCosineScore, 10);
            PrintTopResults(topKPages, userQuery.Split(" "));
        }

        //Cosine similarity is calculated by taking the dot product of the vectors. This means entries with identical keys should be multiplied, 
        // and all these values summed together.
        private IOrderedEnumerable<KeyValuePair<int, double>> CalculateCosineSimilarity(Dictionary<string, double> normalisedQueryVector,
                                                List<Dictionary<string, double>> normalisedDocumentVectors)
        {
            Dictionary<int, double> cosineScore = new Dictionary<int, double>();
            for (int i = 0; i < normalisedDocumentVectors.Count; i++)
            {
                double score = 0;
                foreach (KeyValuePair<string, double> entry in normalisedQueryVector)
                {
                    if (normalisedDocumentVectors[i].ContainsKey(entry.Key))
                    {
                        score += normalisedDocumentVectors[i][entry.Key] * entry.Value;
                    }
                }
                cosineScore.Add(i + 1, score);
            }

            return cosineScore.OrderByDescending(key => key.Value);
        }

        // We read all pages and add the top k results such that we have html to return
        private List<Page> TakeTopKResults(IOrderedEnumerable<KeyValuePair<int, double>> sortedCosineScore, int k)
        {
            var topKResults = sortedCosineScore.Take(k);
            List<Page> topKPages = new List<Page>();

            foreach (var result in topKResults)
            {
                topKPages.Add(Pages.First(p => p.Id == result.Key));
            }

            return topKPages;
        }

        private void ReadAllPages()
        {
            // Reads the pages stored in html.txt but only if they are not already stored in the pages property
            if (Pages.Count == 0)
            {
                string[] lines = System.IO.File.ReadAllLines(Directory.GetCurrentDirectory() + "//html.txt");
                int i = 0;

                while (i < lines.Length)
                {
                    var lineSplit = lines[i].Split(" ");
                    // TODO fix the list that is added here
                    Pages.Add(new Page(lines[i + 1], lineSplit.Last(), new List<string>()));
                    i += 3;
                }
            }
        }

        public List<Dictionary<string, double>> NormaliseDocumentVectors()
        {

            for (int i = 0; i < TfIdfWeight.Count; i++)
            {
                NormalizedTfIdfDocumentVectors.Add(new Dictionary<string, double>());
                double vectorLength = 0;
                foreach (KeyValuePair<string, double> entry in TfIdfWeight[i])
                {
                    NormalizedTfIdfDocumentVectors[i].Add(entry.Key, 0);
                    vectorLength += (entry.Value * entry.Value);
                }
                vectorLength = Math.Sqrt(vectorLength);

                foreach (KeyValuePair<string, double> entry in TfIdfWeight[i])
                {
                    NormalizedTfIdfDocumentVectors[i][entry.Key] = entry.Value / vectorLength;
                }
            }
            return NormalizedTfIdfDocumentVectors;
        }

        private void PrintTopResults(List<Page> topKPages, string[] userQuery)
        {
            var englishStopWords = StopWord.GetEnglishStopwords();

            foreach (var item in topKPages)
            {
                Console.WriteLine("\n" + item.Url);
                string[] htmlSplit = item.Html.Split(" ");
                htmlSplit = htmlSplit.Except(englishStopWords).ToArray();
                string text = "";

                for (int i = 0; i < htmlSplit.Length; i++)
                {
                    if (userQuery.Contains(htmlSplit[i]))
                    {
                        text += "...";
                        for (int j = i - 4; j < i + 4; j++)
                        {
                            if (j < 0 || j > htmlSplit.Length)
                            {
                                continue;
                            }
                            else
                            {
                                text += htmlSplit[j] + " ";
                            }
                        }
                    }
                }
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine(text + "\n\n");
            }
        }

        private void GetSiteOutDegree()
        {

            foreach (Page p in Pages)
            {   try{
                    HtmlWeb hw = new HtmlWeb()
                    {
                        PreRequest = request =>
                        {
                        // Make any changes to the request object that will be used.
                        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                        return true;
                        }
                    };
                    HtmlDocument doc = hw.Load(p.Url);
                    var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
                    if (nodes != null)
                    {
                        foreach (HtmlNode link in nodes)
                        {
                            var url = link.Attributes["href"].Value;
                            if (url.StartsWith("http"))
                            {
                                if (Pages.Any(page => page.Url == url))
                                    p.OutDegreeLinks.Add(link.Attributes["href"].Value);
                            }
                        }
                    }
                }
                catch(Exception e){
                    Console.WriteLine(e.Message);
                }
            }

            using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(Directory.GetCurrentDirectory() + "//outdegreelinks.txt"))
            {
                foreach (Page p in Pages)
                {
                    file.WriteLine("Id: " + p.Id);
                    foreach (var link in p.OutDegreeLinks)
                    {
                        file.WriteLine(link);
                    }
                    
                }
            }
        }

        private void ReadOutDegreeLinks() {
            string[] lines = System.IO.File.ReadAllLines(Directory.GetCurrentDirectory() + "//outdegreelinks.txt");
            int i = 0;
            int id = 1;
            while (i < lines.Length)
            {
                if(lines[i].StartsWith("Id")){
                    var lineSplit = lines[i].Split(" ");
                    id = Int32.Parse(lineSplit[1]);
                } else {
                    Pages.Find(p => p.Id == id).OutDegreeLinks.Add(lines[i]);
                }
                i++;
            }
        }

        private Matrix<double> CreateTransitionProbabilityMatrix()
        {
            var pagesWithOutDegrees = Pages.Where(p => p.OutDegreeLinks.Count != 0).ToList();
            Matrix<double> transitionProbabilityMatrix = Matrix<double>.Build.Dense(pagesWithOutDegrees.Count, pagesWithOutDegrees.Count);

            for (int i = 0; i < transitionProbabilityMatrix.RowCount; i++)
            {
                for (int j = 0; j < transitionProbabilityMatrix.ColumnCount; j++)
                {
                    foreach (var url in pagesWithOutDegrees[i].OutDegreeLinks)
                    {
                        if (pagesWithOutDegrees[j].Url == url)
                        {
                            transitionProbabilityMatrix[i, j] += (double)1.0 / (double)pagesWithOutDegrees[i].OutDegreeLinks.Count;
                        }
                    }
                }
            }

            Matrix<double> danglingPageMatrix = Matrix<double>.Build.Dense(pagesWithOutDegrees.Count, pagesWithOutDegrees.Count, (double)1.0 / (double)pagesWithOutDegrees.Count);
            Matrix<double> weightedTransProbMatrix = Matrix<double>.Build.Dense(pagesWithOutDegrees.Count, pagesWithOutDegrees.Count);
            Matrix<double> weightedDanglingPageMatrix = Matrix<double>.Build.Dense(pagesWithOutDegrees.Count, pagesWithOutDegrees.Count);
            weightedTransProbMatrix = transitionProbabilityMatrix.Multiply(0.9);
            weightedDanglingPageMatrix = danglingPageMatrix.Multiply(0.1);
            
            Matrix<double> finalMatrixLol = Matrix<double>.Build.Dense(pagesWithOutDegrees.Count, pagesWithOutDegrees.Count);
            weightedTransProbMatrix.Add(weightedDanglingPageMatrix, finalMatrixLol);

            return finalMatrixLol;
        }

        private Vector<double> ComputeTransition(Matrix<double> transProbMatrix, Vector<double> distribution){

            Vector<double> newTransProbVector = Vector<double>.Build.Dense(transProbMatrix.RowCount);
            
            //transProbMatrix.Multiply(distribution, newTransProbVector);
           // var v2 = transProbMatrix.Multiply(distribution);

           double newValue = 0;

            for(int i = 0; i < distribution.Count; i++) {
                for (int j = 0; j < transProbMatrix.ColumnCount; j++) {
                    newValue += distribution[j] * transProbMatrix[j, i];
                }
                newTransProbVector[i] = newValue;
                newValue = 0;
            }
            Console.WriteLine(newTransProbVector.Sum());

            return newTransProbVector;

        }
    }
}