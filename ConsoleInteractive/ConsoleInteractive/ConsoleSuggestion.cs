using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Wcwidth;

namespace ConsoleInteractive {
    public static class ConsoleSuggestion {
        public static bool EnableColor { get; set; } = true;
        public static bool Enable24bitColor { get; set; } = true;
        public static bool UseBasicArrow { get; set; } = false;

        public static int MaxSuggestionCount = 6, MaxSuggestionLength = 30;

        public const int MaxValueOfMaxSuggestionCount = 32;

        private static bool InUse = false;

        private static bool AlreadyTriggerTab = false, LastKeyIsTab = false, Hide = false, HideAndClear = false;

        private static Tuple<int, int> TargetTextRange = new(1, 1), LastTextRange = new(1, 1);

        private static int StartIndex = 0, PopupWidth = 0;

        private static int ChoosenIndex = 0, ViewTop = 0, ViewBottom = 0;

        private static Suggestion[] Suggestions = Array.Empty<Suggestion>();

        public static void UpdateSuggestions(Suggestion[] Suggestions, Tuple<int, int> range) {
            if (Console.IsOutputRedirected) {
                ClearSuggestions();
                return;
            }

            int maxLength = 0;
            foreach (Suggestion sug in Suggestions)
                maxLength = Math.Max(maxLength,
                    Math.Min(MaxSuggestionLength, sug.ShortText.Length +
                        (sug.TooltipWidth == 0 ? 0 : (sug.TooltipWidth + 1))));

            if (Suggestions.Length == 0 || maxLength == 0) {
                ClearSuggestions();
                return;
            }

            if (Console.BufferWidth < maxLength) {
                ClearSuggestions();
                return;
            }

            lock (InternalContext.WriteLock) {
                if (InUse && CheckIfNeedClear(Suggestions, range, maxLength))
                    DrawHelper.ClearSuggestionPopup();

                ConsoleSuggestion.Suggestions = Suggestions;

                TargetTextRange = range;
                StartIndex = range.Item1 - 1;

                PopupWidth = maxLength + 2;

                ChoosenIndex = 0;

                ViewTop = 0;
                ViewBottom = Math.Min(Suggestions.Length, MaxSuggestionCount);

                if (!Hide) {
                    InUse = true;
                    DrawHelper.DrawSuggestionPopup();
                }

                AlreadyTriggerTab = false;
                LastKeyIsTab = false;
            }
        }

        public static void ClearSuggestions() {
            if (InUse) {
                lock (InternalContext.WriteLock) {
                    InUse = false;
                    DrawHelper.ClearSuggestionPopup();
                }
            } else if (Hide) {
                HideAndClear = true;
            }
        }

        public static int SetMaxSuggestionCount(int count) {
            if (count < 1) count = 1;
            if (count > MaxValueOfMaxSuggestionCount) count = MaxValueOfMaxSuggestionCount;
            MaxSuggestionCount = count;
            return count;
        }

        public static int SetMaxSuggestionLength(int length) {
            if (length < 4) length = 4;
            MaxSuggestionLength = length;
            return length;
        }

        public static void SetColors(string? Normal = null,
                                     string? NormalBg = null,
                                     string? Highlight = null,
                                     string? HighlightBg = null,
                                     string? Tooltip = null,
                                     string? TooltipHighlight = null,
                                     string? Arrow = null) {
            Normal = StringToColorCode(Normal, false);
            if (!string.IsNullOrEmpty(Normal))
                DrawHelper.NormalColorCode = Normal;

            NormalBg = StringToColorCode(NormalBg, true);
            if (!string.IsNullOrEmpty(NormalBg))
                DrawHelper.NormalBgColorCode = NormalBg;

            Highlight = StringToColorCode(Highlight, false);
            if (!string.IsNullOrEmpty(Highlight))
                DrawHelper.HighlightColorCode = Highlight;

            HighlightBg = StringToColorCode(HighlightBg, true);
            if (!string.IsNullOrEmpty(HighlightBg))
                DrawHelper.HighlightBgColorCode = HighlightBg;

            Tooltip = StringToColorCode(Tooltip, false);
            if (!string.IsNullOrEmpty(Tooltip))
                DrawHelper.TooltipColorCode = Tooltip;

            TooltipHighlight = StringToColorCode(TooltipHighlight, false);
            if (!string.IsNullOrEmpty(TooltipHighlight))
                DrawHelper.HighlightTooltipColorCode = TooltipHighlight;

            Arrow = StringToColorCode(Arrow, false);
            if (!string.IsNullOrEmpty(Arrow))
                DrawHelper.ArrowColorCode = Arrow;
        }

