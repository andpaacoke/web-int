using System;

namespace content_based
{
    class Program
    {
        static void Main(string[] args)
        {
            Recommender r = new Recommender();
            r.KNNContent();
        }
    }
}
