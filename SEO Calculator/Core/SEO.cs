using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using SEO_Calculator.Enums;
using SEO_Calculator.Extensions;
using SEO_Calculator.Model;
using ShellProgressBar;
using static SEO_Calculator.Extensions.StringHelper;
using static SEO_Calculator.Program;

namespace SEO_Calculator.Core
{
    public static class SEO
    {
        // Textbox Terms Hint:
        //internal string Terms_Hint = string.Format("Use \"{0}\" character to separate multiple terms.", My.Settings.User_Separator);

        // POST queries:
        internal static readonly string BingQuery = "https://www.bing.com/search?q=";

        // Regular Expressions:
        internal static readonly string BingRegExResultsA = "id=\"count\">.+?<";

        internal static readonly string BingRegExResultsB = @"[\d\.]+";

        internal static readonly string GoogleQuery = "search?q={0}&nfpr=1";

        internal static readonly string GoogleRegExResultsA = "[^#]result-stats.+?<";
        internal static readonly string GoogleRegExResultsB = @"[\d\,]+";

        internal static readonly string HtmlDocumentBackColor =
            "<body style=\"background-color:{0};\">" + Environment.NewLine + Environment.NewLine;

        internal static readonly string HtmlEnd = "</body></html>";

        // Html file Strings:
        internal static readonly string HtmlHeader =
            "<html><head></head><body><!--- SEO Calculator By z3nth10n -->" + Environment.NewLine +
            Environment.NewLine;

        internal static readonly string HtmlPaletteString =
            " <br><span style=\"color:{0};font-family:Arial;font-size:12px;margin-top:-12px;position:relative;padding-right:12px;\"> {1} ( {2} )</span><img src=\"file:///{3}\" style=\"width:54px; height:29px;\"><span style=\"color:{0};font-family:Arial;font-size:12px;margin-top:-12px;position:relative;padding-left:12px;\" /> {4} ( {5} )</span> <br>" +
            Environment.NewLine + Environment.NewLine;

        internal static readonly string HtmlResultFormat =
            "<b style=\"color:{0};\">The search \"{1}\" has obtained <font style=\"color: {2} ;\"> {3} results.</font></b><br>" +
            Environment.NewLine + Environment.NewLine;

        internal static readonly string HtmlSearchEngineTitle =
            "<h2 style=\"color:{0};\">{1} Results:</h2><br>" + Environment.NewLine + Environment.NewLine;

        internal static readonly string PaletteImage = Path.Combine(Path.GetTempPath(), "SEO Palette.png");

        // Temporary files:
        internal static readonly string ResultsFileHtml = Path.Combine(Path.GetTempPath(), "SEO Calculator.html");

        internal static readonly string ResultsFileTxt = Path.Combine(Path.GetTempPath(), "SEO Calculator.txt");

        // Txt file Strings:
        internal static readonly string TxtHeader = ".:: SEO Calculator By z3nth10n ::." +
                                             Environment.NewLine +
                                             "    =============================================    " +
                                             Environment.NewLine + Environment.NewLine;

        internal static readonly string TxtPaletteString = "Maximum: {0} ({1}) \"{2}\"" + Environment.NewLine +
                                                     "Minimum: {3} ({4}) \"{5}\"" + Environment.NewLine +
                                                     Environment.NewLine + Environment.NewLine;

        internal static readonly string TxtResultFormat = "The search \"{0}\" has obtained {1} results."
                                                    + Environment.NewLine + Environment.NewLine;

        internal static readonly string TxtSearchEngineTitle = "{0} Results:" + Environment.NewLine + Environment.NewLine;

        // Lists:
        //internal static List<Tuple<string, string, long>> Bing_Results_List = new List<Tuple<string, string, long>>();

        //internal static List<Tuple<string, string, long>> Google_Results_List = new List<Tuple<string, string, long>>();

        internal static short _green;

        // Progress Counter
        internal static int _indexPattern = 0;