        internal static void BeforeWrite(string message, int linesAdded) {
            if (InUse) DrawHelper.ClearSuggestionPopup(linesAdded);
            DrawHelper.AddMessage(message);
        }

        internal static void AfterWrite() {
            if (InUse) DrawHelper.DrawSuggestionPopup();
        }

        internal static void OnInputUpdate() {
            LastKeyIsTab = false;
        }

        internal static bool HandleEscape() {
            lock (InternalContext.WriteLock) {
                if (InUse) {
                    Hide = true;
                    ClearSuggestions();
                    return true;
                } else {
                    return false;
                }
            }
        }

        internal static bool HandleTab() {
            lock (InternalContext.WriteLock) {
                if (Hide) {
                    Hide = false;
                    if (!HideAndClear) {
                        InUse = true;
                        DrawHelper.DrawSuggestionPopup();
                    }
                    HideAndClear = false;
                    return true;
                } else if (InUse) {
                    int suggestionWidth;
                    if (AlreadyTriggerTab) {
                        if (LastKeyIsTab)
                            HandleDownArrow();
                        suggestionWidth = ConsoleBuffer.Replace(LastTextRange.Item1, LastTextRange.Item2, Suggestions[ChoosenIndex].Text);
                    } else {
                        suggestionWidth = ConsoleBuffer.Replace(TargetTextRange.Item1, TargetTextRange.Item2, Suggestions[ChoosenIndex].Text);
                    }
                    LastTextRange = new(TargetTextRange.Item1, TargetTextRange.Item1 + suggestionWidth);
                    ConsoleBuffer.RedrawInputArea();
                    AlreadyTriggerTab = true;
                    LastKeyIsTab = true;
                    if (Suggestions[ChoosenIndex].Text == "/")
                        ConsoleReader.CheckInputBufferUpdate();
                    DrawHelper.RedrawOnTab();
                    return true;
                } else {
                    return false;
                }
            }
        }

        internal static bool HandleUpArrow() {
            lock (InternalContext.WriteLock) {
                if (InUse) {
                    LastKeyIsTab = false;
                    if (ChoosenIndex == 0) {
                        ChoosenIndex = Suggestions.Length - 1;
                        ViewTop = Math.Max(0, Suggestions.Length - MaxSuggestionCount);
                        ViewBottom = Suggestions.Length;
                        DrawHelper.DrawSuggestionPopup(refreshMsgBuf: false);
                    } else {
                        --ChoosenIndex;
                        if (ChoosenIndex == ViewTop && ViewTop != 0) {
                            --ViewTop;
                            --ViewBottom;
                            DrawHelper.DrawSuggestionPopup(refreshMsgBuf: false);
                        } else {
                            DrawHelper.RedrawOnArrowKey(offset: 1);
                        }
                    }
                    return true;
                } else {
                    return false;
                }
            }
        }

        internal static bool HandleDownArrow() {
            lock (InternalContext.WriteLock) {
                if (InUse) {
                    LastKeyIsTab = false;
                    if (ChoosenIndex == Suggestions.Length - 1) {
                        ChoosenIndex = 0;
                        ViewTop = 0;
                        ViewBottom = Math.Min(Suggestions.Length, MaxSuggestionCount);
                        DrawHelper.DrawSuggestionPopup(refreshMsgBuf: false);
                    } else {
                        ++ChoosenIndex;
                        if (ChoosenIndex == ViewBottom - 1 && ViewBottom != Suggestions.Length) {
                            ++ViewTop;
                            ++ViewBottom;
                            DrawHelper.DrawSuggestionPopup(refreshMsgBuf: false);
                        } else {
                            DrawHelper.RedrawOnArrowKey(offset: -1);
                        }
                    }
                    return true;
                } else {
                    return false;
                }
            }
        }

