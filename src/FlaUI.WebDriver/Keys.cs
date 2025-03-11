using System;
using System.Runtime.InteropServices;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace FlaUI.WebDriver
{
    internal static class Keys
    {
        // Special key constants (WebDriver key representations)
        public const string Null = "\uE000";
        public const string Cancel = "\uE001";
        public const string Help = "\uE002";
        public const string Backspace = "\uE003";
        public const string Tab = "\uE004";
        public const string Clear = "\uE005";
        public const string Return = "\uE006";
        public const string Enter = "\uE007";
        public const string LeftShift = "\uE008";
        public const string Control = "\uE009";
        public const string Alt = "\uE00A";
        public const string Pause = "\uE00B";
        public const string Escape = "\uE00C";
        public const string Space = "\uE00D";
        public const string PageUp = "\uE00E";
        public const string PageDown = "\uE00F";
        public const string End = "\uE010";
        public const string Home = "\uE011";
        public const string LeftArrow = "\uE012";
        public const string Left = LeftArrow;
        public const string UpArrow = "\uE013";
        public const string RightArrow = "\uE014";
        public const string DownArrow = "\uE015";
        public const string Insert = "\uE016";
        public const string Delete = "\uE017";
        public const string Meta = "\uE03D";
        public const string F1 = "\uE031";
        public const string F2 = "\uE032";
        public const string F3 = "\uE033";
        public const string F4 = "\uE034";
        public const string F5 = "\uE035";
        public const string F6 = "\uE036";
        public const string F7 = "\uE037";
        public const string F8 = "\uE038";
        public const string F9 = "\uE039";
        public const string F10 = "\uE03A";
        public const string F11 = "\uE03B";
        public const string F12 = "\uE03C";


        /// <summary>
        /// Normalizes a key value (from WebDriver actions) to the canonical single-character representation.
        /// For example, "Control" (or "ctrl") becomes the constant Keys.Control (i.e. "\uE009"), "Enter" becomes Keys.Enter, etc.
        /// If the input is already a single character, it is returned as-is.
        /// </summary>
        public static string GetNormalizedKeyValue(string keyValue)
        {
            if (string.IsNullOrEmpty(keyValue))
            {
                return keyValue;
            }
            if (keyValue.Length == 1)
            {
                return keyValue;
            }
            string name = keyValue.Trim();
            string lowerName = name.ToLowerInvariant();
            switch (lowerName)
            {
                case "null":
                    return Null;
                case "shift":
                case "leftshift":
                    return LeftShift;
                case "control":
                case "ctrl":
                case "leftcontrol":
                    return Control;
                case "alt":
                case "leftalt":
                    return Alt;
                case "meta":
                case "command":
                case "win":
                case "windows":
                    return Meta;
                case "enter":
                case "return":
                    return Enter;
                case "backspace":
                case "back":
                    return Backspace;
                case "tab":
                    return Tab;
                case "clear":
                    return Clear;
                case "pause":
                    return Pause;
                case "escape":
                case "esc":
                    return Escape;
                case "space":
                case "spacebar":
                    return Space;
                case "pageup":
                    return PageUp;
                case "pagedown":
                    return PageDown;
                case "end":
                    return End;
                case "home":
                    return Home;
                case "leftarrow":
                case "left":
                    return LeftArrow;
                case "uparrow":
                case "up":
                    return UpArrow;
                case "rightarrow":
                case "right":
                    return RightArrow;
                case "downarrow":
                case "down":
                    return DownArrow;
                case "insert":
                    return Insert;
                case "delete":
                    return Delete;
                case "help":
                    return Help;
                case "cancel":
                    return Cancel;
                default:
                    if (lowerName.StartsWith("key_") && lowerName.Length == 5)
                    {
                        var remainder = keyValue.Substring(4);
                        // Only strip the prefix if the character is a letter or digit.
                        if (char.IsLetterOrDigit(remainder[0]))
                        {
                            return remainder;
                        }
                    }
                    return keyValue;
            }
        }


        /// <summary>
        /// Gets the VirtualKeyShort code for a given key value string (after normalization).
        /// Throws an exception if the key cannot be mapped to a virtual key (e.g., Keys.Null or unmappable Unicode characters).
        /// The returned VirtualKeyShort can be used with Keyboard.Press and Keyboard.Release.
        /// </summary>
        // public static VirtualKeyShort GetCode(string keyValue)
        // {
        //     if (string.IsNullOrEmpty(keyValue))
        //     {
        //         throw new ArgumentException("Key value cannot be null or empty.", nameof(keyValue));
        //     }
        //     string normKey = GetNormalizedKeyValue(keyValue);
        //     if (string.IsNullOrEmpty(normKey))
        //     {
        //         throw new ArgumentException($"Invalid key value: \"{keyValue}\"", nameof(keyValue));
        //     }
        //     // Null key does not correspond to a physical key code
        //     if (normKey == Null)
        //     {
        //         throw new InvalidOperationException("Keys.Null does not correspond to a physical key code.");
        //     }
        //     if (normKey.Length != 1)
        //     {
        //         // After normalization, we expect a single character; otherwise, it's invalid
        //         throw new ArgumentException($"Invalid key value: \"{keyValue}\"", nameof(keyValue));
        //     }
        //     char keyChar = normKey[0];
        //     // Handle special key Unicode values explicitly
        //     switch (keyChar)
        //     {
        //         case '\uE001': // Cancel (Break)
        //             return VirtualKeyShort.CANCEL;
        //         case '\uE002': // Help
        //             // Map to VK_HELP (0x2F)
        //             return (VirtualKeyShort)0x2F;
        //         case '\uE003': // Backspace
        //             return VirtualKeyShort.BACK;
        //         case '\uE004': // Tab
        //             return VirtualKeyShort.TAB;
        //         case '\uE005': // Clear (NumPad 5 when NumLock off)
        //             return VirtualKeyShort.CLEAR;
        //         case '\uE006': // Return
        //         case '\uE007': // Enter
        //             return VirtualKeyShort.RETURN;
        //         case '\uE008': // Shift (use left shift)
        //             return VirtualKeyShort.LSHIFT;
        //         case '\uE009': // Control (use left control)
        //             return VirtualKeyShort.LCONTROL;
        //         case '\uE00A': // Alt (use left alt)
        //             return VirtualKeyShort.LMENU;
        //         case '\uE00B': // Pause
        //             return VirtualKeyShort.PAUSE;
        //         case '\uE00C': // Escape
        //             return VirtualKeyShort.ESCAPE;
        //         case '\uE00D': // Space
        //             return VirtualKeyShort.SPACE;
        //         case '\uE00E': // Page Up
        //             return VirtualKeyShort.PRIOR;
        //         case '\uE00F': // Page Down
        //             return VirtualKeyShort.NEXT;
        //         case '\uE010': // End
        //             return VirtualKeyShort.END;
        //         case '\uE011': // Home
        //             return VirtualKeyShort.HOME;
        //         case '\uE012': // Left Arrow
        //             return VirtualKeyShort.LEFT;
        //         case '\uE013': // Up Arrow
        //             return VirtualKeyShort.UP;
        //         case '\uE014': // Right Arrow
        //             return VirtualKeyShort.RIGHT;
        //         case '\uE015': // Down Arrow
        //             return VirtualKeyShort.DOWN;
        //         case '\uE016': // Insert
        //             return VirtualKeyShort.INSERT;
        //         case '\uE017': // Delete
        //             return VirtualKeyShort.DELETE;
        //         case '\uE03D': // Meta (Windows/Command key)
        //             return VirtualKeyShort.LWIN;
        //         default:
        //             // For normal character keys, use VkKeyScan to get the virtual-key code
        //             short vkScan = User32.VkKeyScan(keyChar);
        //             if (vkScan == -1)
        //             {
        //                 // Character is not supported by the current keyboard layout
        //                 throw new InvalidOperationException($"No virtual key code found for character '{keyChar}' in the current keyboard layout.");
        //             }
        //             // The low-order byte is the virtual-key code
        //             byte vkCode = (byte)(vkScan & 0xFF);
        //             return (VirtualKeyShort)vkCode;
        //     }
        // }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        /// <summary>
        /// Gets the corresponding VirtualKeyShort for a given key code.
        /// This function handles both WebDriver special keys (e.g., Keys.Enter, Keys.Tab, Keys.F1, etc.)
        /// and normal character keys.
        /// </summary>
        public static VirtualKeyShort GetVirtualKey(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Key code cannot be null or empty.", nameof(code));
            }
            // First, normalize the code so that names like "Control" become "\uE009"
            string normalized = GetNormalizedKeyValue(code);
            if (string.IsNullOrEmpty(normalized))
            {
                throw new ArgumentException("Normalized key code is null or empty.", nameof(code));
            }
            // Ensure that the normalized code represents a single key or a valid surrogate pair.
            if (normalized.Length > 1 && !char.IsSurrogatePair(normalized, 0))
            {
                throw new ArgumentException("Key code must be a single key or key cluster.", nameof(code));
            }
            int codePoint = (normalized.Length == 1) ? normalized[0] : char.ConvertToUtf32(normalized, 0);

            // Handle WebDriver special keys in the Unicode PUA range (U+E000 to U+E03D)
            if (codePoint >= 0xE000 && codePoint <= 0xE03D)
            {
                switch (codePoint)
                {
                    case 0xE000: // Null key: no physical key
                        return (VirtualKeyShort)0;
                    case 0xE001: return VirtualKeyShort.CANCEL;
                    case 0xE002: return VirtualKeyShort.HELP;
                    case 0xE003: return VirtualKeyShort.BACK;
                    case 0xE004: return VirtualKeyShort.TAB;
                    case 0xE005: return VirtualKeyShort.CLEAR;
                    case 0xE006: // Return
                    case 0xE007: // Enter
                        return VirtualKeyShort.RETURN;
                    case 0xE008: // Shift (use left shift)
                        return VirtualKeyShort.SHIFT;
                    case 0xE009: // Control (use left control)
                        return VirtualKeyShort.CONTROL;
                    case 0xE00A: // Alt (use left alt)
                        return VirtualKeyShort.ALT;
                    case 0xE00B: return VirtualKeyShort.PAUSE;
                    case 0xE00C: return VirtualKeyShort.ESCAPE;
                    case 0xE00D: return VirtualKeyShort.SPACE;
                    case 0xE00E: return VirtualKeyShort.PRIOR;
                    case 0xE00F: return VirtualKeyShort.NEXT;
                    case 0xE010: return VirtualKeyShort.END;
                    case 0xE011: return VirtualKeyShort.HOME;
                    case 0xE012: return VirtualKeyShort.LEFT;
                    case 0xE013: return VirtualKeyShort.UP;
                    case 0xE014: return VirtualKeyShort.RIGHT;
                    case 0xE015: return VirtualKeyShort.DOWN;
                    case 0xE016: return VirtualKeyShort.INSERT;
                    case 0xE017: return VirtualKeyShort.DELETE;
                    // Handle function keys F1-F12
                    case 0xE031: return VirtualKeyShort.F1;
                    case 0xE032: return VirtualKeyShort.F2;
                    case 0xE033: return VirtualKeyShort.F3;
                    case 0xE034: return VirtualKeyShort.F4;
                    case 0xE035: return VirtualKeyShort.F5;
                    case 0xE036: return VirtualKeyShort.F6;
                    case 0xE037: return VirtualKeyShort.F7;
                    case 0xE038: return VirtualKeyShort.F8;
                    case 0xE039: return VirtualKeyShort.F9;
                    case 0xE03A: return VirtualKeyShort.F10;
                    case 0xE03B: return VirtualKeyShort.F11;
                    case 0xE03C: return VirtualKeyShort.F12;
                    case 0xE03D: return VirtualKeyShort.LWIN;
                }
            }

            // For normal characters, use Windows API to map to a virtual key.
            if (codePoint > 0xFFFF)
            {
                // Characters outside the BMP (e.g. emoji) are not typeable via a single key.
                return VirtualKeyShort.PACKET;
            }
            char character = (char)codePoint;
            IntPtr layout = GetKeyboardLayout(0);
            short result = VkKeyScanEx(character, layout);
            if (result == -1)
            {
                return VirtualKeyShort.PACKET;
            }
            byte vkCode = (byte)(result & 0xFF);
            return (VirtualKeyShort)vkCode;
        }


        /// <summary>
        /// Checks whether the provided key cluster (string) is directly typeable using the current keyboard layout.
        /// Returns true if every character in the cluster can be produced by a standard keystroke (with any needed modifiers).
        /// </summary>
        public static bool IsTypeable(string cluster)
        {
            if (string.IsNullOrEmpty(cluster))
            {
                return false;
            }

            // Use the keyboard layout for possible API lookups.
            IntPtr layout = GetKeyboardLayout(0);
            // Iterate through each code point (handling surrogate pairs)
            for (int i = 0; i < cluster.Length; i++)
            {
                int codePoint;
                if (i < cluster.Length - 1 && char.IsSurrogatePair(cluster, i))
                {
                    codePoint = char.ConvertToUtf32(cluster, i);
                    i++; // advance past the surrogate pair
                }
                else
                {
                    codePoint = cluster[i];
                }

                // Skip the WebDriver "Null" key.
                if (codePoint == 0xE000)
                {
                    continue;
                }

                // If it is one of the special WebDriver keys, consider it typeable.
                if (codePoint >= 0xE001 && codePoint <= 0xE03D)
                {
                    continue;
                }

                // Characters outside the BMP (such as many emoji) are not produced by a single keystroke.
                if (codePoint > 0xFFFF)
                {
                    return false;
                }

                // For normal characters, check their Unicode category.
                char ch = (char)codePoint;
                var category = char.GetUnicodeCategory(ch);

                // Accept as typeable if the character is any kind of letter, digit, punctuation or symbol.
                if (category == System.Globalization.UnicodeCategory.UppercaseLetter ||
                    category == System.Globalization.UnicodeCategory.LowercaseLetter ||
                    category == System.Globalization.UnicodeCategory.TitlecaseLetter ||
                    category == System.Globalization.UnicodeCategory.DecimalDigitNumber ||
                    category == System.Globalization.UnicodeCategory.CurrencySymbol ||
                    category == System.Globalization.UnicodeCategory.MathSymbol ||
                    category == System.Globalization.UnicodeCategory.OtherPunctuation ||
                    category == System.Globalization.UnicodeCategory.DashPunctuation ||
                    category == System.Globalization.UnicodeCategory.OpenPunctuation ||
                    category == System.Globalization.UnicodeCategory.ClosePunctuation ||
                    category == System.Globalization.UnicodeCategory.InitialQuotePunctuation ||
                    category == System.Globalization.UnicodeCategory.FinalQuotePunctuation ||
                    category == System.Globalization.UnicodeCategory.OtherSymbol)
                {
                    // We consider these categories as typeable.
                    continue;
                }
                else
                {
                    // As a last resort, try mapping with VkKeyScanEx.
                    short vkMapping = VkKeyScanEx(ch, layout);
                    if (vkMapping == -1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Determines if the given key value represents a modifier key (Shift, Control, Alt, or Meta).
        /// </summary>
        public static bool IsModifier(string keyValue)
        {
            if (string.IsNullOrEmpty(keyValue))
                return false;
            string normKey = GetNormalizedKeyValue(keyValue);
            return (normKey == LeftShift || normKey == Control || normKey == Alt || normKey == Meta);
        }
        
        /// <summary>
        /// Determines whether a character requires a shifted key press (e.g., uppercase or symbol requiring Shift)
        /// </summary>
        public static bool IsShiftedChar(char key)
        {
            short scan = VkKeyScanEx(key, GetKeyboardLayout(0));
            if (scan == -1)
                return false;
            byte shiftState = (byte)((scan >> 8) & 0xFF);
            return (shiftState & 0x01) != 0;
        }

        /// <summary>
        /// Gets the virtual key code for a given key value.
        /// This is similar to GetVirtualKey but provided as an alias.
        /// </summary>
        public static VirtualKeyShort GetCode(string keyValue)
        {
            return GetVirtualKey(keyValue);
        }
    }
}
