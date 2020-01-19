using System;
using System.Collections.Generic;
using System.IO;
using MathNet.Numerics.LinearAlgebra;

namespace Collaborative
{
    public class Recommender
    {

        public Matrix<double> ReadFile(string fileName) {
            var lines = File.ReadAllLines(fileName);
            Matrix<double> userMovieMatrix = Matrix<double>.Build.Dense(943, 1682);

            foreach(var line in lines) {
                var lineSplit = line.Split("\t");
                var userId = Int32.Parse(lineSplit[0]);
                var movieId = Int32.Parse(lineSplit[1]);
                var rating = Double.Parse(lineSplit[2]);
                userMovieMatrix[userId - 1, movieId - 1] = rating;
            }

            Console.Write(userMovieMatrix[0,0]);

            return userMovieMatrix;

        }

        public Matrix<double> PreProcessMatrix(Matrix<double> m) {
            Dictionary<int, int> movieObservedRatings = new Dictionary<int, int>();
            Dictionary<int, int> userObservedRatings = new Dictionary<int, int>();
            int totalObservedRatings = 0;
            for (int i = 0 ; i < m.RowCount; i ++) {
                for (int j = 0; j < m.ColumnCount; j++) {
                    if(m[i, j] != 0){
                        totalObservedRatings++;
                        if(userObservedRatings.ContainsKey(i + 1)) {
                            userObservedRatings[i + 1]++;
                        } 
                        else {
                            userObservedRatings.Add(i + 1, 1);
                        }

                        if(movieObservedRatings.ContainsKey(j + 1)) {
                            movieObservedRatings[j + 1]++;
                        }
                        else {
                            movieObservedRatings.Add(j + 1, 1);
                        }
                    }
                }
            }

            Matrix<double> preProcessedMatrix = Matrix<double>.Build.Dense(943, 1682);

            double totalRatingsValue = m.ColumnSums().Sum();
            for (int i = 0 ; i < m.RowCount; i ++) {
                for (int j = 0; j < m.ColumnCount; j++) {
                    if(m[i, j] > 0){
                        preProcessedMatrix[i, j] = m[i, j] - (m.Column(j).Sum() / movieObservedRatings[j + 1]) - (m.Row(i).Sum() / userObservedRatings[i + 1]) + (totalRatingsValue / totalObservedRatings);
                    }
                }
            }

            return preProcessedMatrix;
        }


        public void DoFactorization(Matrix<double> m, Matrix<double> preProcessedMatrix) {
            Random random = new Random();
            var userFactorMatrix = Matrix<double>.Build.Dense(m.RowCount, 20);
            var factorMovieMatrix = Matrix<double>.Build.Dense(20, m.ColumnCount);
            for (int i = 0; i < userFactorMatrix.RowCount; i++)
            {
                for (int j = 0; j < userFactorMatrix.ColumnCount; j++)
                {
                    userFactorMatrix[i, j] = random.NextDouble();
                    factorMovieMatrix[j, i] = random.NextDouble();
                }
            }

            var resultMatrix = Matrix<double>.Build.Dense(m.RowCount, m.ColumnCount);

            userFactorMatrix.Multiply(factorMovieMatrix, resultMatrix);
            Console.WriteLine(resultMatrix);
            double squareError = 10;

            for (int runs = 0; runs < 5000 && squareError > 0.001; runs++)
            {
                double learningRate = 0.001;
                double weightDecay = 0.001;
                for (int i = 0; i < m.RowCount; i++)
                {
                    for (int j = 0; j < m.ColumnCount; j++)
                    {
                        if (m[i, j] > 0)
                        {
                            double error = m[i, j] - resultMatrix[i, j];
                            for (int k = 0; k < userFactorMatrix.ColumnCount; k++)
                            {
                                userFactorMatrix[i, k] =
                                    userFactorMatrix[i, k] +
                                    (learningRate *  error * factorMovieMatrix[k, j]) - (weightDecay * userFactorMatrix[i, k]);
                                factorMovieMatrix[k, j] =
                                    factorMovieMatrix[k, j] +
                                    (learningRate * userFactorMatrix[i, k] * error) - (weightDecay * factorMovieMatrix[k, j]);
                            }
                        }

                    }
                }
                userFactorMatrix.Multiply(factorMovieMatrix, resultMatrix);

            }
                squareError = 0;
                int actualRatingCount = 0;
                double RMSE = 0;
			    for (int i = 0; i < m.RowCount; i++) {
				    for (int j = 0; j < m.ColumnCount; j++) {
					    if (m[i, j] > 0) {
                            actualRatingCount++;
						    squareError =
							    squareError + Math.Pow((resultMatrix[i, j] - m[i, j]), 2);
                            
					    }
                    }
                }
                RMSE = Math.Sqrt(squareError/actualRatingCount);
                Console.WriteLine(RMSE);
            //resultMatrix.Add(preProcessedMatrix, resultMatrix);

        }

/* 
        private Matrix<double> DotMatrices(Matrix<double> m1, Matrix<double> m2) {
            double sum = 0;
            var resultMatrix = Matrix<double>.Build.Dense(m1.RowCount, m2.ColumnCount);
            for (int i = 0; i < m1.RowCount; i++)
            {
                for (int j = 0; j < m2.ColumnCount; j++)
                {
                    for (int k = 0; k < m1.ColumnCount; k++)
                    {
                        sum = sum + m1[i,k] * m2[k, j];
                    }
                    resultMatrix[i, j] = sum;
                    sum = 0;
                }
            }
            return resultMatrix;
        }
 */
    }
}