        internal static void HandleEnter() {
            ClearSuggestions();
        }

        private static bool CheckIfNeedClear(Suggestion[] Suggestions, Tuple<int, int> range, int maxLength) {
            if (Suggestions.Length < MaxSuggestionCount && ConsoleSuggestion.Suggestions.Length >= MaxSuggestionCount)
                return true;
            if (ConsoleSuggestion.Suggestions.Length < MaxSuggestionCount && ConsoleSuggestion.Suggestions.Length > Suggestions.Length)
                return true;
            if (StartIndex < (range.Item1 - 1))
                return true;
            if (StartIndex + PopupWidth > (range.Item1 - 1 + maxLength + 2))
                return true;
            return false;
        }

        private static string? StringToColorCode(string? input, bool background) {
            if (string.IsNullOrWhiteSpace(input))
                return null;
            if (input.Length < 6 || input.Length > 7)
                return null;
            if (input.Length == 6 && input[0] == '#')
                return null;
            if (input.Length == 7 && input[0] != '#')
                return null;
            try {
                int rgb = Convert.ToInt32(input.Length == 7 ? input[1..] : input, 16);

                int r = (rgb & 0xff0000) >> 16;
                int g = (rgb & 0xff00) >> 8;
                int b = (rgb & 0xff);

                return string.Format("\u001b[{0};2;{1};{2};{3}m", background ? 48 : 38, r, g, b);
            } catch {
                return null;
            }
        }

        public class Suggestion {
            public string Text, Tooltip;
            public string ShortText;

            public int TooltipWidth;

            private Tuple<int, string>? _cache = null;

            public Suggestion(string text, string? tooltip = null) {
                Text = text;
                if (Text.Length <= MaxSuggestionLength) {
                    ShortText = Text;
                } else {
                    int half = MaxSuggestionLength / 2;
                    ShortText = Text[..(half - 1)] + (MaxSuggestionLength % 2 == 0 ? ".." : "...") + Text[(Text.Length - half + 1)..];
                }

                Tooltip = tooltip ?? string.Empty;
                TooltipWidth = 0;
                foreach (char c in Tooltip)
                    TooltipWidth += Math.Max(0, UnicodeCalculator.GetWidth(c));
            }

            internal string GetShortTooltip(int widthLimit) {
                if (widthLimit <= 2) return new string('.', widthLimit);
                widthLimit -= 2;

                if (_cache != null && _cache.Item1 == widthLimit)
                    return _cache.Item2;

                for (int i = Tooltip.Length - 1, limit = widthLimit; i > 0 && limit > 0; --i) {
                    char c = Tooltip[i];
                    int width = Math.Max(0, UnicodeCalculator.GetWidth(c));
                    if (limit - width < 0)
                        return (_cache = new(widthLimit, new string('.', 2 + limit) + Tooltip[(i + 1)..])).Item2;
                    limit -= width;
                    if (limit == 0)
                        return (_cache = new(widthLimit, ".." + Tooltip[i..])).Item2;
                }

                return (_cache = new(widthLimit, "..")).Item2;
            }
        }

        private static class DrawHelper {
            internal static string NormalColorCode = "\u001b[38;2;248;250;252m";          // Slate   5%  (#f8fafc)
            internal static string NormalBgColorCode = "\u001b[48;2;100;116;139m";        // Slate  50%  (#64748b)

            internal static string HighlightColorCode = "\u001b[38;2;51;65;85m";          // Slate  70%  (#334155)
            internal static string HighlightBgColorCode = "\u001b[48;2;253;224;71m";      // Yellow 30%  (#fde047)

            internal static string TooltipColorCode = "\u001b[38;2;125;211;252m";         // Sky    30%  (#7dd3fc)
            internal static string HighlightTooltipColorCode = "\u001b[38;2;59;130;246m"; // Blue   50%  (#3b82f6)

            internal static string ArrowColorCode = "\u001b[38;2;156;163;175m";           // Gray   40%  (#9ca3af)

            internal const string ResetColorCode = "\u001b[0m";

