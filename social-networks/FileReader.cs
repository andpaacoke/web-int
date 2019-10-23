using System.Collections.Generic;
using System.IO;

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
                textSplit = text.Split(":");
            }

            int i = 0;
            while(i < textSplit.Length - 1) {
                if(textSplit[i] == "user") {
                    i++;
                    name = textSplit[i];
                    i++;
                    if(textSplit[i] == "friends") {
                        i++;
                        friends = textSplit[i].Split(" ");
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