using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TouchEveryWhereBeta
{
    class InputEventPublisher
    {
        private INPUT input;

        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const int INPUT_HARDWARE = 2;
        private const String PptProcName = "PPTVIEW";

        #region イベント入力用

        [DllImport("user32.dll")]
        static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")]
        static extern int SendInput(int nInputs, ref INPUT pInputs, int cbSize);
        const int KEYEVENTF_KEYUP = 0x2;
        const int VK_LWIN = 0x5b;           // windowskey
        const int VK_OEM_PLUS = 0xbb;       // ;
        const int VK_OEM_MINUS = 0xbd;      // -
        const int VK_LEFT = 0x25;
        const int VK_UP = 0x26;
        const int VK_RIGHT = 0x27;
        const int VK_DOWN = 0x28;
        const int VK_SHIFT = 0x10;          // Shift
        const int VK_TAB = 0x09;            // Tab
        const int VK_MENU = 0x12;           // Alt
        const int VK_F5 = 0x74;             // F5
        const int VK_VOLUME_UP = 0xaf;
        const int VK_VOLUME_DOWN = 0xae;

        // dwFlags of MouseInput
        const int MOUSEEVENTF_MOVED = 0x0001;
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;  // 左ボタン Down
        const int MOUSEEVENTF_LEFTUP = 0x0004;  // 左ボタン Up
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;  // 右ボタン Down
        const int MOUSEEVENTF_RIGHTUP = 0x0010;  // 右ボタン Up
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;  // 中ボタン Down
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;  // 中ボタン Up
        const int MOUSEEVENTF_WHEEL = 0x0080;
        const int MOUSEEVENTF_XDOWN = 0x0100;
        const int MOUSEEVENTF_XUP = 0x0200;
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)]
            public uint type;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
            [FieldOffset(4)]
            public KEYBDINPUT ki;
            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        }

        #endregion

        #region ウィンドウ操作用

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;
        const int SW_SHOWNORMAL = 1;
        const int SW_NORMAL = 1;
        const int SW_SHOWMINIMIZED = 2;
        const int SW_SHOWMAXIMIZED = 3;
        const int SW_MAXIMIZE = 3;
        const int SW_SHOWNOACTIVATE = 4;
        const int SW_SHOW = 5;
        const int SW_MINIMIZE = 6;
        const int SW_SHOWMINNOACTIVE = 7;
        const int SW_SHOWNA = 8;
        const int SW_RESTORE = 9;
        const int SW_SHOWDEFAULT = 10;
        const int SW_FORCEMINIMIZE = 11;
        const int SW_MAX = 11;

        #endregion

        public InputEventPublisher() {

        }


        public void MouseMove(int move_x, int move_y) {
            input.type = INPUT_MOUSE;
            input.mi.time = 0;
            input.mi.mouseData = 0;
            input.mi.dwFlags = MOUSEEVENTF_MOVED;
            input.mi.dwExtraInfo = GetMessageExtraInfo();

            input.mi.dx = move_x;
            input.mi.dy = move_y;
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));

            System.Threading.Thread.Sleep(100);
        }


        // 左矢印キー
        public void SendLeftKey() {
            input.type = INPUT_KEYBOARD;
            input.ki.wScan = 0;
            input.ki.time = 0;
            input.ki.dwExtraInfo = GetMessageExtraInfo();
            input.ki.wVk = (ushort)VK_LEFT;
            input.ki.dwFlags = 0;      //Key Down
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));

            input.ki.wVk = (ushort)VK_LEFT;
            input.ki.dwFlags = KEYEVENTF_KEYUP;  //Key Up
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
        }

        // 上矢印キー
        public void SendUpKey()
        {
            input.type = INPUT_KEYBOARD;
            input.ki.wScan = 0;
            input.ki.time = 0;
            input.ki.dwExtraInfo = GetMessageExtraInfo();
            input.ki.wVk = (ushort)VK_UP;
            input.ki.dwFlags = 0;      //Key Down
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));

            input.ki.wVk = (ushort)VK_UP;
            input.ki.dwFlags = KEYEVENTF_KEYUP;  //Key Up
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
        }

        // 右矢印キー
        public void SendRightKey() {
            input.type = INPUT_KEYBOARD;
            input.ki.wScan = 0;
            input.ki.time = 0;
            input.ki.dwExtraInfo = GetMessageExtraInfo();
            input.ki.wVk = (ushort)VK_RIGHT;
            input.ki.dwFlags = 0;      //Key Down
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));

            input.ki.wVk = (ushort)VK_RIGHT;
            input.ki.dwFlags = KEYEVENTF_KEYUP;  //Key Up
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
        }

        // 下矢印キー
        public void SendDownKey()
        {
            input.type = INPUT_KEYBOARD;
            input.ki.wScan = 0;
            input.ki.time = 0;
            input.ki.dwExtraInfo = GetMessageExtraInfo();
            input.ki.wVk = (ushort)VK_DOWN;
            input.ki.dwFlags = 0;      //Key Down
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));

            input.ki.wVk = (ushort)VK_DOWN;
            input.ki.dwFlags = KEYEVENTF_KEYUP;  //Key Up
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
            SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
