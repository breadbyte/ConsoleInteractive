using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleInteractive {
    public static class ConsoleSuggestion {
        private const int MaxSuggestions = 6, MaxSuggestionLength = 18;

        private static bool InUse = false;

        private static bool alreadyTriggerTab = false, lastKeyIsTab = false, hidePopup = false;

        private static Tuple<int, int> TargetTextRange = new(1, 1), LastTextRange = new(1, 1);

        private static int StartIndex = 0, PopupWidth = 0;

        private static int ChoosenIndex = 0, ViewTop = 0, ViewBottom = 0;

        private static Suggestion[] Suggestions = Array.Empty<Suggestion>();


        internal static void OnWriteLine(string message, int linesAdded) {
            if (InUse) DrawHelper.ClearSuggestionPopup(linesAdded);
            DrawHelper.AddMessage(message);
            if (InUse) DrawHelper.DrawSuggestionPopup();
        }

        internal static void OnInputUpdate() {
            lastKeyIsTab = false;
        }

        internal static bool HandleEscape() {
            lock (InternalContext.WriteLock) {
                if (InUse) {
                    hidePopup = true;
                    ClearSuggestions();
                    return true;
                } else {
                    return false;
                }
            }
        }

        internal static bool HandleTab() {
            lock (InternalContext.WriteLock) {
                if (hidePopup) {
                    hidePopup = false;
                    InUse = true;
                    DrawHelper.DrawSuggestionPopup();
                    return true;
                } else if (InUse) {
                    if (alreadyTriggerTab) {
                        if (lastKeyIsTab)
                            HandleDownArrow();
                        ConsoleBuffer.Replace(LastTextRange.Item1, LastTextRange.Item2, Suggestions[ChoosenIndex].Text);
                    } else {
                        ConsoleBuffer.Replace(TargetTextRange.Item1, TargetTextRange.Item2, Suggestions[ChoosenIndex].Text);
                    }
                    LastTextRange = new(TargetTextRange.Item1, ConsoleBuffer.BufferPosition);
                    alreadyTriggerTab = true;
                    lastKeyIsTab = true;
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
                    lastKeyIsTab = false;
                    if (ChoosenIndex == 0) {
                        ChoosenIndex = Suggestions.Length - 1;
                        ViewTop = Math.Max(0, Suggestions.Length - MaxSuggestions);
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
                    lastKeyIsTab = false;
                    if (ChoosenIndex == Suggestions.Length - 1) {
                        ChoosenIndex = 0;
                        ViewTop = 0;
                        ViewBottom = Math.Min(Suggestions.Length, MaxSuggestions);
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

        public static void UpdateSuggestions(Suggestion[] Suggestions, Tuple<int, int> range) {
            int maxLength = 0;
            foreach (Suggestion sug in Suggestions)
                maxLength = Math.Max(maxLength, sug.ShortText.Length);

            if (Suggestions.Length == 0 || maxLength == 0) {
                ClearSuggestions();
                return;
            }

            lock (InternalContext.WriteLock) {
                if (InUse)
                    DrawHelper.ClearSuggestionPopup();

                ConsoleSuggestion.Suggestions = Suggestions;

                TargetTextRange = range;
                StartIndex = range.Item1 - 1;

                PopupWidth = maxLength + 2;

                ChoosenIndex = 0;

                ViewTop = 0;
                ViewBottom = Math.Min(Suggestions.Length, MaxSuggestions);

                if (!hidePopup) {
                    InUse = true;
                    DrawHelper.DrawSuggestionPopup();
                }

                alreadyTriggerTab = false;
                lastKeyIsTab = false;
            }
        }

        public static void ClearSuggestions() {
            lock (InternalContext.WriteLock) {
                if (InUse) {
                    InUse = false;
                    DrawHelper.ClearSuggestionPopup();
                } else if (hidePopup) {
                    hidePopup = false;
                }
            }
        }

        public class Suggestion {
            public string ShortText;
            public string Text, Type;

            public Suggestion(string text, string type) {
                Text = text;
                Type = type;
                if (Text.Length <= MaxSuggestionLength) {
                    ShortText = Text;
                } else {
                    int half = MaxSuggestionLength / 2;
                    ShortText = Text[..(half - 1)] + (MaxSuggestionLength % 2 == 0 ? ".." : "...") + Text[(Text.Length - half + 1)..];
                }
            }
        }

        private static class DrawHelper {
            private const string NormalBgColorCode = "\u001b[100m"; // Bright Black (Gray)
            private const string NormalColorCode = "\u001b[97m"; // Bright White

            private const string ArrowColorCode = "\u001b[37m"; // White

            private const string HighlightBgColorCode = "\u001b[43m"; // Yellow
            private const string HighlightColorCode = "\u001b[97m"; // Bright White

            private const string ResetColorCode = "\u001b[0m";

            private static int LastDrawStartPos = -1;
            private static readonly BgMessageBuffer[] BgBuffer = new BgMessageBuffer[MaxSuggestions];

            internal static void DrawSuggestionPopup(bool refreshMsgBuf = true) {
                BgMessageBuffer[] messageBuffers = Array.Empty<BgMessageBuffer>();
                int curBufIdx = -1, nextMessageIdx = 0;
                LastDrawStartPos = GetDrawStartPos();
                InternalContext.SetCursorVisible(false);
                int top = InternalContext.CurrentCursorTopPos, left = InternalContext.CurrentCursorLeftPos;
                for (int i = ViewBottom - 1; i >= ViewTop; --i) {
                    if (refreshMsgBuf) {
                        if (curBufIdx < 0) {
                            messageBuffers = GetBgMessageBuffer(RecentMessageHandler.GetRecentMessage(nextMessageIdx), LastDrawStartPos, PopupWidth);
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

            internal static void ClearSuggestionPopup(int linesAdded = 0) {
                int DisplaySuggestionsCnt = Math.Min(MaxSuggestions, Suggestions.Length); // Todo: head
                int drawStartPos = GetDrawStartPos();
                InternalContext.SetCursorVisible(false);
                int top = InternalContext.CurrentCursorTopPos, left = InternalContext.CurrentCursorLeftPos;
                Console.Write(ResetColorCode);
                for (int i = 0; i < DisplaySuggestionsCnt; ++i) {
                    if (linesAdded > 0 && i >= linesAdded && drawStartPos == LastDrawStartPos) break;
                    ClearSingleSuggestionPopup(BgBuffer[i], cursorTop: top - (DisplaySuggestionsCnt - i) - linesAdded);
                }
                Console.SetCursorPosition(left, top);
                InternalContext.SetCursorVisible(true);
            }

            internal static void RedrawOnArrowKey(int offset) {
                InternalContext.SetCursorVisible(false);
                int top = InternalContext.CurrentCursorTopPos, left = InternalContext.CurrentCursorLeftPos;

                int cursorTop = top - (ViewBottom - ChoosenIndex);
                DrawSingleSuggestionPopup(ChoosenIndex, BgBuffer[ChoosenIndex - ViewTop], cursorTop);
                DrawSingleSuggestionPopup(ChoosenIndex + offset, BgBuffer[ChoosenIndex - ViewTop + offset], cursorTop + offset);

                Console.SetCursorPosition(left, top);
                InternalContext.SetCursorVisible(true);
            }

            internal static void RedrawOnTab() {
                if (GetDrawStartPos() != LastDrawStartPos) {
                    ClearSuggestionPopup();
                    DrawSuggestionPopup(refreshMsgBuf: true);
                }
            }

            internal static void AddMessage(string message) {
                RecentMessageHandler.AddMessage(message);
            }

            private static int GetDrawStartPos() {
                return Math.Max(0, Math.Min(InternalContext.CursorLeftPosLimit - PopupWidth, StartIndex + ConsoleBuffer.PrefixTotalLength - ConsoleBuffer.BufferOutputAnchor));
            }

            private static void DrawSingleSuggestionPopup(int index, BgMessageBuffer buf, int cursorTop) {
                Console.SetCursorPosition(buf.CursorStart, cursorTop);

                Console.Write(ResetColorCode);
                if (buf.StartSpace)
                    Console.Write(' ');

                Console.Write(NormalBgColorCode);

                Console.Write(ArrowColorCode);
                if (index == ViewTop && ViewTop != 0)
                    Console.Write('↑');
                else if (index + 1 == ViewBottom && ViewBottom != Suggestions.Length)
                    Console.Write('↓');
                else if (index == ChoosenIndex)
                    Console.Write('>');
                else
                    Console.Write(' ');

                if (index == ChoosenIndex) {
                    if (HighlightColorCode != ArrowColorCode)
                        Console.Write(HighlightColorCode);
                    if (HighlightBgColorCode != NormalBgColorCode)
                        Console.Write(HighlightBgColorCode);
                    Console.Write(Suggestions[index].ShortText);
                    Console.Write(new string(' ', PopupWidth - 2 - Suggestions[index].ShortText.Length));
                    if (HighlightBgColorCode != NormalBgColorCode)
                        Console.Write(NormalBgColorCode);
                } else {
                    Console.Write(NormalColorCode);
                    Console.Write(Suggestions[index].ShortText);
                    Console.Write(new string(' ', PopupWidth - 2 - Suggestions[index].ShortText.Length));
                }

                Console.Write(ArrowColorCode);
                if (index == ViewTop && ViewTop != 0)
                    Console.Write('↑');
                else if (index + 1 == ViewBottom && ViewBottom != Suggestions.Length)
                    Console.Write('↓');
                else if (index == ChoosenIndex)
                    Console.Write('<');
                else
                    Console.Write(' ');

                Console.Write(ResetColorCode);
                if (buf.EndSpace)
                    Console.Write(' ');
            }

            private static void ClearSingleSuggestionPopup(BgMessageBuffer buf, int cursorTop) {
                Console.SetCursorPosition(buf.CursorStart, cursorTop);
                Console.Write(buf.Text);
                Console.Write(ResetColorCode);
                Console.Write(new string(' ', buf.AfterTextSpace));
            }

            private static BgMessageBuffer[] GetBgMessageBuffer(RecentMessageHandler.RecentMessage? msg, int start, int length) {
                if (msg == null)
                    return new BgMessageBuffer[1] { new BgMessageBuffer(start, start + length, length) };

                int charIndex = 0;
                char[] chars = msg.Message.ToCharArray();
                List<BgMessageBuffer> buffers = new();

                while (charIndex < chars.Length) {
                    int curIndex = 0;
                    int charStart, charEnd;

                    string Text = string.Empty;
                    bool StartSpace = false, EndSpace = false;
                    int CursorStart, CutsorEnd, AfterTextSpace = 0;

                    while (curIndex < start) {
                        if (charIndex < chars.Length) {
                            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(chars[charIndex]) == System.Globalization.UnicodeCategory.OtherLetter) {
                                if (curIndex + 2 > start) {
                                    StartSpace = true;
                                    break;
                                }
                                curIndex += 2;
                            } else {
                                curIndex += 1;
                            }
                            ++charIndex;
                        } else {
                            curIndex = start;
                            break;
                        }
                    }
                    charStart = charIndex;
                    CursorStart = curIndex;

                    int end = start + length;
                    while (curIndex < end) {
                        if (charIndex < chars.Length) {
                            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(chars[charIndex]) == System.Globalization.UnicodeCategory.OtherLetter)
                                curIndex += 2;
                            else
                                curIndex += 1;

                            if (curIndex > end)
                                EndSpace = true;

                            ++charIndex;
                        } else {
                            int last = end - curIndex;
                            curIndex += last;
                            AfterTextSpace += last;
                            break;
                        }
                    }
                    charEnd = charIndex;
                    CutsorEnd = curIndex;

                    if (charStart < charEnd) {
                        StringBuilder sb = new();

                        int bgIdx = GetLowerBoundColorCode(msg.ColorCodeBG, charStart);
                        while (bgIdx < msg.ColorCodeBG.Length && msg.ColorCodeBG[bgIdx].Item1 < charStart)
                            sb.Append(msg.ColorCodeBG[bgIdx++].Item2);

                        int fgIdx = GetLowerBoundColorCode(msg.ColorCodeFG, charStart);
                        while (fgIdx < msg.ColorCodeFG.Length && msg.ColorCodeFG[fgIdx].Item1 < charStart)
                            sb.Append(msg.ColorCodeFG[fgIdx++].Item2);

                        for (int i = charStart; i < charEnd; ++i) {
                            while (bgIdx < msg.ColorCodeBG.Length && msg.ColorCodeBG[bgIdx].Item1 == i)
                                sb.Append(msg.ColorCodeBG[bgIdx++].Item2);
                            while (fgIdx < msg.ColorCodeFG.Length && msg.ColorCodeFG[fgIdx].Item1 == i)
                                sb.Append(msg.ColorCodeFG[fgIdx++].Item2);
                            sb.Append(chars[i]);
                        }
                        Text = sb.ToString();
                    }

                    buffers.Add(new BgMessageBuffer(CursorStart, CutsorEnd, AfterTextSpace, Text, StartSpace, EndSpace));

                    while (curIndex < InternalContext.CursorLeftPosLimit) {
                        if (charIndex < chars.Length) {
                            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(chars[charIndex]) == System.Globalization.UnicodeCategory.OtherLetter) {
                                if (curIndex + 2 >= InternalContext.CursorLeftPosLimit)
                                    break;
                                curIndex += 2;
                            } else {
                                curIndex += 1;
                            }
                            ++charIndex;
                        } else {
                            break;
                        }
                    }
                }

                return buffers.ToArray();
            }

            private static int GetLowerBoundColorCode(Tuple<int, string>[] ColorCodes, int charStart) {
                --charStart;
                int left = 0, right = ColorCodes.Length - 1;
                while (left < right) {
                    int mid = (left + right) / 2;
                    if (ColorCodes[mid].Item1 >= charStart)
                        right = mid;
                    else
                        left = mid + 1;
                }
                return left;
            }

            private static class RecentMessageHandler {
                private static int Count = 0, Index = 0;

                private static readonly RecentMessage[] Messages = new RecentMessage[MaxSuggestions];

                internal static void AddMessage(string message) {
                    string[] lines = message.Split('\n');
                    int startIdx = MaxSuggestions * (lines.Length / MaxSuggestions);
                    for (int i = startIdx; i < lines.Length; i++) {
                        Index = Count < MaxSuggestions ? Count++ : (Index + 1) % MaxSuggestions;
                        Messages[Index] = new(lines[i]);
                    }
                }

                internal static RecentMessage? GetRecentMessage(int index) {
                    if (index >= Count)
                        return null;
                    RecentMessage message = Messages[(MaxSuggestions + Index - index) % MaxSuggestions];
                    message.ParseColorCode();
                    return message;
                }

                internal class RecentMessage {
                    private readonly static Regex ContralCharRegex = new(@"[\u0000-\u001F]", RegexOptions.Compiled);
                    private readonly static Regex EscapeCodeRegex = new(@"\u001B\[[\d;]+m", RegexOptions.Compiled);

                    private readonly static Regex Fg3bitColorCode = new(@"^\u001B\[(?:3|9)[01234567]m$", RegexOptions.Compiled);
                    private readonly static Regex Bg3bitColorCode = new(@"^\u001B\[(?:4|10)[01234567]m$", RegexOptions.Compiled);
                    private readonly static Regex Fg8bitColorCode = new(@"^\u001B\[38;5;(?:1\d{2}|2[0-4]\d|[1-9]?\d|25[0-5])m$", RegexOptions.Compiled);
                    private readonly static Regex Bg8bitColorCode = new(@"^\u001B\[48;5;(?:1\d{2}|2[0-4]\d|[1-9]?\d|25[0-5])m$", RegexOptions.Compiled);
                    private readonly static Regex Fg24bitColorCode = new(@"^\u001B\[38;2(?:;(?:1\d{2}|2[0-4]\d|[1-9]?\d|25[0-5])){3}m$", RegexOptions.Compiled);
                    private readonly static Regex Bg24bitColorCode = new(@"^\u001B\[48;2(?:;(?:1\d{2}|2[0-4]\d|[1-9]?\d|25[0-5])){3}m$", RegexOptions.Compiled);

                    bool Parsed = false;

                    public Tuple<int, string>[] ColorCodeFG = Array.Empty<Tuple<int, string>>();

                    public Tuple<int, string>[] ColorCodeBG = Array.Empty<Tuple<int, string>>();

                    public string Message = string.Empty, RawMessage = string.Empty;

                    public RecentMessage(string message) {
                        RawMessage = message;
                    }

                    public void ParseColorCode() {
                        if (Parsed)
                            return;

                        List<Tuple<int, string>> fgColor = new(), bgColor = new();
                        MatchCollection matchs = EscapeCodeRegex.Matches(RawMessage);
                        int colorLen = 0;
                        foreach (Match match in matchs) {
                            int index = match.Index - colorLen;
                            string code = match.Groups[0].Value;
                            if (code == ResetColorCode)
                                bgColor.Add(new(index, code));
                            else if (IsForcegroundColorCode(code))
                                fgColor.Add(new(index, code));
                            else if (IsBackgroundColorCode(code))
                                bgColor.Add(new(index, code));
                            colorLen += match.Length;
                        }

                        Message = ContralCharRegex.Replace(EscapeCodeRegex.Replace(RawMessage, string.Empty), string.Empty);
                        ColorCodeFG = fgColor.ToArray();
                        ColorCodeBG = bgColor.ToArray();

                        Parsed = true;
                    }

                    private static bool IsForcegroundColorCode(string code) {
                        if (Fg3bitColorCode.IsMatch(code))
                            return true;
                        if (Fg8bitColorCode.IsMatch(code))
                            return true;
                        if (Fg24bitColorCode.IsMatch(code))
                            return true;
                        return false;
                    }

                    private static bool IsBackgroundColorCode(string code) {
                        if (Bg3bitColorCode.IsMatch(code))
                            return true;
                        if (Bg8bitColorCode.IsMatch(code))
                            return true;
                        if (Bg24bitColorCode.IsMatch(code))
                            return true;
                        return false;
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
