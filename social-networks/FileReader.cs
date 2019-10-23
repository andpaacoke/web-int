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
            string[] textSplit;
            string name = "";
            string[] friends;
            List<User> users = new List<User>();

            using (StreamReader reader = new StreamReader("friendships.txt"))
            {
                var text = reader.ReadToEnd();
                textSplit = text.Split(new Char [] {':' , '\n' , '\r'});
                textSplit = textSplit.Where(x => x != "").ToArray();
            }

            int i = 0;
            while(i < textSplit.Length - 1) {
                if(textSplit[i] == "user") {
                    i++;
                    name = textSplit[i];
                    i++;
                    if(textSplit[i] == "friends") {
                        i++;
                        friends = textSplit[i].Trim().Split("\t");
                        i++;
                        users.Add(new User(name.ToLower(), new List<string>(friends)));
                    }
                }
                i++;
            }

            return users;
        }
    }
}