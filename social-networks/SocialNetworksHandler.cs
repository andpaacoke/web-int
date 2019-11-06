using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.Linq;
using Accord.MachineLearning;
using VaderSharp;
using System.Text.RegularExpressions;

namespace social_networks
{
    public class SocialNetworksHandler
    {
        public SocialNetworksHandler()
        {
            
        }

        public void TestSentiment(){
        SentimentIntensityAnalyzer analyzer = new SentimentIntensityAnalyzer();
        string myStringIsABlowup = "RECONSIDER THIS FORMULA!!<br /><br />Martek advertises this formula on their website!<br /><br /> Although Martek told the board that they would discontinue the use of the controversial neurotoxic solvent n-hexane for DHA/ARA processing, they did not disclose what other synthetic solvents would be substituted. Federal organic standards prohibit the use of all synthetic/petrochemical solvents.<br /><br />Martek Biosciences was able to dodge the ban on hexane-extraction by claiming USDA does not consider omega-3 and omega-6 fats to be agricultural ingredients. Therefore, they argue, the ban against hexane extraction does not apply. The USDA helped them out by classifying those oils as necessary vitamins and minerals, which are exempt from the hexane ban. But hexane-extraction is just the tip of the iceberg. Other questionable manufacturing practices and misleading statements by Martek included:<br /><br />Undisclosed synthetic ingredients, prohibited for use in organics (including the sugar alcohol mannitol, modified starch, glucose syrup solids, and other undisclosed ingredients)<br />Microencapsulation of the powder and nanotechnology, which are prohibited under organic laws<br />Use of volatile synthetic solvents, besides hexane (such as isopropyl alcohol)<br />Recombinant DNA techniques and other forms of genetic modification of organisms; mutagenesis; use of GMO corn as a fermentation medium<br />Heavily processed ingredients that are far from natural<br /><br />quote from: Why is this Organic Food Stuffed With Toxic Solvents? by Dr. Mercola - GOOGLE GMOs found in Martek.<br /><br />This is the latest I have found on DHA in organic and non organic baby food/ formula:<br />AT LEAST READ THIS ONE*** GOOGLE- False Claims That DHA in Organic and Non-Organic Infant Formula Is Safe. AND OrganicconsumersDOTorg<br /><br />Martek's patents for Life'sDHA states: includes mutant organisms and recombinant organisms, (a.k.a. GMOs!) The patents explain that the oil is extracted readily with an effective amount of solvent ... a preferred solvent is hexane.<br /><br />The patent for Life'sARA states: genetically-engineering microorganisms to produce increased amounts of arachidonic acid and extraction with solvents such as ... hexane. Martek has many other patents for DHA and ARA. All of them include GMOs. GMOs and volatile synthetic solvents like hexane aren't allowed in USDA Organic products and ingredients.<br /><br />Tragically, Martek's Life'sDHA is already in hundreds of products, many of them certified USDA Organic. Please demand that the National Organic Standards Board reject Martek's petition, and that the USDA National Organic Program inform the company that the illegal 2006 approval is rescinded and that their GMO, hexane-extracted Life'sDHA and Life'sARA are no longer allowed in organic products.<br /><br />BUT I went to the lifesdha website and THEY DO NOT DISCLOSE HOW THEY MAKE THEIR LifesDHA!!! I have contacted the company to see what they say.<br /><br />Also these are the corporate practices of Martek which are damaging to the environment as well written just last Dec 2011 at NaturalnewsDOTcom<br /><br />The best bet is to just avoid the lifeDHA at this time in my opinion b/c corporate america cares more about the almighty $ than your health.";
        string fixMyString = Regex.Replace(myStringIsABlowup, @"<[^>]*>", " ");

        List<string> sentences = new List<string>{"VADER is smart, handsome, and funny.",  // positive sentence example
             "VADER is smart, handsome, and funny!",  // punctuation emphasis handled correctly (sentiment intensity adjusted)
             "VADER is very smart, handsome, and funny.", // booster words handled correctly (sentiment intensity adjusted)
             "VADER is VERY SMART, handsome, and FUNNY.",  // emphasis for ALLCAPS handled
             "VADER is VERY SMART, handsome, and FUNNY!!!", // combination of signals - VADER appropriately adjusts intensity
             "VADER is VERY SMART, uber handsome, and FRIGGIN FUNNY!!!", // booster words & punctuation make this close to ceiling for score
             "VADER is not smart, handsome, nor funny.",  // negation sentence example
             "The book was good.",  // positive sentence
             "At least it isn't a horrible book.",  // negated negative sentence with contraction
             "The book was only kind of good.", // qualified positive sentence is handled correctly (intensity adjusted)
             "The plot was good, but the characters are uncompelling and the dialog is not great.", // mixed negation sentence
             "Today SUX!",  // negative slang with capitalization emphasis
             "Today only kinda sux! But I'll get by, lol", // mixed sentiment example with slang and constrastive conjunction "but"
             "Make sure you :) or :D today!",  // emoticons handled
             "Catch utf-8 emoji such as such as 💘 and 💋 and 😁",  // emojis handled
             "Not bad at all"  // Capitalized negation
        };

             foreach (var sentence in sentences)
            {
                var sentence2 = analyzer.PolarityScores(sentence);
                Console.WriteLine("----------------");
                Console.WriteLine(sentence);
                Console.WriteLine("Positive score: " + sentence2.Positive);
                Console.WriteLine("Negative score: " + sentence2.Negative);
                Console.WriteLine("Neutral score: " + sentence2.Neutral);
                Console.WriteLine("Compound score: " + sentence2.Compound);
            }
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

        public List<double> ComputeEVD(Matrix<double> laplacian) {
            Evd<double> evd = laplacian.Evd();

            var column = new List<double>(evd.EigenVectors.Column(1).Storage.ToArray());
            
            return column;
        }

        public int[] KMeansClustering(List<double> column)
        {
            var orderedColumn = column.OrderBy(x => x).ToList();
            double maxValue = orderedColumn.Last();
            double smallestValue = orderedColumn.First();

            KMeans kmeans = new KMeans(k: 10);

            double[][] data = new double[column.Count][];

            for(int i = 0; i < column.Count; i++) {
                data[i] = new double[] {1, column[i]};
            }

            // Compute and retrieve the data centroids
            var clusters = kmeans.Learn(data);

            // Use the centroids to parition all the data
            int[] labels = clusters.Decide(data);

            return labels;
        }



    }
}