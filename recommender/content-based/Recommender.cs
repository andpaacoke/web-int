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
            InverseDocumentFrequency(recommendableItemsReviews, TFReviewsByUser);
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

        public int InverseDocumentFrequency(List<Review> reviewsNotRatedByUser, List<Dictionary<string,int>> TFReviewsByUser) {
            int numberOfRecommendable = reviewsNotRatedByUser.Count;
            int docKeywordCount = 0;
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
        


            double InverseDocumentFrequency = 0;
            InverseDocumentFrequency = Math.Log(numberOfRecommendable / docKeywordCount);
            return 2;

        }
    }
}