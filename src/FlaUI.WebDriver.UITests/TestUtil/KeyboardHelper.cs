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
    }
}