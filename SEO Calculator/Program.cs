using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using RestSharp;
using SEO_Calculator.Extensions;
using ShellProgressBar;
using static SEO_Calculator.Program;

namespace SEO_Calculator
{
    internal class Program
    {
        // Textbox Terms Hint:
        //private string Terms_Hint = string.Format("Use \"{0}\" character to separate multiple terms.", My.Settings.User_Separator);

        // POST queries:
        private static readonly string BingQuery = "https://www.bing.com/search?q=";

        // Regular Expressions:
        private static readonly string BingRegExResultsA = "id=\"count\">.+?<";

        private static readonly string BingRegExResultsB = @"[\d\.]+";

        private static readonly string GoogleQuery = "search?q={0}&nfpr=1";

        private static readonly string GoogleRegExResultsA = "[^#]result-stats.+?<";
        private static readonly string GoogleRegExResultsB = @"[\d\,]+";

        private static readonly string HtmlDocumentBackColor =
            "<body style=\"background-color:{0};\">" + Environment.NewLine + Environment.NewLine;

        private static readonly string HtmlEnd = "</body></html>";

        // Html file Strings:
        private static readonly string HtmlHeader =
            "<html><head></head><body><!--- SEO Calculator By z3nth10n -->" + Environment.NewLine +
            Environment.NewLine;

        private static readonly string HtmlPaletteString =
            " <br><span style=\"color:{0};font-family:Arial;font-size:12px;margin-top:-12px;position:relative;padding-right:12px;\"> {1} ( {2} )</span><img src=\"file:///{3}\" style=\"width:54px; height:29px;\"><span style=\"color:{0};font-family:Arial;font-size:12px;margin-top:-12px;position:relative;padding-left:12px;\" /> {4} ( {5} )</span> <br>" +
            Environment.NewLine + Environment.NewLine;

        private static readonly string HtmlResultFormat =
            "<b style=\"color:{0};\">The search \"{1}\" has obtained <font style=\"color: {2} ;\"> {3} results.</font></b><br>" +
            Environment.NewLine + Environment.NewLine;

        private static readonly string HtmlSearchEngineTitle =
            "<h2 style=\"color:{0};\">{1} Results:</h2><br>" + Environment.NewLine + Environment.NewLine;

        private static readonly string PaletteImage = Path.Combine(Path.GetTempPath(), "SEO Palette.png");

        // Temporary files:
        private static readonly string ResultsFileHtml = Path.Combine(Path.GetTempPath(), "SEO Calculator.html");

        private static readonly string ResultsFileTxt = Path.Combine(Path.GetTempPath(), "SEO Calculator.txt");

        // Txt file Strings:
        private static readonly string TxtHeader = ".:: SEO Calculator By z3nth10n ::." +
                                             Environment.NewLine +
                                             "    =============================================    " +
                                             Environment.NewLine + Environment.NewLine;

        private static readonly string TxtPaletteString = "Maximum: {0} ({1}) \"{2}\"" + Environment.NewLine +
                                                     "Minimum: {3} ({4}) \"{5}\"" + Environment.NewLine +
                                                     Environment.NewLine + Environment.NewLine;

        private static readonly string TxtResultFormat = "The search \"{0}\" has obtained {1} results."
                                                    + Environment.NewLine + Environment.NewLine;

        private static readonly string TxtSearchEngineTitle = "{0} Results:" + Environment.NewLine + Environment.NewLine;

        // Lists:
        //private static List<Tuple<string, string, long>> Bing_Results_List = new List<Tuple<string, string, long>>();

        //private static List<Tuple<string, string, long>> Google_Results_List = new List<Tuple<string, string, long>>();

        private static short _green;

        // Progress Counter
        private static int _indexPattern = 0;

        // RGB Table Rule of three
        private static short _red;

        // Rule of three
        private static double _ruleOf3;

        private static Tuple<long, string> _ruleOf3Greatest;

        private static Tuple<long, string> _ruleOf3Lowest;

