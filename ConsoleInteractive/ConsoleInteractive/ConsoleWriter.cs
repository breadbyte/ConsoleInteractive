using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ConsoleInteractive.WriterImpl;
using PInvoke;
using static ConsoleInteractive.FormattedStringBuilder;

namespace ConsoleInteractive {
    public static class ConsoleWriter {
        private static WriterBase? WriterImpl = null; 

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
            WriterImpl!.Write(formattedString);
        }

        public static void WriteLine(string value) {
            WriterImpl!.Write(value);
        }
    }

    internal static class InternalWriter {
        /*
         * The InternalWriter is the class where the actual writing of the string to the console happens.
         * This class is responsible for reporting the current position on the console.
         *
         * The InternalWriter currently requires going through a Write Chain, in which
         * 1. All newlines are split beforehand and processed individually
         * 2. If the string contains color and formatting information, it is passed in as a StringData. Otherwise, it is passed as a regular string. 
         */


        // Start of the Write Chain.
        // TODO FIXME: Does not detect escape codes in the value
        internal static void BeginWriteChain(string value) {
            // Split each newline into it's own individual string.
            // var splitted = SplitNewLine(value);
            // FIXME
            // Start writing each individual splitted line.
            //foreach (var split in splitted) {
            //   WriteInternal(split);
            //}
        }
        
    }
}