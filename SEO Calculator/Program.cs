using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenQA.Selenium;
using SEO_Calculator.Extensions;
using static SEO_Calculator.Core.SEO;

namespace SEO_Calculator
{
    internal class Program
    {
        public static IWebDriver Web { get; private set; }

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

            //await DriverHelper.ConsumeDriver(async web => await GenerateResults(ResultSorting));
            using (Web = DriverHelper.CreateDriver())
            {
                await GenerateResults(ResultSorting);
            }

            DisplayResults(ResultFormat);

            //Console.Read();
        }

        private static void OnExit()
        {
            //web.Close();
            //web.Quit();
            //web.Dispose();
        }
    }
}