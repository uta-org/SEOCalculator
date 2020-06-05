using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SEO_Calculator.Extensions
{
    public static class DriverHelper
    {
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
    }
}