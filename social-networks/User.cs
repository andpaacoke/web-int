using System.Collections.Generic;

namespace social_networks
{
    public class User
    {
        public string Name { get; set; }
        public List<string> Friends { get; set; }

        public User(string name, List<string> friends)
        {
            Name = name;
            Friends = friends;
        }
    }
}