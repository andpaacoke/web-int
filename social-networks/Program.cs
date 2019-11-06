using System;

namespace social_networks
{
    class Program
    {
        static void Main(string[] args)
        {
            /*FileReader fr = new FileReader();
            fr.ReadFriendshipsFile();
            var users = fr.ReadFriendshipsReviewsFile();*/
            SocialNetworksHandler snh = new SocialNetworksHandler();
            /*var adjMatrix = snh.BuildAdjacencyMatrix(users);
            var diagonalMatrix = snh.MakeDiagonalMatrix(adjMatrix);
            var laplacian = snh.ComputeLaplacian(diagonalMatrix, adjMatrix);
            var column = snh.ComputeEVD(laplacian);
            snh.KMeansClustering(column);*/
            snh.TestSentiment();

        }
    }
}
