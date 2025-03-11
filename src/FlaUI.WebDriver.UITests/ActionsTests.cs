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
            var commandTimeout = System.TimeSpan.FromSeconds(6);
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

            Assert.That(element.Text, Is.EqualTo("abc123"));
        }

        [Test]
        public void SendKeys_Default2_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            element.SendKeys("aBC123");

            Assert.That(element.Text, Is.EqualTo("aBC123"));
        }

        [Test]
        public void SendKeys_Default3_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            element.SendKeys("aAbBcCeE");

            Assert.That(element.Text, Is.EqualTo("aAbBcCeE"));
        }

        [Test]
        public void SendKeys_Default4_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            element.SendKeys("a1Bc3A1D2G");

            Assert.That(element.Text, Is.EqualTo("a1Bc3A1D2G"));
        }

        [Test]
        public void SendKeys_ShiftedCharacter_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            element.SendKeys("@TEST");

            Assert.That(element.Text, Is.EqualTo("@TEST"));
        }

        [Test]
        public void SendKeys_Special_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            string withSpecialCharacters = "A1%3ee5&";
            element.SendKeys(withSpecialCharacters);

            Assert.That(element.Text, Is.EqualTo(withSpecialCharacters));
        }

        [Test]
        public void SendKeys_SpecialCharacters_IsSupported()
        {
            var element = _driver.FindElement(ExtendedBy.AccessibilityId("TextBox"));
            element.Clear();

            string withSpecialCharacters = "#a1!b2%c3&d4*e5(f6)g7";
            element.SendKeys(withSpecialCharacters);

            Assert.That(element.Text, Is.EqualTo(withSpecialCharacters));
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
