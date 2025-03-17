using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Microsoft.Win32;

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

    public class KeyboardLayoutInfo
    {
        public string LayoutId { get; set; }
        public string LayoutName { get; set; }
    }

    public static class KeyboardLayoutHelper
    {
        private const uint KLF_ACTIVATE = 0x00000001;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

        [DllImport("user32.dll")]
        private static extern int GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        // Add the SHLoadIndirectString API from shlwapi.dll.
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

        /// <summary>
        /// Loads and activates the keyboard layout specified by the identifier (e.g., "00000409" for US English).
        /// </summary>
        public static void SwitchToKeyboardLayout(string layoutId)
        {
            // Load the given keyboard layout.
            IntPtr hkl = LoadKeyboardLayout(layoutId, KLF_ACTIVATE);
            if (hkl == IntPtr.Zero)
            {
                throw new Exception($"Failed to load keyboard layout: {layoutId}. Error: {Marshal.GetLastWin32Error()}");
            }

            // Activate the loaded keyboard layout.
            IntPtr result = ActivateKeyboardLayout(hkl, KLF_ACTIVATE);
            if (result == IntPtr.Zero)
            {
                throw new Exception($"Failed to activate keyboard layout: {layoutId}. Error: {Marshal.GetLastWin32Error()}");
            }
        }

        /// <summary>
        /// Returns a list of currently loaded keyboard layout IDs in hexadecimal format.
        /// </summary>
        public static IEnumerable<(string,string)> GetLoadedKeyboardLayouts()
        {
            int count = GetKeyboardLayoutList(0, null);
            IntPtr[] ids = new IntPtr[count];
            GetKeyboardLayoutList(count, ids);

            var layouts = new List<(string,string)>();
            foreach (var id in ids)
            {
                var layoutId = id.ToString("X8");
                var layoutName = GetKeyboardLayoutName(layoutId);
                layouts.Add((layoutId, layoutName));
            }
            return layouts;
        }

        /// <summary>
        /// Returns the current active keyboard layout Id string.
        /// </summary>
        public static string GetCurrentKeyboardLayout()
        {
            // Get active layout for the current thread (thread id 0)
            IntPtr hkl = GetKeyboardLayout(0);
            return hkl.ToString("X8");
        }

        /// <summary>
        /// Returns the name of the keyboard based on the layout ID from the system registry.
        /// Looks for the "Layout Display Name" (Windows 11 or indirect on Windows 10) first, 
        /// then falls back to "Layout Text".
        /// </summary>
        public static string GetKeyboardLayoutName(string layoutId)
        {
            if (!int.TryParse(layoutId, System.Globalization.NumberStyles.HexNumber, null, out int hkl))
            {
                return $"Unknown layout ({layoutId})";
            }

            // Extract both the low and high order words.
            int lowWord = hkl & 0xFFFF;
            int highWord = (hkl >> 16) & 0xFFFF;

            // If the high-order word differs, assume it holds the layout identifier.
            int keyId = (highWord != 0 && highWord != lowWord) ? highWord : lowWord;
            string registryId = keyId.ToString("X4").PadLeft(8, '0');

            using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\{registryId}"))
            {
                if (key == null)
                {
                    return $"Unknown layout ({layoutId})";
                }

                // Try to get the "Layout Display Name" first.
                var layoutDisplayName = key.GetValue("Layout Display Name") as string;
                if (!string.IsNullOrEmpty(layoutDisplayName))
                {
                    // If it's an indirect string (starts with "@"), resolve it.
                    if (layoutDisplayName.StartsWith("@"))
                    {
                        layoutDisplayName = ResolveIndirectString(layoutDisplayName);
                    }
                    return layoutDisplayName;
                }

                // Fall back to the classic "Layout Text"
                var layoutText = key.GetValue("Layout Text") as string;
                return !string.IsNullOrEmpty(layoutText) ? layoutText : $"Unknown layout ({layoutId})";
            }
        }

        /// <summary>
        /// Resolves an indirect string from the registry using SHLoadIndirectString.
        /// </summary>
        private static string ResolveIndirectString(string indirectString)
        {
            StringBuilder sb = new StringBuilder(512);
            int hr = SHLoadIndirectString(indirectString, sb, sb.Capacity, IntPtr.Zero);
            return hr == 0 ? sb.ToString() : indirectString;
        }
    }
}