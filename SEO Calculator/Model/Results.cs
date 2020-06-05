using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SEO_Calculator.Enums;
using ShellProgressBar;
using static SEO_Calculator.Core.SEO;

namespace SEO_Calculator.Model
{
    public static class Results
    {
        public static List<Result> GoogleResults { get; set; } = new List<Result>();

        public static List<Result> BingResults { get; set; } = new List<Result>();

        // Generate Result Lists
        internal static async Task Generate(string[] terms, SearchEngines searchEngine)
        {
            BingResults.Clear();
            GoogleResults.Clear();

            var options = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ProgressBarOnBottom = true
            };

            var notnullTerms = terms.Where(term => !string.IsNullOrWhiteSpace(term));
            // ReSharper disable once PossibleMultipleEnumeration
            int termsCount = notnullTerms.Count();

            int count = 0;
            using (var pbar = new ProgressBar(termsCount, $"Step 0 of {termsCount}: ", options))
            {
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var term in notnullTerms)
                {
                    //if (term != string.Empty)
                    //    UpdateProgress();

                    switch (searchEngine)
                    {
                        case SearchEngines.All:
                            BingResults.Add(new Result(term, await GetBingResults(term)));
                            GoogleResults.Add(new Result(term, await GetGoogleResults(term)));
                            break;

                        case SearchEngines.Bing:
                            BingResults.Add(new Result(term, await GetBingResults(term)));
                            break;

                        case SearchEngines.Google:
                            BingResults.Add(new Result(term, await GetGoogleResults(term)));
                            break;
                    }

                    pbar.Tick($"Step {count} of {termsCount}: {term}"); //will advance pbar to 1 out of 10.
                    //we can also advance and update the progressbar text
                    //pbar.Tick("Step 2 of 10");

                    ++count;
                }
            }
        }
    }
}