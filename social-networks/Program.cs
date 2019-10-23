using System;

namespace social_networks
{
    class Program
    {
        static void Main(string[] args)
        {
            FileReader fr = new FileReader();
            fr.ReadFriendshipsFile();
        }
    }
}
