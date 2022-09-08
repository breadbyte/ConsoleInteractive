using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PInvoke;

namespace ConsoleInteractive {
    public static class ConsoleWriter {

        public static void Init() {
            SetWindowsConsoleAnsi();
            Console.Clear();
        }

        private static void SetWindowsConsoleAnsi() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Kernel32.GetConsoleMode(Kernel32.GetStdHandle(Kernel32.StdHandle.STD_OUTPUT_HANDLE), out var cModes);
                Kernel32.SetConsoleMode(Kernel32.GetStdHandle(Kernel32.StdHandle.STD_OUTPUT_HANDLE), cModes | Kernel32.ConsoleBufferModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING);
            }
        }

        public static void Write(string value) {
            InternalWriter.Write(value);
        }

        public static void WriteLine(string value) {
            InternalWriter.WriteLine(value);
        }
    }

    internal static class InternalWriter {
        private static void Write(string value) {
            int linesAdded = 0;
            foreach (string line in value.Split('\n'))
            {
                int lineLen = line.Length;
                foreach (Match colorCode in Regex.Matches(line, @"\u001B\[\d+m"))
                    lineLen -= colorCode.Groups[0].Length;
                linesAdded += (Math.Max(0, lineLen - 1) / InternalContext.CursorLeftPosLimit) + 1;
            }
            
            lock (InternalContext.WriteLock) {
                
                // If the buffer is initialized, then we should get the current cursor position
                // because we potentially are writing over user input.
                //
                // 0 otherwise, since we know that there is no user input,
                // so we can start at the beginning.
                int currentCursorPos = 0;
                if (InternalContext.BufferInitialized) {
                    currentCursorPos = InternalContext.CurrentCursorLeftPos;
                    ConsoleBuffer.ClearVisibleUserInput();
                }
                else
                    // Clears the entire line. Not optimal as it also clears blank spaces,
                    // but ensures that the entire line is cleared.
                    ConsoleBuffer.ClearCurrentLine();
                
                Console.WriteLine(value);

                // Determine if we need to use the previous top position.
                // i.e. vertically constrained.
                if (InternalContext.CurrentCursorTopPos + linesAdded >= InternalContext.CursorTopPosLimit)
                    Interlocked.Exchange(ref InternalContext.CurrentCursorTopPos, InternalContext.CursorTopPosLimit - 1);
                else 
                    Interlocked.Add(ref InternalContext.CurrentCursorTopPos, linesAdded);

                // Only redraw if we have a buffer initialized.
                if (InternalContext.BufferInitialized) {
                    ConsoleBuffer.RedrawInput();

                    // Need to redraw the prefix manually in cases that RedrawInput() doesn't
                    ConsoleBuffer.DrawPrefix();
                }

                Console.SetCursorPosition(currentCursorPos, InternalContext.CurrentCursorTopPos);
                InternalContext.SetLeftCursorPosition(currentCursorPos);
            }
        }

        public static void WriteLine(string value) {
            Write(value);
        }
    }
}