            private enum ColorType { Reset, Normal, NormalBg, Highlight, HighlightBg, Tooltip, HighlightTooltip, Arrow };

            private static string LastColorFg = ResetColorCode, LastColorBg = ResetColorCode;

            private static int LastDrawStartPos = -1;
            private static readonly BgMessageBuffer[] BgBuffer = new BgMessageBuffer[MaxValueOfMaxSuggestionCount];

            internal static void DrawSuggestionPopup(bool refreshMsgBuf = true, int bufWidth = -1) {
                BgMessageBuffer[] messageBuffers = Array.Empty<BgMessageBuffer>();
                int curBufIdx = -1, nextMessageIdx = 0;
                lock (InternalContext.WriteLock) {
                    if (bufWidth == -1) bufWidth = Console.BufferWidth;
                    if (PopupWidth > bufWidth) return;
                    (int left, int top) = Console.GetCursorPosition();
                    LastDrawStartPos = GetDrawStartPos(bufWidth);
                    InternalContext.SetCursorVisible(false);
                    for (int i = ViewBottom - 1; i >= ViewTop; --i) {
                        if (refreshMsgBuf) {
                            if (curBufIdx < 0) {
                                messageBuffers = GetBgMessageBuffer(RecentMessageHandler.GetRecentMessage(nextMessageIdx), LastDrawStartPos, PopupWidth, bufWidth);
                                curBufIdx = messageBuffers.Length - 1;
                                ++nextMessageIdx;
                            }
                            BgBuffer[i - ViewTop] = messageBuffers[curBufIdx];
                            --curBufIdx;
                        }
                        DrawSingleSuggestionPopup(i, BgBuffer[i - ViewTop], cursorTop: top - (ViewBottom - i));
                    }
                    Console.SetCursorPosition(left, top);
                    InternalContext.SetCursorVisible(true);
                }
            }

            internal static void ClearSuggestionPopup(int linesAdded = 0, int bufWidth = -1) {
                int DisplaySuggestionsCnt = Math.Min(MaxSuggestionCount, Suggestions.Length);
                lock (InternalContext.WriteLock) {
                    if (bufWidth == -1) bufWidth = Console.BufferWidth;
                    int drawStartPos = GetDrawStartPos(bufWidth);
                    (int left, int top) = Console.GetCursorPosition();
                    InternalContext.SetCursorVisible(false);
                    for (int i = 0; i < DisplaySuggestionsCnt; ++i) {
                        if (linesAdded > 0 && i >= linesAdded && drawStartPos == LastDrawStartPos) break;
                        ClearSingleSuggestionPopup(BgBuffer[i], cursorTop: top - (DisplaySuggestionsCnt - i));
                    }
                    Console.SetCursorPosition(left, top);
                    InternalContext.SetCursorVisible(true);
                }
            }

            internal static void RedrawOnArrowKey(int offset) {
                lock (InternalContext.WriteLock) {
                    (int left, int top) = Console.GetCursorPosition();
                    InternalContext.SetCursorVisible(false);

                    int cursorTop = top - (ViewBottom - ChoosenIndex);
                    DrawSingleSuggestionPopup(ChoosenIndex, BgBuffer[ChoosenIndex - ViewTop], cursorTop);
                    DrawSingleSuggestionPopup(ChoosenIndex + offset, BgBuffer[ChoosenIndex - ViewTop + offset], cursorTop + offset);

                    Console.SetCursorPosition(left, top);
                    InternalContext.SetCursorVisible(true);
                }
            }

            internal static void RedrawOnTab() {
                lock (InternalContext.WriteLock) {
                    int bufWidth = Console.BufferWidth;
                    if (GetDrawStartPos(bufWidth) != LastDrawStartPos) {
                        ClearSuggestionPopup(bufWidth: bufWidth);
                        DrawSuggestionPopup(refreshMsgBuf: true, bufWidth: bufWidth);
                    }
                }
            }

            internal static void AddMessage(string message) {
                RecentMessageHandler.AddMessage(message);
            }