        // [PROP] SearchPatterns
        private static string[] Terms => File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, "search.txt"));

        // [PROP] SearchEngine
        private static SearchEngines SearchEngine => SearchEngines.Google;

        // [PROP] ResultFormat
        private static ResultFormatting ResultFormat => ResultFormatting.Html;

        // [PROP] ResultSorting
        private static Sorting ResultSorting => Sorting.ResultDescending;

        // [PROP] Separator
        private static char Separator => Convert.ToChar(Environment.NewLine);

        // [PROP] HTML_BackColor
        private static string HtmlBackColor => ColorTranslator.ToHtml(Color.Black);

        // [PROP] HTML_ForeColor
        private static string HtmlForeColor => ColorTranslator.ToHtml(Color.White);

        //private static WebClient WebClient;

        private static IWebDriver web;

        private const string SearchBarXPath = "/html/body/div[3]/form/div[2]/div[1]/div[2]/div/div[2]/input";

        private const string SearchButtonXPath = "/html/body/div[3]/form/div[2]/div[1]/div[2]/button";

        private const string ResultStatsId = "result-stats";

        private static bool exitSystem = false;

        #region Trap application termination

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);

        private static EventHandler _handler;

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            Console.WriteLine("Exiting system due to external CTRL-C, or process kill, or shutdown");

            OnExit();

            //allow main to run off
            exitSystem = true;

            //shutdown right away so there are no lingering threads
            Environment.Exit(-1);

            return true;
        }

        #endregion Trap application termination

        private static void Main(string[] args)
            => MainAsync(args).GetAwaiter().GetResult();

        private static async Task MainAsync(string[] args)
        {
            // Some biolerplate to react to close window event, CTRL-C, kill, etc
            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);

            using (web = CreateDriver())
            {
                await GenerateResults(ResultSorting);
            }

            DisplayResults(ResultFormat);

            //WebClient.Dispose();

            //Console.Read();
        }

        private static void OnExit()
        {
            //web.Close();
            //web.Quit();
            //web.Dispose();
        }

        private static ChromeDriver CreateDriver()
        {
            const int width = 1040,
                      height = 890;

            const string userAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.122 Safari/537.36";

            var chromeOptions = new ChromeOptions();

            chromeOptions.AddArgument("--no-sandbox");
            chromeOptions.AddArgument("disable-extensions");
            chromeOptions.AddArguments("--disable-blink-features");
            chromeOptions.AddArguments("--disable-blink-features=AutomationControlled");

            chromeOptions.AddArguments("--lang=en");

            chromeOptions.AddExcludedArgument("enable-automation");
            //chromeOptions.AddAdditionalChromeOption("useAutomationExtension", false);

            chromeOptions.AddUserProfilePreference("credentials_enable_service", false);
            chromeOptions.AddUserProfilePreference("profile.password_manager_enable", false);

            chromeOptions.AddArgument($@"--user-agent=""{userAgent}""");

            chromeOptions.AddArgument("--user-data-dir=chrome-data");
            chromeOptions.AddArgument($"--window-size={width},{height}");

            // ReSharper disable once JoinDeclarationAndInitializer
            ChromeDriver driver;

            //if (IsLinux)
            //{
            //    // Thanks to: https://stackoverflow.com/questions/41133391/which-chromedriver-version-is-compatible-with-which-chrome-browser-version
            //    // https://chromium.woolyss.com/

            //    var service = ChromeDriverService.CreateDefaultService();

            //    service.LogPath = Path.Combine(Environment.CurrentDirectory, "logs", "client.log");
            //    Console.WriteLine(service.LogPath);

            //    if (File.Exists(service.LogPath))
            //        F.SetBackupFile(service.LogPath);

            //    if (!Directory.Exists(service.LogPath.ParentDirectory()))
            //        Directory.CreateDirectory(service.LogPath.ParentDirectory());

            //    service.EnableVerboseLogging = true;

            //    chromeOptions.AddArguments("headless");
            //    chromeOptions.AddArgument("--remote-debugging-port=9222");

            //    //chromeOptions.AddArgument("--disable-extensions");
            //    //chromeOptions.AddArgument("--proxy-server='direct://'");
            //    //chromeOptions.AddArgument("--proxy-bypass-list=*");
            //    //chromeOptions.AddArgument("--disable-gpu");
            //    //chromeOptions.AddArgument("--disable-dev-shm-usage");
            //    //chromeOptions.AddArgument("--no-sandbox");
            //    //chromeOptions.AddArgument("--ignore-certificate-errors");

            //    driver = new ChromeDriver(service, chromeOptions);
            //}
            //else
            driver = new ChromeDriver(chromeOptions);

            var paramsForDisableDriver = new Dictionary<string, object>
            {
                {"source", "Object.defineProperty(navigator, 'webdriver', { get: () => undefined })"}
            };

            driver.ExecuteChromeCommand("Page.addScriptToEvaluateOnNewDocument", paramsForDisableDriver);

            var paramsForUserAgent = new Dictionary<string, object>
            {
                { "userAgent", userAgent },
                { "platform", "Windows" }
            };

            driver.ExecuteChromeCommand("Network.setUserAgentOverride", paramsForUserAgent);

            /*
            var botResult = driver.ExecuteJavaScript<ReadOnlyCollection<object>>(Resources.botdetection_js);
            if (!botResult.IsNullOrEmpty())
                Console.WriteLine($"Bot detected!\r\n{string.Join(Environment.NewLine, botResult)}", Color.Aqua);
            */

            //var botResult = driver.ExecuteJavaScript<bool>(Resources.botdetection_js);
            //if (botResult)
            //    Console.WriteLine("Bot detected!", Color.Aqua);

            return driver;
        }

        // Get URL SourceCode
        private static async Task<string> GetUrlSourceCode(SearchEngines engine, string url, string term, bool useRest = false)
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

            if (!useRest)
                url = engineUrl + url;

            //var client = new RestClient(engineUrl);
            //var request = new RestRequest(url);
            //var get = client.GetAsync<string>(request);

            //await get;

            //return get.Result;

            try
            {
                //return new StreamReader(WebRequest.Create(url).GetResponse().GetResponseStream() ??
                //                        throw new InvalidOperationException()).ReadToEnd();

                //if (WebClient == null)
                //    WebClient = new WebClient();

                //var task = Task.Run(() => new StreamReader(WebRequest.Create(url).GetResponse().GetResponseStream() ??
                //                                                                   throw new InvalidOperationException()).ReadToEnd());

                //var task = Task.Run(() =>
                //{
                //    var request = WebRequest.Create(url);
                //    // request.Timeout = 1000;
                //    using (var response = request.GetResponse())
                //    using (var responseStream = response.GetResponseStream())
                //    {
                //        if (responseStream == null)
                //            throw new InvalidOperationException();

                //        using (var reader = new StreamReader(responseStream))
                //            return reader.ReadToEnd();
                //    }
                //});

                //await task;
                //return task.Result;

                var task = Task.Run(() =>
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

                    // TODO: Check for bot confirmation

                    return web.PageSource;
                });

                await task;
                return task.Result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // RGB To HTML
        private static string RgbToHtml(short r, short g, short b)
        {
            return ColorTranslator.ToHtml(Color.FromArgb(r, g, b));
        }

        // Format Number
        private static string FormatNumber(object number)
        {
            if (number is short || number is int ||
                number is long)
                return Strings.FormatNumber(number, IncludeLeadingDigit: TriState.False);
            return Strings.FormatNumber(number, IncludeLeadingDigit: TriState.False);
        }

        // Number Abbreviation
        private static string NumberAbbreviation(string quantity, bool rounded = true)
        {
            var abbreviation = string.Empty;

            //if (Quantity is short || Quantity is int ||
            //    Quantity is long)
            //    Quantity = Strings.FormatNumber(Quantity, TriState.False);
            //else
            //    Quantity = Strings.FormatNumber(Quantity, IncludeLeadingDigit: TriState.False);

            switch (quantity.Count(character => character == Convert.ToChar(".")))
            {
                case 0:
                    return quantity;

                case 1:
                    abbreviation = "kilos";
                    break;

                case 2:
                    abbreviation = "Millions";
                    break;

                case 3:
                    abbreviation = "Billions";
                    break;

                case 4:
                    abbreviation = "Trillions";
                    break;

                case 5:
                    abbreviation = "Quadrillions";
                    break;

                case 6:
                    abbreviation = "Quintuillions";
                    break;

                case 7:
                    abbreviation = "Sextillions";
                    break;

                case 8:
                    abbreviation = "Septillions";
                    break;

                default:
                    return quantity;
            }

            return rounded
                ? $"{Strings.StrReverse(Strings.StrReverse(quantity).Substring(Strings.StrReverse(quantity).LastIndexOf(".", StringComparison.Ordinal) + 1))} {abbreviation}"
                : $"{Strings.StrReverse(Strings.StrReverse(quantity).Substring(Strings.StrReverse(quantity).LastIndexOf(".", StringComparison.Ordinal) - 1))} {abbreviation}";
        }

        // Get Bing Results
        private static async Task<long> GetBingResults(string searchPattern)
        {
            var source = GetUrlSourceCode(SearchEngines.Bing, $"{BingQuery}{searchPattern}", searchPattern);
            await source;

            try
            {
                return Convert.ToInt64(Convert
                    .ToString(Regex.Match(Convert.ToString(Regex.Match(source.Result, BingRegExResultsA).Groups[0]),
                            BingRegExResultsB).Groups[0]
                    ).Replace(".", ""));
            }
            catch
            {
                return 0;
            }
        }

        // Get Google Results
        private static async Task<long> GetGoogleResults(string searchPattern)
        {
            var source = GetUrlSourceCode(SearchEngines.Google, string.Format(GoogleQuery, searchPattern), searchPattern);
            await source;

            try
            {
                return Convert.ToInt64(Convert
                    .ToString(Regex.Match(Convert.ToString(Regex.Match(source.Result, GoogleRegExResultsA).Groups[0]),
                            GoogleRegExResultsB).Groups[0]
                    ).Replace(",", ""));
            }
            catch
            {
                return 0;
            }
        }

        private static async Task GenerateResults(Sorting sorting)
        {
            await Results.Generate();

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
        private static void DisplayResults(ResultFormatting resultFormatting)
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

        // [ENUM] SearchEngines
        internal enum SearchEngines
        {
            All = 0,
            Bing = 1,
            Google = 2
        }

        // [ENUM] Sorting
        private enum Sorting
        {
            TermAscending = 0,
            TermDescending = 1,
            ResultAscending = 2,
            ResultDescending = 3
        }

        // [ENUM] ResultFormatting
        private enum ResultFormatting
        {
            Txt = 0,
            Html = 1
        }

        internal class Result
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

        internal static class Results
        // : Tuple<SearchEngines, Result, Result>
        {
            //public SearchEngines Engine => base.Item1;
            //public List<Result> GoogleResults => base.Item2;

            //private new SearchEngines Item1 => default;
            //private new Result Item2 => null;

            //public Results(SearchEngines item1, Result item2) : base(item1, item2)
            //{
            //}

            // private static ProgressBar progressBar;

            static Results()
            {
            }

            public static List<Result> GoogleResults { get; set; } = new List<Result>();

            public static List<Result> BingResults { get; set; } = new List<Result>();

            // Generate Result Lists
            internal static async Task Generate()
            {
                BingResults.Clear();
                GoogleResults.Clear();

                var options = new ProgressBarOptions
                {
                    ProgressCharacter = '─',
                    ProgressBarOnBottom = true
                };

                var terms = Terms.Where(term => !string.IsNullOrWhiteSpace(term));
                int termsCount = terms.Count();

                int count = 0;
                using (var pbar = new ProgressBar(Terms.Length, $"Step 0 of {termsCount}: ", options))
                {
                    foreach (var term in terms)
                    {
                        //if (term != string.Empty)
                        //    UpdateProgress();

                        switch (SearchEngine)
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
}