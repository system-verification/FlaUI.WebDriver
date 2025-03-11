using System;
using System.Runtime.InteropServices;
using FlaUI.WebDriver;
using FlaUI.Core.WindowsAPI;
using NUnit.Framework;
using NUnit.Framework.Legacy;

[TestFixture]
public class KeysTests
{
    [DllImport("user32.dll")]
    private static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [Test]
    public void GetVirtualKey_MapsSpecialKeys()
    {
        // WebDriver special keys should map to the correct VirtualKeyShort
        ClassicAssert.AreEqual(VirtualKeyShort.RETURN, Keys.GetVirtualKey(Keys.Enter));
        ClassicAssert.AreEqual(VirtualKeyShort.TAB, Keys.GetVirtualKey(Keys.Tab));
        ClassicAssert.AreEqual(VirtualKeyShort.SPACE, Keys.GetVirtualKey(Keys.Space));
        ClassicAssert.AreEqual(VirtualKeyShort.ESCAPE, Keys.GetVirtualKey(Keys.Escape));
        ClassicAssert.AreEqual(VirtualKeyShort.LEFT, Keys.GetVirtualKey(Keys.Left));
        ClassicAssert.AreEqual(VirtualKeyShort.F1, Keys.GetVirtualKey(Keys.F1));
        // Meta/Command key
        ClassicAssert.AreEqual(VirtualKeyShort.LWIN, Keys.GetVirtualKey(Keys.Meta));
    }

    [Test]
    public void GetVirtualKey_MapsCharacterKeys()
    {
        // Letters (case-insensitive mapping to same key)
        ClassicAssert.AreEqual(VirtualKeyShort.KEY_A, Keys.GetVirtualKey("a"));
        ClassicAssert.AreEqual(VirtualKeyShort.KEY_A, Keys.GetVirtualKey("A"));
        ClassicAssert.AreEqual(VirtualKeyShort.KEY_Z, Keys.GetVirtualKey("Z"));
        // Digits
        ClassicAssert.AreEqual(VirtualKeyShort.KEY_0, Keys.GetVirtualKey("0"));
        ClassicAssert.AreEqual(VirtualKeyShort.KEY_1, Keys.GetVirtualKey("1"));
        ClassicAssert.AreEqual(VirtualKeyShort.KEY_9, Keys.GetVirtualKey("9"));
        // Symbols (that are typeable via Shift/AltGr on a standard layout)
        ClassicAssert.IsTrue(Keys.IsTypeable("@"), "Pre-condition: '@' should be typeable on current layout");
        VirtualKeyShort vkAt = Keys.GetVirtualKey("@");
        // '@' on US layout should map to '2' key; on other layouts it will map to the respective key.
        ClassicAssert.AreNotEqual(VirtualKeyShort.PACKET, vkAt, "'@' should map to a physical key on supported layouts");
        // Whitespace
        ClassicAssert.AreEqual(VirtualKeyShort.SPACE, Keys.GetVirtualKey(" "));
    }

    [Test]
    public void GetVirtualKey_ReturnsPacketForUnmappedChars()
    {
        // Characters not directly produced by the keyboard should map to VirtualKeyShort.PACKET
        ClassicAssert.AreEqual(VirtualKeyShort.PACKET, Keys.GetVirtualKey("Ω"), "Greek omega is not on a standard US/EU keyboard");
        ClassicAssert.AreEqual(VirtualKeyShort.PACKET, Keys.GetVirtualKey("𠀋"), "Unicode character outside BMP should return PACKET");
    }

    [Test]
    public void GetVirtualKey_InvalidInput_ThrowsException()
    {
        // Null or empty string should throw an ArgumentException
        ClassicAssert.Throws<ArgumentException>(() => Keys.GetVirtualKey(null));
        ClassicAssert.Throws<ArgumentException>(() => Keys.GetVirtualKey(string.Empty));
        // Multiple-character string (not a single key cluster) should throw
        ClassicAssert.Throws<ArgumentException>(() => Keys.GetVirtualKey("AB"));
    }

    [Test]
    public void IsTypeable_ReturnsTrueForTypeableInputs()
    {
        // Simple letters and digits
        ClassicAssert.IsTrue(Keys.IsTypeable("hello"), "Lowercase letters should be typeable");
        ClassicAssert.IsTrue(Keys.IsTypeable("HELLO"), "Uppercase letters (with Shift) should be typeable");
        ClassicAssert.IsTrue(Keys.IsTypeable("12345"), "Digits should be typeable");
        ClassicAssert.IsTrue(Keys.IsTypeable("Hello123"), "Alphanumeric strings should be typeable");
        // Including space and punctuation that exist on keyboard
        ClassicAssert.IsTrue(Keys.IsTypeable("Hi there!"), "Letters, space, and '!' are typeable (exclamation via Shift+1)");
        // Special keys
        ClassicAssert.IsTrue(Keys.IsTypeable(Keys.Enter), "Enter key should be typeable (physical key exists)");
        ClassicAssert.IsTrue(Keys.IsTypeable(Keys.Tab), "Tab key should be typeable");
    }

    [Test]
    public void IsTypeable_ReturnsFalseForNonTypeableInputs()
    {
        // Characters not present on current keyboard layout
        ClassicAssert.IsFalse(Keys.IsTypeable("Ω"), "Omega character should not be typeable on standard English layout");
        ClassicAssert.IsFalse(Keys.IsTypeable("你好"), "CJK characters are not directly typeable on a European keyboard layout");
        // Emoji (surrogate pair)
        string emoji = "😃";  // U+1F603
        ClassicAssert.IsFalse(Keys.IsTypeable(emoji), "Emoji should not be directly typeable via keyboard");
        // Null or empty
        ClassicAssert.IsFalse(Keys.IsTypeable(null), "Null input is not typeable");
        ClassicAssert.IsFalse(Keys.IsTypeable(string.Empty), "Empty string is not typeable");
        // Sequence containing only WebDriver Null keys should be not typeable (no actual output)
        string nullSequence = Keys.Null + Keys.Null;
        ClassicAssert.IsFalse(Keys.IsTypeable(nullSequence), "Sequence of Null keys produces no typeable output");
    }

    [Test]
    public void GetVirtualKey_MapsPunctuation_AccordingToSystemLayout()
    {
        // Get the current keyboard layout handle.
        IntPtr hkl = GetKeyboardLayout(0);
        
        // Define the punctuation string to test.
        string punctuation = "!`\"#%&/()=?`^~*'-_.:,;<>|\\";
        
        foreach (char c in punctuation)
        {
            string s = c.ToString();
            // Get the expected virtual key (ignoring modifiers) using Windows API.
            // VkKeyScanEx returns a short; the low-order byte contains the virtual key code.
            short vkScan = VkKeyScanEx(c, hkl);
            VirtualKeyShort expected = (VirtualKeyShort)(vkScan & 0xFF);
            
            // Get the virtual key from your Keys class.
            VirtualKeyShort actual = Keys.GetVirtualKey(s);
            
            ClassicAssert.AreEqual(expected, actual, 
                $"Character '{c}' expected to map to {expected} but mapped to {actual}");
        }
    }
}