            private static int GetDrawStartPos(int bufWidth) {
                return Math.Max(0, Math.Min(bufWidth - PopupWidth, StartIndex + ConsoleBuffer.PrefixTotalLength - ConsoleBuffer.BufferOutputAnchor));
            }

            private static string GetColorCode(ColorType color) {
                if (!EnableColor || !ConsoleWriter.UseVT100ColorCode)
                    return string.Empty;

                if (color == ColorType.Reset) {
                    LastColorBg = LastColorFg = ResetColorCode;
                    return ResetColorCode;
                }

                bool foreground = true;
                string result = string.Empty;
                switch (color) {
                    case ColorType.Normal:
                        result = Enable24bitColor ? NormalColorCode : "\u001b[97m";
                        break;
                    case ColorType.NormalBg:
                        foreground = false;
                        result = Enable24bitColor ? NormalBgColorCode : "\u001b[100m";
                        break;
                    case ColorType.Highlight:
                        result = Enable24bitColor ? HighlightColorCode : "\u001b[97m";
                        break;
                    case ColorType.HighlightBg:
                        foreground = false;
                        result = Enable24bitColor ? HighlightBgColorCode : "\u001b[43m";
                        break;
                    case ColorType.Tooltip:
                        result = Enable24bitColor ? TooltipColorCode : "\u001b[96m";
                        break;
                    case ColorType.HighlightTooltip:
                        result = Enable24bitColor ? HighlightTooltipColorCode : "\u001b[34m";
                        break;
                    case ColorType.Arrow:
                        result = Enable24bitColor ? ArrowColorCode : "\u001b[37m";
                        break;
                }

                if (foreground) {
                    if (result == LastColorFg)
                        return string.Empty;
                    LastColorFg = result;
                } else {
                    if (result == LastColorBg)
                        return string.Empty;
                    LastColorBg = result;
                }

                return result;
            }

            private static void DrawSingleSuggestionPopup(int index, BgMessageBuffer buf, int cursorTop) {
                if (cursorTop < 0) return;

                List<Tuple<int, bool, ConsoleColor>>? colors = ConsoleWriter.UseVT100ColorCode ? null : new();

                StringBuilder sb = new(PopupWidth + 4 * NormalColorCode.Length);

                if (colors == null) {
                    sb.Append(GetColorCode(ColorType.Reset));
                } else {
                    colors.Add(new(sb.Length, true, ConsoleColor.White));
                    colors.Add(new(sb.Length, false, ConsoleColor.Black));
                }

                if (buf.StartSpace)
                    sb.Append(' ');

                if (colors == null) {
                    sb.Append(GetColorCode(ColorType.NormalBg));
                    sb.Append(GetColorCode(ColorType.Arrow));
                } else {
                    colors.Add(new(sb.Length, false, ConsoleColor.DarkGray));
                    colors.Add(new(sb.Length, true, ConsoleColor.Gray));
                }

                if (index == ViewTop && ViewTop != 0)
                    sb.Append(UseBasicArrow ? '^' : '↑');
                else if (index + 1 == ViewBottom && ViewBottom != Suggestions.Length)
                    sb.Append(UseBasicArrow ? 'v' : '↓');
                else if (index == ChoosenIndex)
                    sb.Append('>');
                else
                    sb.Append(' ');

                if (index == ChoosenIndex) {
                    if (colors == null) {
                        sb.Append(GetColorCode(ColorType.Highlight));
                        sb.Append(GetColorCode(ColorType.HighlightBg));
                    } else {
                        colors.Add(new(sb.Length, true, ConsoleColor.White));
                        colors.Add(new(sb.Length, false, ConsoleColor.DarkYellow));
                    }
                } else {
                    if (colors == null)
                        sb.Append(GetColorCode(ColorType.Normal));
                    else
                        colors.Add(new(sb.Length, true, ConsoleColor.White));
                }

                sb.Append(Suggestions[index].ShortText);

                int lastSpace = PopupWidth - 2 - Suggestions[index].ShortText.Length;
                if (Suggestions[index].TooltipWidth > 0 && lastSpace > 1) {
                    --lastSpace;
                    sb.Append(' ');

                    if (colors == null)
                        sb.Append(GetColorCode((index == ChoosenIndex) ? ColorType.HighlightTooltip : ColorType.Tooltip));
                    else
                        colors.Add(new(sb.Length, true, (index == ChoosenIndex) ? ConsoleColor.DarkBlue : ConsoleColor.Cyan));

                    if (lastSpace >= Suggestions[index].TooltipWidth) {
                        sb.Append(' ', lastSpace - Suggestions[index].TooltipWidth);
                        sb.Append(Suggestions[index].Tooltip);
                    } else {
                        sb.Append(Suggestions[index].GetShortTooltip(lastSpace));
                    }
                } else if (lastSpace > 0) {
                    sb.Append(new string(' ', lastSpace));
                }

                if (index == ChoosenIndex) {
                    if (colors == null)
                        sb.Append(GetColorCode(ColorType.NormalBg));
                    else
                        colors.Add(new(sb.Length, false, ConsoleColor.DarkGray));
                }

                if (colors == null)
                    sb.Append(GetColorCode(ColorType.Arrow));
                else
                    colors.Add(new(sb.Length, true, ConsoleColor.Gray));

                if (index == ViewTop && ViewTop != 0)
                    sb.Append(UseBasicArrow ? '^' : '↑');
                else if (index + 1 == ViewBottom && ViewBottom != Suggestions.Length)
                    sb.Append(UseBasicArrow ? 'v' : '↓');
                else if (index == ChoosenIndex)
                    sb.Append('<');
                else
                    sb.Append(' ');

                if (colors == null) {
                    sb.Append(GetColorCode(ColorType.Reset));
                } else {
                    colors.Add(new(sb.Length, true, ConsoleColor.White));
                    colors.Add(new(sb.Length, false, ConsoleColor.Black));
                }

                if (buf.EndSpace)
                    sb.Append(' ');

                Console.SetCursorPosition(buf.CursorStart, cursorTop);

                InternalWriter.WriteConsole(sb.ToString(), colors);
            }

