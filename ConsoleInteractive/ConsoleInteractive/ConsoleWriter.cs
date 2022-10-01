using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
    
        public static void WriteLine(string value) {
            InternalWriter.WriteLine(value);
        }

        public static void WriteLineFormatted(string value) {
            InternalWriter.WriteLineFormatted(value);
        }

    }

    internal static class InternalWriter {
        private static void Write(string value) {
            int linesAdded = 0;
            foreach (string line in value.Split('\n')) {
                int lineLen = line.Length;
                foreach (Match colorCode in Regex.Matches(line, @"\u001B\[\d+m").Cast<Match>())
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

        public static void WriteLineFormatted(string value) {
            StringBuilder b = new();
            var matches = InternalContext.FormatRegex.Matches(value);

            if (matches.Count == 0) {
                Write(value);
                return;
            }

            var idx = value.IndexOf('§');
            if (idx > 0) {
                b.Append(value[..idx]);
            }

            bool funkyMode = false;
            
            foreach (Match match in matches.Cast<Match>()) {
                if (match.Groups[1].Value == "§r") {
                    funkyMode = false;
                }

                if (match.Groups[1].Value == "§k") {
                    funkyMode = true;
                }
                
                if (funkyMode) {
                    b.Append(match.Groups[1]);
                    
                    var charArr = new char[match.Groups[2].Length];
                    Array.Fill(charArr, '\u2588'); // Square character in place of §k.
                    b.Append(new string(charArr));
                    continue;
                }
                
                b.Append(match.Groups[1].Value + match.Groups[2].Value);
            }

            b.Append("§r");
            b.Replace("§0", "\u001B[30m")  // Black
             .Replace("§1", "\u001B[34m")  // Dark Blue
             .Replace("§2", "\u001B[32m")  // Dark Green
             .Replace("§3", "\u001B[36m")  // Dark Aqua
             .Replace("§4", "\u001B[31m")  // Dark Red
             .Replace("§5", "\u001B[35m")  // Dark Purple
             .Replace("§6", "\u001B[33m")  // Gold
             .Replace("§7", "\u001B[90m")  // Gray
             .Replace("§8", "\u001B[37m")  // Dark Gray
             .Replace("§9", "\u001B[94m")  // Blue
             .Replace("§a", "\u001B[92m")  // Green
             .Replace("§b", "\u001B[96m")  // Aqua
             .Replace("§c", "\u001B[91m")  // Red
             .Replace("§d", "\u001B[95m")  // Light Purple
             .Replace("§e", "\u001B[93m")  // Yellow
             .Replace("§f", "\u001B[97m")  // White
             .Replace("§k", "")            // Obfuscated
             .Replace("§l", "\u001B[1m")   // Bold
             .Replace("§m", "\u001B[9m")   // Strikethrough
             .Replace("§n", "\u001B[4m")   // Underline
             .Replace("§o", "\u001B[3m")   // Italic
             .Replace("§r", "\u001B[0m")   // Reset
             .Replace("§w", "\u001B[41m")  // Custom: Background Red
             .Replace("§x", "\u001B[43m")  // Custom: Background Yellow
             .Replace("§y", "\u001B[42m")  // Custom: Background Green
             .Replace("§z", "\u001B[100m");// Custom: Background Gray

            Write(b.ToString());
        }
    }
}