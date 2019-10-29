using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            string[] friends;
            string summary = "";
            string review = "";

            using (StreamReader reader = new StreamReader("friendships.reviews.txt"))
            {
                var text = reader.ReadToEnd();
                textSplit = text.Split(new Char[] {' ', '\n', '\r' });
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
                        
                        friends = textSplit[i].Trim().Split("\t");
                        friends = friends.Where(x => !x.StartsWith("friends")).ToArray();
                        i++;

                        if (textSplit[i] == "summary:")
                        {
                            summary = "";
                            i++;
                            while(!(textSplit[i] == "review:")){
                                summary += textSplit[i];
                                i++;
                            }
                            if (textSplit[i] == "review:")
                            {
                                review = "";
                                i++;
                                while (!(textSplit[i] == "user:"))
                                {
                                    review += textSplit[i];
                                    i++;
                                }
                                users.Add(new User(name, new List<string>(friends), summary.ToLower(), review.ToLower()));
                            }
                        }
                    }
                }
                i++;
            }
            return users;
        }
    }
}