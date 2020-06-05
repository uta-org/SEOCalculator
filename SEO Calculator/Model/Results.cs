using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using SEO_Calculator.Enums;
using ShellProgressBar;
using static SEO_Calculator.Core.SEO;

namespace SEO_Calculator.Model
{
    public static class Results
    {
        public static List<Result> GoogleResults { get; set; } = new List<Result>();

        public static List<Result> BingResults { get; set; } = new List<Result>();

        internal static string[] GetNotNullTerms(string[] terms, out int termsCount)
        {
            var notnullTerms = terms.Where(term => !string.IsNullOrWhiteSpace(term)).ToArray();
            // ReSharper disable once PossibleMultipleEnumeration
            termsCount = notnullTerms.Length;
            return notnullTerms;
        }

        // Generate Result Lists
        internal static async Task Generate(IWebDriver web, ProgressBar progressBar, string[] terms, int min, int max, SearchEngines searchEngine, bool clear)
        {
            if (clear)
            {
                BingResults.Clear();
                GoogleResults.Clear();
            }

            int termsCount = terms.Length;
            int count = 0;

            // ReSharper disable once PossibleMultipleEnumeration
            //foreach (var term in terms)
            for (int i = min; i < max; i++)
            {
                var term = terms[i];
                //if (term != string.Empty)
                //    UpdateProgress();

                switch (searchEngine)
                {
                    case SearchEngines.All:
                        BingResults.Add(new Result(term, await GetBingResults(web, term)));
                        GoogleResults.Add(new Result(term, await GetGoogleResults(web, term)));
                        break;

                    case SearchEngines.Bing:
                        BingResults.Add(new Result(term, await GetBingResults(web, term)));
                        break;

                    case SearchEngines.Google:
                        BingResults.Add(new Result(term, await GetGoogleResults(web, term)));
                        break;
                }

                progressBar.Tick($"Step {count} of {termsCount}: {term}"); //will advance pbar to 1 out of 10.
                                                                           //we can also advance and update the progressbar text
                                                                           //pbar.Tick("Step 2 of 10");

                ++count;
            }
        }
    }
}