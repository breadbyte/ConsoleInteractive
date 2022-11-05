using System;
using System.Runtime.InteropServices;
using ConsoleInteractive.WriterImpl;
using PInvoke;

namespace ConsoleInteractive {
    public static class ConsoleWriter {
        private static WriterBase? WriterImpl; 

        public static void Init() {
            SetWindowsConsoleAnsi();
            CheckConsoleCapability();
            Console.Clear();
        }

        private static void SetWindowsConsoleAnsi() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                if (OperatingSystem.IsWindowsVersionAtLeast(10)) {
                    Kernel32.GetConsoleMode(Kernel32.GetStdHandle(Kernel32.StdHandle.STD_OUTPUT_HANDLE), out var cModes);
                    Kernel32.SetConsoleMode(Kernel32.GetStdHandle(Kernel32.StdHandle.STD_OUTPUT_HANDLE), cModes | Kernel32.ConsoleBufferModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING);
                } else {
                    // If we're not on Win10+, default to the Windows API Coloring system.
                    InternalContext.ConsoleColorMode = InternalContext.ColorMode.WindowsAPI;
                    WriterImpl = new WindowsWriter();
                    return;
                }
            }

            // Use VT Code Coloring by default.
            InternalContext.ConsoleColorMode = InternalContext.ColorMode.VTCode;
            WriterImpl = new CrossPlatformWriter();
        }

        public static void ForceUseWindowsAPI() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                throw new InvalidOperationException("Windows API cannot be used on non-Windows platforms.");
            }

            InternalContext.ConsoleColorMode = InternalContext.ColorMode.WindowsAPI;
            WriterImpl = new WindowsWriter();
        }

        private static void CheckConsoleCapability() {

            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != null) {
                InternalContext.ConsoleColorMode = InternalContext.ColorMode.None;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                if (Environment.GetEnvironmentVariable("TERM") != null) {
                    // TODO: Check our color range?
                }
            }
        }

        public static void WriteLine(FormattedStringBuilder formattedString) {
            WriterImpl?.Write(formattedString);
        }

        public static void WriteLine(string value) {
            WriterImpl?.Write(value);
        }
    }
}