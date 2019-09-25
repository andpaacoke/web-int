namespace web_crawler
{
    public class Term
    {
        public int Id { get; set; }
        public string Word { get; set; }

        public Term(string word, int id)
        {
            Word = word;
            Id = id;
        }
    }
}