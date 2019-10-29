using System.Collections.Generic;

namespace social_networks
{
    public class User
    {
        public static int counter;
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Friends { get; set; }
        public string Summary { get; set; }
        public string Review { get; set; }

        public User(string name, List<string> friends, string summary, string review)
        {
            Id = ++counter;
            Name = name;
            Friends = friends;
            Summary = summary;
            Review = review;
        }
    }
}