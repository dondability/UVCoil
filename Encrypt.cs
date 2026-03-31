using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace RecoilOverlay
{
    public static class Encrypt
    {
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetInformationThread(IntPtr threadHandle, int threadInformationClass, IntPtr threadInformation, int threadInformationLength);

        // 0x11 is the constant for ThreadHideFromDebugger
        private const int ThreadHideFromDebugger = 0x11;

        /// <summary>
        /// Runs all security checks. Call this at the start of Program.cs.
        /// </summary>
        public static void InitializeSecurity()
        {
            if (CheckDebuggers() || ScanForDebuggers())
            {
                // If a debugger is found, exit immediately without error messages
                Environment.Exit(0);
            }

            // Hide the main thread from any debugger that tries to attach later
            HideThread(Process.GetCurrentProcess().Handle);
        }

        private static bool CheckDebuggers()
        {
            bool isDebuggerPresent = false;

            // Basic check
            if (IsDebuggerPresent()) return true;

            // Remote debugger check (detects x64dbg/x32dbg)
            CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent);

            return isDebuggerPresent;
        }

        private static void HideThread(IntPtr hProcess)
        {
            try
            {
                // This call makes the thread invisible to debuggers.
                // If x64dbg is attached and hits this, it will often crash the debugging session.
                NtSetInformationThread(Process.GetCurrentProcess().Handle, ThreadHideFromDebugger, IntPtr.Zero, 0);
            }
            catch { }
        }

        private static bool ScanForDebuggers()
        {
            // List of common debugging and reversing tools
            string[] forbiddenProcesses = {
                "x64dbg",
                "x32dbg",
                "ollydbg",
                "ida64",
                "idag",
                "ghidra",
                "wireshark",
                "cheatengine",
                "dnspy",
                "de4dot"
            };

            var runningProcesses = Process.GetProcesses();
            foreach (var process in runningProcesses)
            {
                if (forbiddenProcesses.Any(p => process.ProcessName.ToLower().Contains(p)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}