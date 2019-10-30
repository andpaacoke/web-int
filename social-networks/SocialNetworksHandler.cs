using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.Linq;

namespace social_networks
{
    public class SocialNetworksHandler
    {
        public SocialNetworksHandler()
        {
            
        }

        public double[,] BuildAdjacencyMatrix(List<User> users) {
            double[,] adjacencyMatrix = new double[users.Count + 1, users.Count + 1];
            int id = 0;
            for(int i = 0; i < users.Count; i++) {
                foreach(string friend in users[i].Friends) {
                    try
                    {
                        id = users.Find(x => x.Name == friend).Id;                        
                    }
                    catch (System.Exception)
                    {
                        Console.WriteLine($"{friend} Friend could not be found");
                        continue;
                    }
                    adjacencyMatrix[i, id] = 1;
                    adjacencyMatrix[id, i] = 1;
                }
            }

            //Evd<double> evd = A.Evd();
            return adjacencyMatrix;
        }

        private double[] MakeSumVector(double[,] adjacencyMatrix) {
            double[] sumVector = new double[adjacencyMatrix.GetLength(0)];
            for (int i = 1; i < sumVector.Length - 1;  i++) {
                
                for (int j = 1 ; j < sumVector.Length - 1; j++) {
                    
                    sumVector[i] += adjacencyMatrix[i, j];
                }
            }

            return sumVector;
        }

        public DiagonalMatrix MakeDiagonalMatrix(double[,] adjacencyMatrix) {
            var sumVector = MakeSumVector(adjacencyMatrix);
            var diagonalMatrix = DiagonalMatrix.OfDiagonal(sumVector.Length, sumVector.Length, sumVector);

            return diagonalMatrix;
        }

        public Matrix<double> ComputeLaplacian(Matrix diagonalMatrix , double[,] adjacencyMatrix) { 
            Matrix<double> A = DenseMatrix.OfArray(adjacencyMatrix);

            Matrix<double> laplacian = diagonalMatrix.Subtract(A);

            return laplacian;
        }

        public Evd<double> ComputeEVD(Matrix<double> laplacian) {
            Evd<double> evd = laplacian.Evd();

            var column = new List<double>(evd.EigenVectors.Column(1).Storage.ToArray());
            var orderedColumn = column.OrderBy(x => x).ToList();
            double maxValue = orderedColumn.Last();
            double smallestValue = orderedColumn.First();
            return evd;
        }

        

    }
}