            private static void ClearSingleSuggestionPopup(BgMessageBuffer buf, int cursorTop) {
                if (cursorTop < 0) return;

                StringBuilder sb = new(PopupWidth + ResetColorCode.Length);

                if (ConsoleWriter.EnableColor)
                    sb.Append(buf.Text);
                else
                    sb.Append(InternalContext.VT100CodeRegex.Replace(buf.Text, string.Empty));

                if (ConsoleWriter.EnableColor && ConsoleWriter.UseVT100ColorCode)
                    sb.Append(ResetColorCode);

                sb.Append(' ', buf.AfterTextSpace);

                Console.SetCursorPosition(buf.CursorStart, cursorTop);
                if (!ConsoleWriter.UseVT100ColorCode)
                    Console.ForegroundColor = ConsoleColor.White;
                Console.Write(sb.ToString());
            }

            private static BgMessageBuffer[] GetBgMessageBuffer(RecentMessageHandler.RecentMessage? msg, int start, int length, int bufWidth) {
                if (msg == null || msg.Message.Length == 0)
                    return new BgMessageBuffer[1] { new BgMessageBuffer(start, start + length, length) };

                int charIndex = 0;
                char[] chars = msg.Message.ToCharArray();
                List<BgMessageBuffer> buffers = new();

                while (charIndex < chars.Length) {
                    int cursorPos = 0;
                    int charStart, charEnd;

                    string Text = string.Empty;
                    bool StartSpace = false, EndSpace = false;
                    int CursorStart, CutsorEnd, AfterTextSpace = 0;

                    while (cursorPos < start) {
                        if (charIndex < chars.Length) {
                            int width = GetCharacterWidth(chars, charIndex, out int consumed);
                            if (cursorPos + width > start) {
                                StartSpace = true;
                                break;
                            }
                            cursorPos += width;
                            charIndex += consumed;
                        } else {
                            cursorPos = start;
                            break;
                        }
                    }
                    charStart = charIndex;
                    CursorStart = cursorPos;

                    int end = start + length;
                    while (cursorPos < end) {
                        if (charIndex < chars.Length) {
                            cursorPos += GetCharacterWidth(chars, charIndex, out int consumed);

                            if (cursorPos > end)
                                EndSpace = true;

                            charIndex += consumed;
                        } else {
                            int last = end - cursorPos;
                            cursorPos += last;
                            AfterTextSpace += last;
                            break;
                        }
                    }
                    charEnd = charIndex;
                    CutsorEnd = cursorPos;

                    if (charStart < charEnd) {
                        StringBuilder sb = new();

                        // Seed the style the covered slice starts in, then replay the codes inside it.
                        int codeIdx = GetFirstCodeAfterReset(msg.Codes, charStart);
                        while (codeIdx < msg.Codes.Length && msg.Codes[codeIdx].Item1 < charStart)
                            sb.Append(msg.Codes[codeIdx++].Item2);

                        for (int i = charStart; i < charEnd; ++i) {
                            while (codeIdx < msg.Codes.Length && msg.Codes[codeIdx].Item1 == i)
                                sb.Append(msg.Codes[codeIdx++].Item2);
                            sb.Append(chars[i]);
                        }
                        Text = sb.ToString();
                    }

                    buffers.Add(new BgMessageBuffer(CursorStart, CutsorEnd, AfterTextSpace, Text, StartSpace, EndSpace));

                    while (cursorPos <= bufWidth) {
                        if (charIndex < chars.Length) {
                            int width = GetCharacterWidth(chars, charIndex, out int consumed);
                            if (cursorPos + width > bufWidth)
                                break;
                            cursorPos += width;
                            charIndex += consumed;
                        } else {
                            break;
                        }
                    }
                }

                return buffers.ToArray();
            }

