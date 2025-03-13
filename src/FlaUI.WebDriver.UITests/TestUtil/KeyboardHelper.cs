using System;
using System.Threading;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace FlaUI.WebDriver.UITests.TestUtil
{
    public static class KeyboardHelper
    {
        public static void ResetKeyboardState()
        {
            var modifiers = new[]
            {
                VirtualKeyShort.SHIFT,
                VirtualKeyShort.CONTROL,
                VirtualKeyShort.ALT,  
                VirtualKeyShort.LWIN,
                VirtualKeyShort.RWIN
            };

            foreach (var key in modifiers)
            {
                Keyboard.Release(key);
            }
        }

        public static void Retry(System.Action action, TimeSpan timeout, int retryIntervalMs = 1000)
        {
            DateTime endTime = DateTime.Now.Add(timeout);
            Exception lastException = null;

            while (DateTime.Now < endTime)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Thread.Sleep(retryIntervalMs);
                }
            }

            // Rethrow the final exception once time is up
            throw new Exception($"Action did not succeed within {timeout.TotalSeconds} seconds.", lastException);
        }
    }
}