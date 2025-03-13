using System;
using System.Runtime.InteropServices;

namespace FlaUI.WebDriver.Services
{
    public interface IBatchInputDispatcher
    {
        void SendKeysBatch(ushort[] keyCodes);
    }

    public class BatchInputDispatcher : IBatchInputDispatcher
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;

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

        public void SendKeysBatch(ushort[] keyCodes)
        {
            var inputCount = keyCodes.Length * 2;
            INPUT[] inputs = new INPUT[inputCount];
            int index = 0;
            foreach (var key in keyCodes)
            {
                inputs[index].type = INPUT_KEYBOARD;
                inputs[index].u.ki.wVk = key;
                inputs[index].u.ki.dwFlags = 0;
                inputs[index].u.ki.wScan = 0;
                inputs[index].u.ki.time = 0;
                inputs[index].u.ki.dwExtraInfo = IntPtr.Zero;
                index++;

                inputs[index].type = INPUT_KEYBOARD;
                inputs[index].u.ki.wVk = key;
                inputs[index].u.ki.dwFlags = KEYEVENTF_KEYUP;
                inputs[index].u.ki.wScan = 0;
                inputs[index].u.ki.time = 0;
                inputs[index].u.ki.dwExtraInfo = IntPtr.Zero;
                index++;
            }

            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent != inputs.Length)
            {
                throw new Exception("SendInput failed: " + Marshal.GetLastWin32Error());
            }
        }
    }
}