            /// <summary>
            /// The display width of the character at <paramref name="index"/>, and how many UTF-16 units it
            /// occupies.
            /// </summary>
            /// <remarks>
            /// A character outside the BMP is stored as a surrogate PAIR, and measuring the halves
            /// separately is what split emoji across a popup edge: each half reports width 1, so a
            /// two-column emoji measured two columns by accident while the walk was still free to stop
            /// between the halves. The restored slice then began with a lone low surrogate, which is not a
            /// character at all, and the terminal drew replacement glyphs. Wcwidth already has a Rune
            /// overload; this just uses it.
            /// </remarks>
            private static int GetCharacterWidth(char[] chars, int index, out int consumed) {
                if (char.IsHighSurrogate(chars[index]) && index + 1 < chars.Length && char.IsLowSurrogate(chars[index + 1])) {
                    consumed = 2;
                    return Math.Max(0, UnicodeCalculator.GetWidth(new Rune(chars[index], chars[index + 1])));
                }

                consumed = 1;
                return Math.Max(0, UnicodeCalculator.GetWidth(chars[index]));
            }

            /// <summary>
            /// The index of the first code that has to be replayed to reproduce the style in effect at
            /// <paramref name="charStart"/>: everything from the last full reset at or before it.
            /// </summary>
            /// <remarks>
            /// This used to seek the LAST code at or before the cut and replay only that, once per colour
            /// list. That is correct only while a code REPLACES the whole style, which is true of a colour
            /// and false of everything else: bold, italic, underline and strikethrough accumulate, so a run
            /// that was "yellow and bold" needs both codes replayed and the old seek could produce only one.
            /// Combined with the classifier below, which discarded any escape that was not purely a colour,
            /// decorated text came back undecorated and text whose colour arrived in the same escape as its
            /// decoration came back with no colour at all. A reset is the only point at which the style is
            /// known from nothing, so replay starts there.
            /// </remarks>
            private static int GetFirstCodeAfterReset(Tuple<int, string>[] codes, int charStart) {
                for (int i = GetLowerBound(codes, charStart); i >= 0; --i) {
                    if (codes[i].Item1 <= charStart && codes[i].Item2 == ResetColorCode)
                        return i;
                }

                return 0;
            }

            private static int GetLowerBound(Tuple<int, string>[] ColorCodes, int charStart) {
                if (ColorCodes.Length == 0)
                    return -1;

                int left = 0, right = ColorCodes.Length - 1;
                while (left < right) {
                    int mid = (left + right + 1) / 2;
                    if (ColorCodes[mid].Item1 <= charStart)
                        left = mid;
                    else
                        right = mid - 1;
                }
                return left;
            }

