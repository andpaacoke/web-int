using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;

namespace content_based
{
    public class Recommender
    {
        public List<Review> LoadJson() {
            List<Review> reviews = new List<Review>();
 
                var f = File.ReadLines("Musical_Instruments_5.json");

                foreach(var json in f) {
                    reviews.Add(JsonConvert.DeserializeObject<Review>(json));
                }
            

            return reviews;
        }

        public void KNNContent() {
            var reviews = LoadJson();
            
            var uniqueItems = new List<string>();
            var uniqueUsers = new List<string>();
            var recommendableDocuments = new List<Review>();
            foreach(var review in reviews){
                if (!uniqueItems.Contains(review.Asin)) {
                    uniqueItems.Add(review.Asin);
                    recommendableDocuments.Add(review);
                }
                if(!uniqueUsers.Contains(review.ReviewerName)) {
                    uniqueUsers.Add(review.ReviewerName);
                }
            }

            var reviewsByUser = reviews.FindAll(name => name.ReviewerName == uniqueUsers[0]);
            List<Review> recommendableItemsReviews = reviews;

            foreach(var review in reviewsByUser) {
                recommendableItemsReviews = recommendableItemsReviews.Where(r => r.Asin != review.Asin).ToList();
            }

            var recommendableItems = recommendableDocuments.Except(reviewsByUser).ToList();
            var TFReviewsByUser = TFReviewer(reviewsByUser);
            var IDFs = InverseDocumentFrequency(recommendableItemsReviews, TFReviewsByUser);
            var TFIDFs = CalcTFIDF(TFReviewsByUser, IDFs);

            var TFAllReviews = TFReviewer(reviews);
            var IDFAllReviews = InverseDocumentFrequency(reviews, TFAllReviews);
            var TFIDFAllReviews = CalcTFIDF(TFAllReviews, IDFAllReviews);
        }


        public List<Dictionary<string,int>> TFReviewer(List<Review> reviewsByUser) {
            List<Dictionary<string, int>> TFReviewsByUser = new List<Dictionary<string, int>>();
            foreach(var review in reviewsByUser){
                TFReviewsByUser.Add(CalculateTF(review.ReviewText));
            }

            return TFReviewsByUser;
        }

        public Dictionary<string, int> CalculateTF(string document) {
            Dictionary<string, int> TF = new Dictionary<string, int>();
            //null gør at den splitter whitespace som default åbenbart
            string noPunctuation = Regex.Replace(document, @"\p{P}", "");

            string[] allWordsInDoc = document.Split(null);
            
            foreach(string word in allWordsInDoc){
                if(TF.ContainsKey(word)) {
                    TF[word]++;
                }
                else {
                    TF.Add(word, 1);
                }
            }

            return TF;
        }

        public Dictionary<string, double>  InverseDocumentFrequency(List<Review> reviewsNotRatedByUser, List<Dictionary<string,int>> TFReviewsByUser) {
            int numberOfRecommendable = reviewsNotRatedByUser.Count;
            Dictionary<string, double> termIDFValues = new Dictionary<string, double>();
            Dictionary<string, int> allDocumentsWordFrequency = new Dictionary<string, int>();

            foreach (var dict in TFReviewsByUser)
            {
                foreach (KeyValuePair<string, int> entry in dict)
                {
                    if(!termIDFValues.ContainsKey(entry.Key)) {
                        termIDFValues.Add(entry.Key, 0);
                        allDocumentsWordFrequency.Add(entry.Key, 0);
                    }
                }
            }

            foreach (KeyValuePair<string, double> entry in termIDFValues)
            {
                foreach(var review in reviewsNotRatedByUser) {
                    if(review.ReviewText.Contains(entry.Key)) {
                        allDocumentsWordFrequency[entry.Key]++;
                    }
                }
            }

            foreach (KeyValuePair<string, int> entry in allDocumentsWordFrequency)
            {
                if(entry.Value != 0) {
                    termIDFValues[entry.Key] =  Math.Log((double)numberOfRecommendable / (double)entry.Value);
                }
                else {
                    termIDFValues[entry.Key] = 0;
                }
            }
            return termIDFValues;

        }

        public List<Dictionary<string,double>> CalcTFIDF(List<Dictionary<string,int>> TFs, Dictionary<string, double> IDFs) {
            List<Dictionary<string, double>> TFIDFs = new List<Dictionary<string, double>>();

            foreach (var dict in TFs)
            {
                Dictionary<string, double> IDF = new Dictionary<string, double>();
                foreach (KeyValuePair<string, int> entry in dict)
                {
                    IDF.Add(entry.Key, entry.Value * IDFs[entry.Key]);
                }
                TFIDFs.Add(IDF);
            }
            return TFIDFs;
        }
    }
}