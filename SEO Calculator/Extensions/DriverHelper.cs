using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SEO_Calculator.Extensions
{
    public static class DriverHelper
    {
        //public static IWebDriver Web { get; private set; }

        private const string TimeoutExceptionMessage = @"internalLoopMilliseconds must be smaller than the timeout!";

        public static IWebElement WaitForElement(this IWebDriver web, By by, int timeoutMilliseconds = 5000, int internalLoopMilliseconds = 100, params Type[] ignoreExceptionTypes)
        {
            // TODO: Not working stacktrace check!
            if (timeoutMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

            if (timeoutMilliseconds < internalLoopMilliseconds)
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), TimeoutExceptionMessage);

            var wait = new WebDriverWait(web, TimeSpan.FromMilliseconds(timeoutMilliseconds));

            // typeof(NoSuchElementException), typeof(ElementNotVisibleException)

            if (ignoreExceptionTypes != null)
                wait.IgnoreExceptionTypes(ignoreExceptionTypes);
            else
                wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(ElementNotVisibleException));

            var element = wait.Until(driver =>
            {
                IWebElement e;

                do
                {
                    Thread.Sleep(internalLoopMilliseconds);
                    e = driver.FindElement(by);
                }
                while (e?.Displayed == false);

                return e;
            });

            return element;
        }

        public static ChromeDriver CreateDriver()
        {
            return CreateDriver(-1);
        }

        public static ChromeDriver CreateDriver(int index)
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

            var datadir = index > -1 ? $"chrome-data-{index}" : "chrome-data";
            var folder = Path.Combine(Environment.CurrentDirectory, datadir);
            if (Directory.Exists(folder)) IOHelper.DeleteDirectory(folder);
            // Directory.Delete(folder, true);
            chromeOptions.AddArgument($"--user-data-dir={datadir}");
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

            if (index > -1)
            {
                ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                service.Port = service.Port + index;

                driver = new ChromeDriver(service, chromeOptions);
            }
            else
            {
                driver = new ChromeDriver(chromeOptions);
            }

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

            return driver;
        }

        public static IWebElement FindElementOrDefault(this IWebDriver web, By by)
        {
            try
            {
                return web.FindElement(by);
            }
            catch
            {
                return null;
            }
        }
    }
}