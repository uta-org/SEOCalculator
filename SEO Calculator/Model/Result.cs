namespace SEO_Calculator.Model
{
    public class Result
    {
        public string Term { get; }
        public long Count { get; }
        public string SpellOrig { get; }

        private Result()
        {
        }

        public Result(string term, long count)
        {
            Term = term;
            Count = count;
        }

        public Result(string term, long count, string spellOrig)
        {
            Term = term;
            Count = count;
            SpellOrig = spellOrig;
        }
    }
}