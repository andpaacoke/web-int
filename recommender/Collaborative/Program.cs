using System;

namespace Collaborative
{
    class Program
    {
        static void Main(string[] args)
        {
            Recommender fn = new Recommender();
            var m = fn.ReadFile("u1.base");
            var preProcessedMatrix = fn.PreProcessMatrix(m);
            fn.DoFactorization(m, preProcessedMatrix);
        }
    }
}
