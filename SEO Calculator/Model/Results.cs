﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        private static string BingFile => Path.Combine(Environment.CurrentDirectory, "bing.json");
        private static string GoogleFile => Path.Combine(Environment.CurrentDirectory, "google.json");

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
                File.Delete(BingFile);
                File.Delete(GoogleFile);

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
                        GoogleResults.AddRange(await GetGoogleResults(web, term));
                        break;

                    case SearchEngines.Bing:
                        BingResults.Add(new Result(term, await GetBingResults(web, term)));
                        break;

                    case SearchEngines.Google:
                        GoogleResults.AddRange(await GetGoogleResults(web, term));
                        break;
                }

                progressBar.Tick($"Step {count} of {termsCount}: {term}");

                Persist();

                ++count;
            }
        }

        private static void Persist()
        {
            var bingJson = JsonConvert.SerializeObject(BingResults, Formatting.Indented);
            var googleJson = JsonConvert.SerializeObject(GoogleResults, Formatting.Indented);

            File.WriteAllText(BingFile, bingJson);
            File.WriteAllText(GoogleFile, googleJson);
        }
    }
}