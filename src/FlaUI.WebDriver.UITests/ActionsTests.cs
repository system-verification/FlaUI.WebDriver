using System.Collections.Generic;
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
        public void LocalSetup()
        {
            var driverOptions = FlaUIDriverOptions.TestApp();
            var commandTimeout = System.TimeSpan.FromSeconds(10);
            _driver = new RemoteWebDriver(WebDriverFixture.WebDriverUrl, driverOptions.ToCapabilities(), commandTimeout);
        }

        [TearDown]
        public void Teardown()
        {
            KeyboardHelper.ResetKeyboardState();

            _driver?.ResetInputState();
            _driver?.Quit();
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

            var testValue = "a1b2cD4efGhijK6lmn a1b2cD4efGhijK6lmn";
            element.SendKeys(testValue);
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo(testValue));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_ShiftedCharacters1_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            var testValue = "aA@Bb aA$Bb 3Aa|zD 2gG~lsS Ff\\1pP <>11aA";
            element.SendKeys(testValue);
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo(testValue));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_UnicodeCharacters1_IsSupported()
        {
            if (KeyboardLayoutHelper.GetKeyboardLayoutName(KeyboardLayoutHelper.GetCurrentKeyboardLayout()) != "Swedish") {
                Assert.Ignore("Skipping beacuse it is only for a Swedish keyboard");
            }

            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            var _sendString = "aA£Bb aA€Bb 4aF¤1rR";
            element.SendKeys(_sendString);
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo(_sendString));
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
                Assert.That(refreshedElement.Text, Is.EqualTo(withSpecialCharacters));
            }, System.TimeSpan.FromSeconds(2));
        }

        [Test]
        public void SendKeys_ShiftCharacters3_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            string withSpecialCharacters = "!\"#Aa% &/(12)=?`!#%Bb^&*()+ >;:_^!\"#%&/(cC )=?Vv`!#%^&*(56)+ ><;:_*";
            element.SendKeys(withSpecialCharacters);
            KeyboardHelper.Retry(() =>
            {
                var refreshedElement = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
                Assert.That(refreshedElement.Text, Is.EqualTo(withSpecialCharacters));
            }, System.TimeSpan.FromSeconds(2));
        }
 
        [Test]
        public void XPerformActions_KeyDownKeyUp_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Click();

            new Actions(_driver)
                .KeyDown(Keys.Control)
                .KeyDown(Keys.Backspace)
                .KeyUp(Keys.Backspace)
                .KeyUp(Keys.Control)
                .Perform();

            string activeElementText = _driver.SwitchTo().ActiveElement().Text;
            Assert.That(activeElementText, Is.EqualTo("Test "));
        }

        [Test]
        public void XReleaseActions_Default_ReleasesKeys()
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
        public void XPerformActions_MoveToElementAndClick_SelectsElement()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));

            new Actions(_driver).MoveToElement(element).Click().Perform();

            string activeElementText = _driver.SwitchTo().ActiveElement().Text;
            Assert.That(activeElementText, Is.EqualTo("Test TextBox"));
        }

        [Test]
        public void XPerformActions_MoveToElement_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("LabelWithHover"));

            new Actions(_driver).MoveToElement(element).Perform();

            Assert.That(element.Text, Is.EqualTo("Hovered!"));
        }

        [Test]
        public void XPerformActions_MoveToElementMoveByOffsetAndClick_SelectsElement()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));

            new Actions(_driver).MoveToElement(element).MoveByOffset(5, 0).Click().Perform();

            string activeElementText = _driver.SwitchTo().ActiveElement().Text;
            Assert.That(activeElementText, Is.EqualTo("Test TextBox"));
        }
    }
}
