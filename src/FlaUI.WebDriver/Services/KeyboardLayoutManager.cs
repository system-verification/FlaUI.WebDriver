using System;
using System.Runtime.InteropServices;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace FlaUI.WebDriver.Services
{
    public class KeyboardLayoutManager
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

        private const uint KLF_NONE = 0x0000;

        // Public (static) property holding the current active layout.
        public static IntPtr CurrentKeyboardLayout { get; private set; }

        private readonly ILogger<KeyboardLayoutManager> _logger;

        public KeyboardLayoutManager(ILogger<KeyboardLayoutManager> logger)
        {
            _logger = logger;
            // Get and store the active layout.
            CurrentKeyboardLayout = GetKeyboardLayout(0);
            uint currentLayoutId = (uint)CurrentKeyboardLayout & 0xFFFF;
            _logger.LogInformation("Detected environment keyboard layout: 0x{Layout:X4}", currentLayoutId);
            // Re-activate that layout for this process
            ActivateKeyboardLayout(CurrentKeyboardLayout, KLF_NONE);
        }
    }
}