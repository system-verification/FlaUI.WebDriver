using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
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

        // Cache for computed virtual key codes.
        private static ConcurrentDictionary<string, VirtualKeyShort> _keyCache = new ConcurrentDictionary<string, VirtualKeyShort>();

        public static VirtualKeyShort GetVirtualKey(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Key code cannot be null or empty.", nameof(code));
            }

            string normalized = GetNormalizedKeyValue(code);
            if (string.IsNullOrEmpty(normalized))
            {
                throw new ArgumentException("Normalized key code is null or empty.", nameof(code));
            }

            if (_keyCache.TryGetValue(normalized, out VirtualKeyShort cachedValue))
            {
                return cachedValue;
            }

            VirtualKeyShort vk;
            int codePoint = (normalized.Length == 1) ? normalized[0] : char.ConvertToUtf32(normalized, 0);
            if (codePoint >= 0xE000 && codePoint <= 0xE03D)
            {
                switch (codePoint)
                {
                    case 0xE000:
                        vk = (VirtualKeyShort)0;
                        break;
                    case 0xE001: vk = VirtualKeyShort.CANCEL; break;
                    case 0xE002: vk = VirtualKeyShort.HELP; break;
                    case 0xE003: vk = VirtualKeyShort.BACK; break;
                    case 0xE004: vk = VirtualKeyShort.TAB; break;
                    case 0xE005: vk = VirtualKeyShort.CLEAR; break;
                    case 0xE006:
                    case 0xE007: vk = VirtualKeyShort.RETURN; break;
                    case 0xE008: vk = VirtualKeyShort.SHIFT; break;
                    case 0xE009: vk = VirtualKeyShort.CONTROL; break;
                    case 0xE00A: vk = VirtualKeyShort.ALT; break;
                    case 0xE00B: vk = VirtualKeyShort.PAUSE; break;
                    case 0xE00C: vk = VirtualKeyShort.ESCAPE; break;
                    case 0xE00D: vk = VirtualKeyShort.SPACE; break;
                    case 0xE00E: vk = VirtualKeyShort.PRIOR; break;
                    case 0xE00F: vk = VirtualKeyShort.NEXT; break;
                    case 0xE010: vk = VirtualKeyShort.END; break;
                    case 0xE011: vk = VirtualKeyShort.HOME; break;
                    case 0xE012: vk = VirtualKeyShort.LEFT; break;
                    case 0xE013: vk = VirtualKeyShort.UP; break;
                    case 0xE014: vk = VirtualKeyShort.RIGHT; break;
                    case 0xE015: vk = VirtualKeyShort.DOWN; break;
                    case 0xE016: vk = VirtualKeyShort.INSERT; break;
                    case 0xE017: vk = VirtualKeyShort.DELETE; break;
                    case 0xE031: vk = VirtualKeyShort.F1; break;
                    case 0xE032: vk = VirtualKeyShort.F2; break;
                    case 0xE033: vk = VirtualKeyShort.F3; break;
                    case 0xE034: vk = VirtualKeyShort.F4; break;
                    case 0xE035: vk = VirtualKeyShort.F5; break;
                    case 0xE036: vk = VirtualKeyShort.F6; break;
                    case 0xE037: vk = VirtualKeyShort.F7; break;
                    case 0xE038: vk = VirtualKeyShort.F8; break;
                    case 0xE039: vk = VirtualKeyShort.F9; break;
                    case 0xE03A: vk = VirtualKeyShort.F10; break;
                    case 0xE03B: vk = VirtualKeyShort.F11; break;
                    case 0xE03C: vk = VirtualKeyShort.F12; break;
                    case 0xE03D: vk = VirtualKeyShort.LWIN; break;
                    default:
                        vk = VirtualKeyShort.PACKET;
                        break;
                }
            }
            else
            {
                if (codePoint > 0xFFFF)
                {
                    vk = VirtualKeyShort.PACKET;
                }
                else
                {
                    char character = (char)codePoint;
                    IntPtr layout = GetKeyboardLayout(0);
                    short result = VkKeyScanEx(character, layout);
                    if (result == -1)
                    {
                        vk = VirtualKeyShort.PACKET;
                    }
                    else
                    {
                        byte vkCode = (byte)(result & 0xFF);
                        vk = (VirtualKeyShort)vkCode;
                    }
                }
            }

            _keyCache[normalized] = vk;
            return vk;
        }

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
                case "null": return Null;
                case "shift":
                case "leftshift": return LeftShift;
                case "control":
                case "ctrl":
                case "leftcontrol": return Control;
                case "alt":
                case "leftalt": return Alt;
                case "meta":
                case "command":
                case "win":
                case "windows": return Meta;
                case "enter":
                case "return": return Enter;
                case "backspace":
                case "back": return Backspace;
                case "tab": return Tab;
                case "clear": return Clear;
                case "pause": return Pause;
                case "escape":
                case "esc": return Escape;
                case "space":
                case "spacebar": return Space;
                case "pageup": return PageUp;
                case "pagedown": return PageDown;
                case "end": return End;
                case "home": return Home;
                case "leftarrow":
                case "left": return LeftArrow;
                case "uparrow":
                case "up": return UpArrow;
                case "rightarrow":
                case "right": return RightArrow;
                case "downarrow":
                case "down": return DownArrow;
                case "insert": return Insert;
                case "delete": return Delete;
                case "help": return Help;
                case "cancel": return Cancel;
                default:
                    if (lowerName.StartsWith("key_") && lowerName.Length == 5)
                    {
                        var remainder = keyValue.Substring(4);
                        if (char.IsLetterOrDigit(remainder[0]))
                        {
                            return remainder;
                        }
                    }
                    return keyValue;
            }
        }

        /// <summary>
        /// Returns the full result from VkKeyScanEx for the given character.
        /// The result includes modifier information (in the high-order byte) indicating if Shift, Control, or Alt are needed.
        /// </summary>
        public static short GetVkScanResult(char ch)
        {
            return VkKeyScanEx(ch, GetKeyboardLayout(0));
        }
        
        /// <summary>
        /// A public helper that wraps VkKeyScanEx.
        /// Use this to get the full scan result (including modifier information) for the given character.
        /// </summary>
        public static short GetVkScanEx(char ch, IntPtr layout)
        {
            return VkKeyScanEx(ch, layout);
        }

        // --- Unicode Injection Helper (fallback) ---
        /// <summary>
        /// Dispatches a Unicode keystroke via SendInput (down + up) using the KEYEVENTF_UNICODE flag.
        /// Use this only when a normal physical key mapping is unavailable (i.e. GetVirtualKey returns PACKET).
        /// </summary>
        public static void DispatchUnicodeKeystroke(char unicodeChar)
        {
            INPUT[] inputs = new INPUT[2];

            // Key down event using Unicode injection.
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0, // Ignored when KEYEVENTF_UNICODE is used.
                        wScan = unicodeChar,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up event.
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = unicodeChar,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent != inputs.Length)
            {
                int error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"SendInput error: {error}");
            }
            System.Threading.Thread.Sleep(50);
        }

        /// <summary>
        /// Checks whether the provided key cluster (string) is directly typeable under the current keyboard layout.
        /// Returns true if every character in the cluster can be produced by a standard keystroke.
        /// </summary>
        public static bool IsTypeable(string cluster)
        {
            if (string.IsNullOrEmpty(cluster))
            {
                return false;
            }

            IntPtr layout = GetKeyboardLayout(0);
            for (int i = 0; i < cluster.Length; i++)
            {
                int codePoint;
                if (i < cluster.Length - 1 && char.IsSurrogatePair(cluster, i))
                {
                    codePoint = char.ConvertToUtf32(cluster, i);
                    i++;
                }
                else
                {
                    codePoint = cluster[i];
                }

                if (codePoint == 0xE000)
                    continue;
                if (codePoint >= 0xE001 && codePoint <= 0xE03D)
                    continue;
                if (codePoint > 0xFFFF)
                    return false;

                char ch = (char)codePoint;
                var category = char.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.UppercaseLetter ||
                    category == UnicodeCategory.LowercaseLetter ||
                    category == UnicodeCategory.TitlecaseLetter ||
                    category == UnicodeCategory.DecimalDigitNumber ||
                    category == UnicodeCategory.CurrencySymbol ||
                    category == UnicodeCategory.MathSymbol ||
                    category == UnicodeCategory.OtherPunctuation ||
                    category == UnicodeCategory.DashPunctuation ||
                    category == UnicodeCategory.OpenPunctuation ||
                    category == UnicodeCategory.ClosePunctuation ||
                    category == UnicodeCategory.InitialQuotePunctuation ||
                    category == UnicodeCategory.FinalQuotePunctuation ||
                    category == UnicodeCategory.OtherSymbol)
                {
                    continue;
                }
                else
                {
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
        /// Determines whether a character requires Shift (e.g. uppercase letters or symbols).
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
        /// Gets the virtual key code for a given key value (alias to GetVirtualKey).
        /// </summary>
        public static VirtualKeyShort GetCode(string keyValue)
        {
            return GetVirtualKey(keyValue);
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetKeyboardLayout(uint idThread);
    }
}