        // RGB Table Rule of three
        internal static short _red;

        // Rule of three
        internal static double _ruleOf3;

        internal static Tuple<long, string> _ruleOf3Greatest;

        internal static Tuple<long, string> _ruleOf3Lowest;

        // [PROP] SearchPatterns
        internal static string[] Terms => File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, "search.txt"));

        // [PROP] SearchEngine
        internal static SearchEngines SearchEngine => SearchEngines.Google;

        // [PROP] ResultFormat
        internal static ResultFormatting ResultFormat => ResultFormatting.Html;

        // [PROP] ResultSorting
        internal static Sorting ResultSorting => Sorting.ResultDescending;

        // [PROP] Separator
        internal static char Separator => Convert.ToChar(Environment.NewLine);

        // [PROP] HTML_BackColor
        internal static string HtmlBackColor => ColorTranslator.ToHtml(Color.Black);

        // [PROP] HTML_ForeColor
        internal static string HtmlForeColor => ColorTranslator.ToHtml(Color.White);

        //internal static WebClient WebClient;

        internal const string SearchBarXPath = "/html/body/div[3]/form/div[2]/div[1]/div[2]/div/div[2]/input";

        internal const string SearchButtonXPath = "/html/body/div[3]/form/div[2]/div[1]/div[2]/button";

        internal const string SpellOrigXPath =
            "/html/body/div[6]/div[2]/div[9]/div[1]/div[2]/div/div[1]/div[2]/div/p/a[2]";

        private static PageSource LastPageSource { get; set; }

        //internal const string ResultStatsId = "result-stats";

        // Get URL SourceCode
        internal static async Task<PageSource> GetUrlSourceCode(IWebDriver web, SearchEngines engine, string url, string term, bool doSearch)
        {
            string engineUrl = "";

            switch (engine)
            {
                case SearchEngines.Bing:
                    break;

                case SearchEngines.Google:
                    engineUrl = "https://www.google.com/";
                    break;

                default:
                    throw new InvalidOperationException("Unsupported engine type.");
            }

            try
            {
                var task = Task.Run(() =>
                {
                    if (doSearch)
                    {
                        var curUrl = web.Url;
                        bool search = true;
                        if (engine == SearchEngines.Bing && !curUrl.Contains("bing") || engine == SearchEngines.Google && !curUrl.Contains("google"))
                        {
                            web.Navigate().GoToUrl(url);
                            search = false;
                        }

                        if (search)
                        {
                            var element = web.WaitForElement(By.XPath(SearchBarXPath));
                            var text = element.GetAttribute("value");

                            var action = new Actions(web);

                            for (int i = 0; i < text.Length; i++)
                                action = action.SendKeys(element, Keys.Backspace);

                            action.Build().Perform();
                            element.SendKeys(term);
                        }

                        var button = web.WaitForElement(By.XPath(SearchButtonXPath));
                        button.Click();

                        Thread.Sleep(200);
                    }

                    // TODO: Check for bot confirmation

                    IWebElement spellOrig = web.FindElement(By.XPath(SpellOrigXPath));
                    var pageSource = spellOrig != null
                        ? new PageSource(spellOrig, web.PageSource)
                        : new PageSource(web.PageSource);

                    return pageSource;
                });

                await task;
                return task.Result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // Get Bing Results
        internal static async Task<long> GetBingResults(IWebDriver web, string searchPattern)
        {
            var task = GetUrlSourceCode(web, SearchEngines.Bing, $"{BingQuery}{searchPattern}", searchPattern, true);
            await task;

            try
            {
                return Convert.ToInt64(Convert
                    .ToString(Regex.Match(Convert.ToString(Regex.Match(task.Result.Source, BingRegExResultsA).Groups[0]),
                            BingRegExResultsB).Groups[0]
                    ).Replace(".", ""));
            }
            catch
            {
                return 0;
            }
        }

        // Get Google Results
        internal static async Task<List<Result>> GetGoogleResults(IWebDriver web, string searchPattern)
        {
            long GetLong(Task<PageSource> task1)
            {
                return Convert.ToInt64(Convert
                    .ToString(Regex.Match(Convert.ToString(Regex.Match(task1.Result.Source, GoogleRegExResultsA).Groups[0]),
                            GoogleRegExResultsB).Groups[0]
                    ).Replace(",", ""));
            }

            List<Result> results = new List<Result>();

        Retry:
            var task = GetUrlSourceCode(web, SearchEngines.Google, string.Format(GoogleQuery, searchPattern), searchPattern, LastPageSource?.SpellOrig == null);
            await task;

            LastPageSource = task.Result;

            long result;

            try
            {
                result = GetLong(task);
            }
            catch
            {
                result = 0;
            }

            var spelledWord = task.Result.SpellOrig?.Text;
            results.Add(string.IsNullOrEmpty(spelledWord) ? new Result(searchPattern, result) :
                new Result(searchPattern, result, spelledWord));

            task.Result.SpellOrig?.Click();

            if (task.Result.SpellOrig == null)
                return results;

            searchPattern = spelledWord;
            goto Retry;
        }

        internal static async Task GenerateResults(IWebDriver web, ProgressBar progressBar, string[] terms, int min, int max, bool clear)
        {
            await Results.Generate(web, progressBar, terms, min, max, SearchEngine, clear);
        }

        internal static void SortResults(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.ResultAscending:
                    Results.BingResults = Results.BingResults.OrderBy(item => item.Count).ToList();
                    Results.GoogleResults = Results.GoogleResults.OrderBy(item => item.Count).ToList();
                    break;

                case Sorting.ResultDescending:
                    Results.BingResults = Results.BingResults.OrderBy(item => item.Count).Reverse().ToList();
                    Results.GoogleResults = Results.GoogleResults.OrderBy(item => item.Count).Reverse().ToList();
                    break;

                case Sorting.TermAscending:
                    Results.BingResults = Results.BingResults.OrderBy(item => item.Term).ToList();
                    Results.GoogleResults = Results.GoogleResults.OrderBy(item => item.Term).ToList();
                    break;

                case Sorting.TermDescending:
                    Results.BingResults = Results.BingResults.OrderBy(item => item.Term).Reverse().ToList();
                    Results.GoogleResults = Results.GoogleResults.OrderBy(item => item.Term).Reverse().ToList();
                    break;
            }
        }

        // Display Results
        internal static void DisplayResults(ResultFormatting resultFormatting)
        {
            var resultsList = new[] { Results.BingResults, Results.GoogleResults };

            switch (resultFormatting)
            {
                case ResultFormatting.Html: // WebBrowser
                    // Save the palette image to temp directory.
                    // My.Resources.RGB.Save(Palette_Image);

                    // Write the header title.
                    File.WriteAllText(ResultsFileHtml, HtmlHeader);

                    // Write the document back color.
                    File.WriteAllText(ResultsFileHtml, string.Format(HtmlDocumentBackColor, HtmlBackColor));

                    // Loop over each list.
                    for (var i = 0; i < resultsList.Length; i++)
                    {
                        List<Result> list = resultsList[i];
                        SearchEngines engine = (SearchEngines)(i + 1);
                        if (list.Count != 0)
                        {
                            // Get the lowest and greatest result numbers to calculate the rule of 3.
                            _ruleOf3Lowest = Tuple.Create(list.OrderBy(tuple => tuple.Count).First().Count,
                                list.OrderBy(tuple => tuple.Count).First().Term);

                            _ruleOf3Greatest = Tuple.Create(list.OrderBy(tuple => tuple.Count).Last().Count,
                                list.OrderBy(tuple => tuple.Count).Last().Term);

                            // Write the Search Engine title.
                            File.AppendAllText(ResultsFileHtml,
                                string.Format(HtmlSearchEngineTitle, HtmlForeColor, engine));

                            // Loop over each result in list.
                            foreach (var result in list)
                            {
                                // Calculate the rule of 3.
                                _ruleOf3 = (result.Count - _ruleOf3Lowest.Item1) /
                                           (double)(_ruleOf3Greatest.Item1 - _ruleOf3Lowest.Item1) * 100;

                                // Set the result colors.
                                if (_ruleOf3 > 50)
                                {
                                    _green = (short)(255 - Math.Round(_ruleOf3 * 2.55));
                                    _red = 255;
                                }
                                else if (_ruleOf3 <= 50)
                                {
                                    _red = (short)Math.Round(_ruleOf3 * 5.1);
                                    _green = 255;
                                }

                                // Write the search pattern and result quantity.
                                File.AppendAllText(ResultsFileHtml,
                                    string.Format(HtmlResultFormat, HtmlForeColor, result.Term,
                                        RgbToHtml(_red, _green, 0), FormatNumber(result.Count)));
                            }

                            // Write the minimum and maximum stats.
                            File.AppendAllText(ResultsFileHtml,
                                string.Format(HtmlPaletteString, HtmlForeColor,
                                    NumberAbbreviation(_ruleOf3Greatest.Item1.ToString(), false),
                                    FormatNumber(_ruleOf3Greatest.Item1), PaletteImage,
                                    NumberAbbreviation(_ruleOf3Lowest.Item1.ToString(), false),
                                    FormatNumber(_ruleOf3Lowest.Item1)));
                        }
                    }

                    // Write the EndOfFile.
                    File.AppendAllText(ResultsFileHtml, HtmlEnd);

                    // Start the file using Shell Execute.
                    Process.Start(ResultsFileHtml);
                    break;

                case ResultFormatting.Txt: // Notepad
                    // Write the header title.
                    File.WriteAllText(ResultsFileTxt, TxtHeader);

                    // Loop over each list.
                    for (var i = 0; i < resultsList.Length; i++)
                    {
                        List<Result> list = resultsList[i];
                        SearchEngines engine = (SearchEngines)(i + 1);
                        if (list.Count != 0)
                        {
                            // Write the Search Engine title.
                            File.AppendAllText(ResultsFileTxt,
                                string.Format(TxtSearchEngineTitle, engine));

                            // Get the lowest and greatest result numbers to calculate the rule of 3.
                            _ruleOf3Lowest = Tuple.Create(list.OrderBy(tuple => tuple.Count).First().Count,
                                list.OrderBy(tuple => tuple.Count).First().Term);

                            _ruleOf3Greatest = Tuple.Create(list.OrderBy(tuple => tuple.Count).Last().Count,
                                list.OrderBy(tuple => tuple.Count).Last().Term);

                            // Loop over each result in list.
                            foreach (var result in list)

                                // Write the search pattern and result quantity.
                                File.AppendAllText(ResultsFileTxt,
                                    string.Format(TxtResultFormat, result.Term, FormatNumber(result.Count)));

                            // Write the minimum and maximum stats.
                            File.AppendAllText(ResultsFileTxt,
                                string.Format(TxtPaletteString,
                                    NumberAbbreviation(_ruleOf3Greatest.Item1.ToString(), false),
                                    FormatNumber(_ruleOf3Greatest.Item1), _ruleOf3Greatest.Item2,
                                    NumberAbbreviation(_ruleOf3Lowest.Item1.ToString(), false),
                                    FormatNumber(_ruleOf3Lowest.Item1), _ruleOf3Lowest.Item2));
                        }
                    }

                    // Start the file using Shell Execute.
                    Process.Start(ResultsFileTxt);
                    break;
            }
        }

        internal class PageSource
        {
            public IWebElement SpellOrig { get; }
            public string Source { get; }

            private PageSource()
            {
            }

            public PageSource(string source)
            {
                Source = source;
            }

            public PageSource(IWebElement spellOrig, string source)
            {
                SpellOrig = spellOrig;
                Source = source;
            }
        }
    }
}