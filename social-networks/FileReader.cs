using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace social_networks
{
    public class FileReader
    {
        public FileReader()
        {

        }

        public List<User> ReadFriendshipsFile()
        {
            User.counter = 0;
            string[] textSplit;
            string name = "";
            string[] friends;
            List<User> users = new List<User>();

            using (StreamReader reader = new StreamReader("friendships.txt"))
            {
                var text = reader.ReadToEnd();
                textSplit = text.Split(new Char[] { ':', '\n', '\r' });
                textSplit = textSplit.Where(x => x != "").ToArray();
            }

            int i = 0;
            while (i < textSplit.Length - 1)
            {
                if (textSplit[i] == "user")
                {
                    i++;
                    name = textSplit[i];
                    i++;
                    if (textSplit[i] == "friends")
                    {
                        i++;
                        friends = textSplit[i].Trim().Split("\t");
                        i++;
                        users.Add(new User(name.ToLower(), new List<string>(friends), "", ""));
                    }
                }
                i++;
            }

            return users;
        }

        public List<User> ReadFriendshipsReviewsFile()
        {
            User.counter = 0;
            List<User> users = new List<User>();
            string[] textSplit;
            string name = "";
            List<string> friends = new List<string>();
            string summary = "";
            string review = "";

            using (StreamReader reader = new StreamReader("friendships.reviews.txt"))
            {
                var text = reader.ReadToEnd();
                textSplit = text.Split(new Char[] {'\t', ' ', '\n', '\r' });
                textSplit = textSplit.Where(x => x != "").ToArray();
            }

            int i = 0;
            while (i < textSplit.Length - 1)
            {
                if (textSplit[i].StartsWith("user"))
                {
                    i++;
                    name = textSplit[i];
                    i++;

                    if (textSplit[i].StartsWith("friends"))
                    {
                        i++;
                        while(!(textSplit[i].StartsWith("summary:"))){
                                friends.Add(textSplit[i]);
                                i++;
                            }
                    
                        if (textSplit[i].StartsWith("summary:"))
                        {
                            summary = "";
                            i++;
                            while(!(textSplit[i].StartsWith("review:"))){
                                summary += " " + textSplit[i];
                                i++;
                            }
                            if (textSplit[i] == "review:")
                            {
                                review = "";
                                i++;
                                while (!(textSplit[i].StartsWith("user:")) && i < textSplit.Length - 1)
                                {
                                    review += " " + textSplit[i];
                                    i++;
                                }
                                users.Add(new User(name, friends, summary.ToLower(), review.ToLower()));
                                friends = new List<string>();                            }
                        }
                    }
                }
                else {
                    i++;
                }
            }
            return users;
        }

        public List<string> ReadReviewsFromSentimentData() {

            var lines = File.ReadAllLines("SentimentTrainingData.txt");

            List<string> reviews = new List<string>();

            foreach (var line in lines)
            {
                if(line.StartsWith("review/text")) {
                    var stringFormat = Regex.Replace(line, @"<[^>]*>", " ");
                    stringFormat = stringFormat.Replace("review/text:", "");
                    reviews.Add(stringFormat);
                }
            }

            return reviews;
        }

        public List<float> ReadScores() {
            var lines = File.ReadAllLines("scores.txt");

            List<float> scores = new List<float>();

            foreach (var line in lines)
            {
                scores.Add(float.Parse(line));
            }

            return scores;
        }
    }
}