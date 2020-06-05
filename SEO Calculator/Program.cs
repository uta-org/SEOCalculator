using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenQA.Selenium;
using SEO_Calculator.Extensions;
using SEO_Calculator.Model;
using ShellProgressBar;
using static SEO_Calculator.Core.SEO;

namespace SEO_Calculator
{
    internal class Program
    {
        public static IWebDriver Web { get; private set; }

        private const int MAX_INSTANCES = 5;

        private static bool exitSystem;

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

            var tasks = new List<Task>();

            var terms = Results.GetNotNullTerms(Terms, out int count);

            var options = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ProgressBarOnBottom = true
            };

            using (var progressBar = new ProgressBar(count, $"Step 0 of {count}: ", options))
            {
                for (int i = 0; i < MAX_INSTANCES; i++)
                {
                    var i1 = i;
                    tasks.Add(new Task(async () =>
                    {
                        using (Web = DriverHelper.CreateDriver())
                        {
                            await GenerateResults(progressBar, terms, GetMin(i1, count), GetMax(i1, count), i1 == 0);
                        }
                    }));
                }

                Parallel.ForEach(tasks, task => task.Start());

                await Task.WhenAll(tasks);
            }

            SortResults(ResultSorting);
            DisplayResults(ResultFormat);

            //Console.Read();
        }

        private static int GetMin(int i, int count)
        {
            return (int)(i * (count / (float)MAX_INSTANCES));
        }

        private static int GetMax(int i, int count)
        {
            if (i + 1 == MAX_INSTANCES) return count;
            return (int)(i + 1 * (count / (float)MAX_INSTANCES));
        }

        private static void OnExit()
        {
            //web.Close();
            //web.Quit();
            //web.Dispose();
        }
    }
}