            private static class RecentMessageHandler {
                private static int Count = 0, Index = 0;

                private static readonly RecentMessage[] Messages = new RecentMessage[MaxValueOfMaxSuggestionCount];

                internal static void AddMessage(string message) {
                    string[] lines = message.Split('\n');
                    int startIdx = Math.Max(0, lines.Length - MaxValueOfMaxSuggestionCount);
                    for (int i = startIdx; i < lines.Length; i++) {
                        Index = Count < MaxValueOfMaxSuggestionCount ? Count++ : (Index + 1) % MaxValueOfMaxSuggestionCount;
                        Messages[Index] = new(lines[i]);
                    }
                }

                internal static RecentMessage? GetRecentMessage(int index) {
                    if (index >= Count)
                        return null;
                    RecentMessage message = Messages[(MaxValueOfMaxSuggestionCount + Index - index) % MaxValueOfMaxSuggestionCount];
                    message.ParseColorCode();
                    return message;
                }

                internal class RecentMessage {
                    private readonly static Regex ContralCharRegex = new(@"[\u0000-\u001F]", RegexOptions.Compiled);
                    private readonly static Regex EscapeCodeRegex = new(@"\u001B\[[\d;]+m", RegexOptions.Compiled);

                    bool Parsed = false;

                    /// <summary>
                    /// Every SGR escape in the line, in order, each with the index in <see cref="Message"/>
                    /// of the character it takes effect at.
                    /// </summary>
                    /// <remarks>
                    /// One ordered list, not a foreground list and a background list. The split existed to
                    /// let the restore seed one code from each, which only works while a code replaces the
                    /// whole style; decorations accumulate instead, so the restore replays a range and needs
                    /// them interleaved in the order they were written.
                    /// </remarks>
                    public Tuple<int, string>[] Codes = Array.Empty<Tuple<int, string>>();

                    public string Message = string.Empty, RawMessage = string.Empty;

                    public RecentMessage(string message) {
                        RawMessage = message;
                    }

                    public void ParseColorCode() {
                        if (Parsed)
                            return;

                        // Every SGR escape is kept, not just the ones that name a colour. What used to
                        // happen: an escape matching none of the six colour patterns was DISCARDED, so
                        // ESC[1m (bold) and friends never reached the restore, and a writer that emits a
                        // colour and a decoration as one sequence (ESC[38;2;255;255;85;1m, which is legal
                        // and common) had that sequence thrown away whole, losing the colour too. Anything
                        // the writer put on the line is style the restore has to reproduce; classifying it
                        // was never the restore's job.
                        List<Tuple<int, string>> codes = new();
                        MatchCollection matchs = EscapeCodeRegex.Matches(RawMessage);
                        int colorLen = 0;
                        foreach (Match match in matchs) {
                            codes.Add(new(match.Index - colorLen, match.Groups[0].Value));
                            colorLen += match.Length;
                        }

                        Message = ContralCharRegex.Replace(EscapeCodeRegex.Replace(RawMessage, string.Empty), string.Empty);
                        Codes = codes.ToArray();

                        Parsed = true;
                    }
                }
            }

            private record BgMessageBuffer {
                public BgMessageBuffer(int cursorStart, int cutsorEnd, int afterTextSpace) {
                    CursorStart = cursorStart;
                    CutsorEnd = cutsorEnd;
                    StartSpace = false;
                    EndSpace = false;
                    Text = string.Empty;
                    AfterTextSpace = afterTextSpace;
                }

                public BgMessageBuffer(int cursorStart, int cutsorEnd, int afterTextSpace, string text, bool startSpace, bool endSpace) {
                    CursorStart = cursorStart;
                    CutsorEnd = cutsorEnd;
                    StartSpace = startSpace;
                    EndSpace = endSpace;
                    Text = text;
                    AfterTextSpace = afterTextSpace;
                }

                public int CursorStart { get; init; }
                public int CutsorEnd { get; init; }

                public bool StartSpace { get; init; }
                public bool EndSpace { get; init; }

                public string Text { get; init; }

                public int AfterTextSpace { get; init; }
            }
        }
    }
}
