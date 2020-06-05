namespace SEO_Calculator.Model
{
    public class Result
    {
        public string Term { get; }
        public long Count { get; }

        private Result()
        {
        }

        public Result(string term, long count)
        {
            Term = term;
            Count = count;
        }
    }
}