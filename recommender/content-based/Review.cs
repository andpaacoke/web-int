namespace content_based
{
    public class Review
    {
        public string ReviewerId { get; set; }
        public string Asin { get; set; }
        public string ReviewerName { get; set; }
        public int[] Helpful { get; set; }
        public string ReviewText { get; set; }
        public double Overall { get; set; }
        public string Summary { get; set; }
        public int UnixReviewTime { get; set; }
        public string ReviewTime { get; set; }
        public Review(string reviewerId, string asin, string reviewerName, int[] helpful, string reviewText, double overall, string summary, int unixReviewTime, string reviewTime)
        {
            ReviewerId = reviewerId;
            Asin = asin;
            ReviewerName = reviewerName;
            Helpful = helpful;
            ReviewText = reviewText;
            Overall = overall;
            Summary = summary;
            UnixReviewTime = unixReviewTime;
            ReviewTime = reviewTime;
        }
    }
}