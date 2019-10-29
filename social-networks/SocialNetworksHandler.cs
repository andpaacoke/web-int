using System.Collections.Generic;

namespace social_networks
{
    public class SocialNetworksHandler
    {
        public SocialNetworksHandler()
        {
            
        }

        public int[,] BuildAdjacencyMatrix(List<User> users) {
            int[,] adjacencyMatrix = new int[users.Count, users.Count];

            for(int i = 0; i < users.Count; i++) {
                foreach(string friend in users[i].Friends) {
                    int id = users.Find(user => user.Name.ToLower().Trim() == friend.ToLower().Trim()).Id;
                    adjacencyMatrix[i, id] = 1;
                    adjacencyMatrix[id, i] = 1;
                }
            }
            return adjacencyMatrix;
        }
    }
}