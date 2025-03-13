using FlaUI.WebDriver.UITests.TestUtil;
using NUnit.Framework;

namespace FlaUI.WebDriver.UITests
{
    public class KeyboardTests
    {
        [Test]
        public void KeyboardResetTest()
        {
            KeyboardHelper.ResetKeyboardState();
        }
    }
}