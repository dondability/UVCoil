using KeyAuth;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace RecoilOverlay
{
    internal class Program
    {
        public static class Settings
        {
            public static bool Enabled = false;
            public static int Vertical = 5;
            public static int Horizontal = 0;
            public static int Jitter = 1;
            public static int Delay = 10;
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        const uint MOUSE_MOVE = 0x0001;
        public static api KeyAuthApp = new api();

        private static bool _altPressed = false;
        private static bool _semiPressed = false;

        public static void ApplyConfig(int v, int h, int j, int d)
        {
            Settings.Vertical = v;
            Settings.Horizontal = h;
            Settings.Jitter = j;
            Settings.Delay = d;
        }

        static void Main(string[] args)
        {
            // Security check
            Encrypt.InitializeSecurity();

            Console.Title = "System";

            try
            {
                KeyAuthApp.init();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Init Error: " + ex.Message);
                Thread.Sleep(5000);
                return;
            }

            while (!api.IsAuthenticated)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("\n  [SYSTEM] LOGIN");
                Console.ResetColor();
                Console.Write("\n  License: ");

                string? key = Console.ReadLine();

                // Fix for the null reference exception
                if (string.IsNullOrEmpty(key))
                {
                    Console.WriteLine("\n  Key cannot be empty.");
                    Thread.Sleep(1000);
                    continue;
                }

                try
                {
                    KeyAuthApp.license(key);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n  Login Error: " + ex.Message);
                    Thread.Sleep(2000);
                    continue;
                }

                if (api.IsAuthenticated) break;
                Thread.Sleep(1000);
            }

            var menu = new Menu();

            Thread logicThread = new Thread(() =>
            {
                Random rng = new Random();
                while (true)
                {
                    if ((GetAsyncKeyState(0xBA) & 0x8000) != 0)
                    {
                        if (!_semiPressed) { Settings.Enabled = !Settings.Enabled; _semiPressed = true; }
                    }
                    else { _semiPressed = false; }

                    if ((GetAsyncKeyState(0x12) & 0x8000) != 0)
                    {
                        if (!_altPressed) { menu.IsVisible = !menu.IsVisible; _altPressed = true; }
                    }
                    else { _altPressed = false; }

                    if (Settings.Enabled && (GetAsyncKeyState(0x01) & 0x8000) != 0)
                    {
                        int x = Settings.Horizontal + (Settings.Jitter > 0 ? rng.Next(-Settings.Jitter, Settings.Jitter + 1) : 0);
                        int y = Settings.Vertical + (Settings.Jitter > 0 ? rng.Next(-Settings.Jitter, Settings.Jitter + 1) : 0);
                        mouse_event(MOUSE_MOVE, x, y, 0, UIntPtr.Zero);
                    }
                    Thread.Sleep(Settings.Delay);
                }
            })
            { IsBackground = true, Priority = ThreadPriority.Highest };

            logicThread.Start();
            menu.Run().Wait();
        }
    }
}