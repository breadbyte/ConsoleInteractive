using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PInvoke;
using Wcwidth;

namespace ConsoleInteractive {
    public static class ConsoleWriter {
        public static bool EnableColor { get; set; } = true;
        public static bool UseVT100ColorCode { get; set; } = false;
        private static bool InDocker { get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; } }
        private static IWriter BackendWriter = new InternalWriter();

        public static void Init() {
            SetWindowsConsoleAnsi();
            if (!Console.IsOutputRedirected && InternalContext.IsInteractiveConsole) {
                try { Console.Clear(); } catch { }
            }
            if (InDocker || !InternalContext.IsInteractiveConsole)
                BackendWriter = new FallbackWriter();
        }

        private static void SetWindowsConsoleAnsi() {
            if (OperatingSystem.IsWindows()) {
                Kernel32.GetConsoleMode(Kernel32.GetStdHandle(Kernel32.StdHandle.STD_OUTPUT_HANDLE), out var cModes);
                Kernel32.SetConsoleMode(Kernel32.GetStdHandle(Kernel32.StdHandle.STD_OUTPUT_HANDLE), cModes | Kernel32.ConsoleBufferModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING);
            }
        }

        public static void WriteLine(string value) {
            BackendWriter.WriteLine(value);
        }

        public static void WriteLineFormatted(string value) {
            BackendWriter.WriteLineFormatted(value);
        }
    }

    internal interface IWriter {
        public abstract void WriteLine(string value);

        public abstract void WriteLineFormatted(string value);
    }

    internal class FallbackWriter : IWriter {
        public void WriteLine(string value) {
            Console.WriteLine(value);
        }

        public void WriteLineFormatted(string value) {
            Console.WriteLine(value);
        }
    }

    internal class InternalWriter : IWriter {

        /// <summary>
        /// Gets the number of lines and the width of the first line of the message.
        /// </summary>
        private Tuple<int, int> GetLineCountInTerminal(string value) {
            if (Console.IsOutputRedirected || !InternalContext.IsInteractiveConsole)
                return new(0, 0);

            bool escape = false;
            int lineCnt = 0, cursorPos = 0, firstLineLength = -1;
            int bufWidth = InternalContext.SafeBufferWidth;
            foreach (char c in value) {
                if (!escape && c == '\u001B') {
                    escape = true;
                    continue;
                } else if (escape && c == 'm') {
                    escape = false;
                    continue;
                } else if (escape) {
                    continue;
                }

                if (c == '\n') {
                    if (lineCnt == 0)
                        firstLineLength = cursorPos;
                    ++lineCnt;
                    cursorPos = 0;
                }

                int width = Math.Max(0, UnicodeCalculator.GetWidth(c));
                if (cursorPos + width > bufWidth) {
                    if (lineCnt == 0)
                        firstLineLength = cursorPos;
                    ++lineCnt;
                    cursorPos = width;
                } else {
                    cursorPos += width;
                }

                if (cursorPos >= bufWidth) {
                    if (lineCnt == 0)
                        firstLineLength = cursorPos;
                    ++lineCnt;
                    cursorPos = 0;
                }
            }
            if (firstLineLength == -1)
                firstLineLength = cursorPos;
            if (lineCnt == 0 || cursorPos > 0)
                ++lineCnt;
            return new(lineCnt, firstLineLength);
        }

        internal static void WriteConsole(string value, List<Tuple<int, bool, ConsoleColor>>? colors = null) {
            if (colors != null) {
                int curIndex = 0;
                foreach ((int idx, bool foreground, ConsoleColor color) in colors) {
                    Console.Write(value[curIndex..idx]);
                    curIndex = idx;
                    if (foreground)
                        Console.ForegroundColor = color;
                    else
                        Console.BackgroundColor = color;
                }
                Console.WriteLine(value[curIndex..]);
            } else {
                Console.WriteLine(value);
            }
        }

        private void Write(string value, List<Tuple<int, bool, ConsoleColor>>? colors = null) {
            (int linesAdded, int firstLineLength) = GetLineCountInTerminal(value);

            lock (InternalContext.WriteLock) {
                ConsoleSuggestion.BeforeWrite(value, linesAdded);

                if (!Console.IsOutputRedirected && InternalContext.IsInteractiveConsole) {
                    try {
                        if (InternalContext.BufferInitialized)
                            ConsoleBuffer.ClearVisibleUserInput(startPos: firstLineLength);
                        else
                            Console.CursorLeft = 0;
                    } catch { }
                }

                WriteConsole(value, colors);

                if (!Console.IsOutputRedirected && InternalContext.IsInteractiveConsole) {
                    if (InternalContext.BufferInitialized)
                        ConsoleBuffer.RedrawInputArea(RedrawAll: true);
                }

                ConsoleSuggestion.AfterWrite();
            }
        }

        public void WriteLine(string value) {
            Write(value);
        }

        public void WriteLineFormatted(string value) {
            if (!ConsoleWriter.EnableColor) {
                Write(InternalContext.FormatRegex.Replace(value, string.Empty));
                return;
            }

            var matches = InternalContext.FormatRegex.Matches(value);
            if (matches.Count == 0) {
                Write(value);
                return;
            }

            int curIndex = 0;
            bool funkyMode = false;
            StringBuilder sb = new(value.Length);
            if (ConsoleWriter.UseVT100ColorCode) {
                foreach (Match match in matches) {
                    if (funkyMode)
                        sb.Append('\u2588', match.Index - curIndex);
                    else
                        sb.Append(value.AsSpan(curIndex, match.Index - curIndex));
                    curIndex = match.Index + match.Length;

                    string matchValue = match.Groups[1].Value;
                    if (matchValue[1] == '§') { // background
                        if (matchValue.Length > 3 && matchValue[2] == '#') {
                            var (r, g, b) = ParseHexRgb(matchValue.AsSpan(3));
                            sb.Append($"\u001B[48;2;{r};{g};{b}m");
                        } else switch (matchValue[2]) {
                            case '0': sb.Append("\u001B[40m"); break;
                            case '1': sb.Append("\u001B[44m"); break;
                            case '2': sb.Append("\u001B[42m"); break;
                            case '3': sb.Append("\u001B[46m"); break;
                            case '4': sb.Append("\u001B[41m"); break;
                            case '5': sb.Append("\u001B[45m"); break;
                            case '6': sb.Append("\u001B[43m"); break;
                            case '7': sb.Append("\u001B[100m"); break;
                            case '8': sb.Append("\u001B[47m"); break;
                            case '9': sb.Append("\u001B[104m"); break;
                            case 'a': sb.Append("\u001B[102m"); break;
                            case 'b': sb.Append("\u001B[106m"); break;
                            case 'c': sb.Append("\u001B[101m"); break;
                            case 'd': sb.Append("\u001B[105m"); break;
                            case 'e': sb.Append("\u001B[103m"); break;
                            case 'f': sb.Append("\u001B[107m"); break;
                            case 'r': funkyMode = false; sb.Append("\u001B[0m"); break;
                        }
                    } else if (matchValue[1] == '#') { // foreground hex
                        var (r, g, b) = ParseHexRgb(matchValue.AsSpan(2));
                        sb.Append($"\u001B[38;2;{r};{g};{b}m");
                    } else { // foreground standard
                        switch (matchValue[1]) {
                            case '0': sb.Append("\u001B[30m"); break;
                            case '1': sb.Append("\u001B[34m"); break;
                            case '2': sb.Append("\u001B[32m"); break;
                            case '3': sb.Append("\u001B[36m"); break;
                            case '4': sb.Append("\u001B[31m"); break;
                            case '5': sb.Append("\u001B[35m"); break;
                            case '6': sb.Append("\u001B[33m"); break;
                            case '7': sb.Append("\u001B[90m"); break;
                            case '8': sb.Append("\u001B[37m"); break;
                            case '9': sb.Append("\u001B[94m"); break;
                            case 'a': sb.Append("\u001B[92m"); break;
                            case 'b': sb.Append("\u001B[96m"); break;
                            case 'c': sb.Append("\u001B[91m"); break;
                            case 'd': sb.Append("\u001B[95m"); break;
                            case 'e': sb.Append("\u001B[93m"); break;
                            case 'f': sb.Append("\u001B[97m"); break;
                            case 'k': funkyMode = true; break;
                            case 'l': sb.Append("\u001B[1m"); break;
                            case 'm': sb.Append("\u001B[9m"); break;
                            case 'n': sb.Append("\u001B[4m"); break;
                            case 'o': sb.Append("\u001B[3m"); break;
                            case 'r': funkyMode = false; sb.Append("\u001B[0m"); break;
                        }
                    }
                }
                sb.Append(value.AsSpan(curIndex, value.Length - curIndex));
                if (matches[^1].Value[^1] != 'r')
                    sb.Append("\u001b[0m");
                Write(sb.ToString());
            } else {
                List<Tuple<int, bool, ConsoleColor>> colors = new();
                foreach (Match match in matches) {
                    if (funkyMode)
                        sb.Append('\u2588', match.Index - curIndex);
                    else
                        sb.Append(value.AsSpan(curIndex, match.Index - curIndex));
                    curIndex = match.Index + match.Length;

                    string matchValue = match.Groups[1].Value;

                    bool isForeground = matchValue[1] != '§';
                    char colorChar = isForeground ? matchValue[1] : matchValue[2];
                    if (colorChar == '#') {
                        int hexStart = isForeground ? 2 : 3;
                        var (r, g, b) = ParseHexRgb(matchValue.AsSpan(hexStart));
                        colors.Add(new(sb.Length, isForeground, NearestConsoleColor(r, g, b)));
                    } else switch (colorChar) {
                        case '0': colors.Add(new(sb.Length, isForeground, ConsoleColor.Gray)); break;
                        case '1': colors.Add(new(sb.Length, isForeground, ConsoleColor.DarkBlue)); break;
                        case '2': colors.Add(new(sb.Length, isForeground, ConsoleColor.DarkGreen)); break;
                        case '3': colors.Add(new(sb.Length, isForeground, ConsoleColor.DarkCyan)); break;
                        case '4': colors.Add(new(sb.Length, isForeground, ConsoleColor.DarkRed)); break;
                        case '5': colors.Add(new(sb.Length, isForeground, ConsoleColor.DarkMagenta)); break;
                        case '6': colors.Add(new(sb.Length, isForeground, ConsoleColor.DarkYellow)); break;
                        case '7': colors.Add(new(sb.Length, isForeground, ConsoleColor.Gray)); break;
                        case '8': colors.Add(new(sb.Length, isForeground, ConsoleColor.DarkGray)); break;
                        case '9': colors.Add(new(sb.Length, isForeground, ConsoleColor.Blue)); break;
                        case 'a': colors.Add(new(sb.Length, isForeground, ConsoleColor.Green)); break;
                        case 'b': colors.Add(new(sb.Length, isForeground, ConsoleColor.Cyan)); break;
                        case 'c': colors.Add(new(sb.Length, isForeground, ConsoleColor.Red)); break;
                        case 'd': colors.Add(new(sb.Length, isForeground, ConsoleColor.Magenta)); break;
                        case 'e': colors.Add(new(sb.Length, isForeground, ConsoleColor.Yellow)); break;
                        case 'f': colors.Add(new(sb.Length, isForeground, ConsoleColor.White)); break;
                        case 'k': funkyMode = true; break;
                        case 'l': break;
                        case 'm': break;
                        case 'n': break;
                        case 'o': break;
                        case 'r':
                            funkyMode = false;
                            colors.Add(new(sb.Length, isForeground, isForeground ? ConsoleColor.White : ConsoleColor.Black));
                            break;
                    }
                }
                sb.Append(value.AsSpan(curIndex, value.Length - curIndex));
                if (matches[^1].Value[^1] != 'r')
                    colors.Add(new(sb.Length, true, ConsoleColor.White));
                Write(sb.ToString(), colors);
            }
        }

        private static (byte R, byte G, byte B) ParseHexRgb(ReadOnlySpan<char> hex) {
            byte r = byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte g = byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte b = byte.Parse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return (r, g, b);
        }

        private static readonly (int R, int G, int B, ConsoleColor Color)[] ConsoleColorMap = {
            (0,   0,   0,   ConsoleColor.Black),
            (0,   0,   170, ConsoleColor.DarkBlue),
            (0,   170, 0,   ConsoleColor.DarkGreen),
            (0,   170, 170, ConsoleColor.DarkCyan),
            (170, 0,   0,   ConsoleColor.DarkRed),
            (170, 0,   170, ConsoleColor.DarkMagenta),
            (255, 170, 0,   ConsoleColor.DarkYellow),
            (170, 170, 170, ConsoleColor.Gray),
            (85,  85,  85,  ConsoleColor.DarkGray),
            (85,  85,  255, ConsoleColor.Blue),
            (85,  255, 85,  ConsoleColor.Green),
            (85,  255, 255, ConsoleColor.Cyan),
            (255, 85,  85,  ConsoleColor.Red),
            (255, 85,  255, ConsoleColor.Magenta),
            (255, 255, 85,  ConsoleColor.Yellow),
            (255, 255, 255, ConsoleColor.White),
        };

        private static ConsoleColor NearestConsoleColor(byte r, byte g, byte b) {
            double bestDist = double.MaxValue;
            ConsoleColor bestColor = ConsoleColor.White;
            foreach (var (cr, cg, cb, cc) in ConsoleColorMap) {
                double rMean = (r + cr) / 2.0;
                double dr = r - cr, dg = g - cg, db = b - cb;
                double dist = (2 + rMean / 256) * dr * dr + 4 * dg * dg + (2 + (255 - rMean) / 256) * db * db;
                if (dist < bestDist) {
                    bestDist = dist;
                    bestColor = cc;
                }
            }
            return bestColor;
        }
    }
}