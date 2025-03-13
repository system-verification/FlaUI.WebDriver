using System.Threading;
using FlaUI.WebDriver.UITests.TestUtil;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;

namespace FlaUI.WebDriver.UITests
{
    [TestFixture]
    public class ActionsTests
    {
        private RemoteWebDriver _driver;

        [SetUp]
        public void Setup()
        {
            var driverOptions = FlaUIDriverOptions.TestApp();
            var commandTimeout = System.TimeSpan.FromSeconds(1000);
            _driver = new RemoteWebDriver(WebDriverFixture.WebDriverUrl, driverOptions.ToCapabilities(), commandTimeout);
        }

        [TearDown]
        public void Teardown()
        {
            KeyboardHelper.ResetKeyboardState();
            _driver?.ResetInputState();
            _driver?.Dispose();
        }

        [Test]
        public void PerformActions_KeyDownKeyUp_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Click();

            new Actions(_driver).KeyDown(Keys.Control).KeyDown(Keys.Backspace).KeyUp(Keys.Backspace).KeyUp(Keys.Control).Perform();

            string activeElementText = _driver.SwitchTo().ActiveElement().Text;
            Assert.That(activeElementText, Is.EqualTo("Test "));
        }

        [Test]
        public void SendKeys_Default_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            element.SendKeys("abc123");

            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo("abc123"));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_Default2_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            element.SendKeys("aBC123");

            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo("aBC123"));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_Default3_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            element.SendKeys("aAb1B cC3 eE8");
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo("aAb1B cC3 eE8"));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_Default4_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            element.SendKeys("a1b2cD4efGhijK6lmn");
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo("a1b2cD4efGhijK6lmn"));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_ShiftedCharacters1_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            //element.SendKeys("A@B a£b 2$5 3|z 2~l");
            element.SendKeys("a@B A£b");
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo("a@B A£b"));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_ShiftCharacters2_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            string withSpecialCharacters = "A1%3ee5&";
            element.SendKeys(withSpecialCharacters);
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo("A1%3ee5&"));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_ShiftCharacters3_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            string withSpecialCharacters = "!\"#% &/()=?`!#%^&*()+ >;:_^!\"#%&/( )=?`!#%^&*()+ ><;:_*";
            element.SendKeys(withSpecialCharacters);
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo("!\"#% &/()=?`!#%^&*()+ >;:_^!\"#%&/( )=?`!#%^&*()+ ><;:_*"));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_UnicodeCharacters_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            string withUnicodeCharacters = "AB ¤";
            element.SendKeys(withUnicodeCharacters);
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo(withUnicodeCharacters));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void ReleaseActions_Default_ReleasesKeys()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Click();
            new Actions(_driver).KeyDown(Keys.Control).Perform();

            _driver.ResetInputState();

            new Actions(_driver).KeyDown(Keys.Backspace).KeyUp(Keys.Backspace).Perform();
            string activeElmentText = _driver.SwitchTo().ActiveElement().Text;
            Assert.That(activeElmentText, Is.EqualTo("Test TextBo"));
        }

        [Test]
        public void PerformActions_MoveToElementAndClick_SelectsElement()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));

            new Actions(_driver).MoveToElement(element).Click().Perform();

            string activeElementText = _driver.SwitchTo().ActiveElement().Text;
            Assert.That(activeElementText, Is.EqualTo("Test TextBox"));
        }

        [Test]
        public void PerformActions_MoveToElement_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("LabelWithHover"));

            new Actions(_driver).MoveToElement(element).Perform();

            Assert.That(element.Text, Is.EqualTo("Hovered!"));
        }

        [Test]
        public void PerformActions_MoveToElementMoveByOffsetAndClick_SelectsElement()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));

            new Actions(_driver).MoveToElement(element).MoveByOffset(5, 0).Click().Perform();

            string activeElementText = _driver.SwitchTo().ActiveElement().Text;
            Assert.That(activeElementText, Is.EqualTo("Test TextBox"));
        }
    }
}
