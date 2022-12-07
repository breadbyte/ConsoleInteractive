using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConsoleInteractive {
    internal static class InternalContext {
        internal static object WriteLock = new();
        internal static Regex FormatRegex = new("(§[0-9a-fk-orw-z])((?:[^§]|§[^0-9a-fk-orw-z])*)", RegexOptions.Compiled);
        internal static volatile int CurrentCursorLeftPos = 0;
        internal static volatile int CurrentCursorTopPos = 0;
        internal static volatile int CursorLeftPosLimit = Console.BufferWidth;
        internal static volatile int CursorTopPosLimit = Console.BufferHeight;
        internal static volatile bool _suppressInput = false;
        internal static volatile bool BufferInitialized = false;

        internal static bool SuppressInput {
            get { return _suppressInput; }
            set {
                _suppressInput = value;
                if (value) {
                    ConsoleBuffer.ClearVisibleUserInput();
                }
                ConsoleBuffer.RedrawInput();
                ConsoleBuffer.MoveToEndBufferPosition();
            }
        }

        internal static void SetLeftCursorPosition(int leftPos) {
            if (CurrentCursorTopPos == CursorTopPosLimit) {
                Interlocked.Exchange(ref CurrentCursorTopPos, CursorTopPosLimit - 1);
            }

            Console.SetCursorPosition(leftPos, CurrentCursorTopPos);
            Interlocked.Exchange(ref CurrentCursorLeftPos, leftPos);
        }

        internal static void SetTopCursorPosition(int topPos) {
            if (CurrentCursorTopPos == CursorTopPosLimit) {
                Interlocked.Exchange(ref CurrentCursorTopPos, CursorTopPosLimit - 1);
            }

            Console.SetCursorPosition(CurrentCursorLeftPos, topPos);
            Interlocked.Exchange(ref CurrentCursorTopPos, topPos);
        }

        internal static void SetCursorPosition(int leftPos, int topPos) {
            SetLeftCursorPosition(leftPos);
            SetTopCursorPosition(topPos);
        }

        internal static void ResetCursorPosition() {
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }

        internal static void SetCursorVisible(bool visible) {

            // It's useful to have the cursor visible in debug situations
#if DEBUG
            return;
#endif

            Console.CursorVisible = visible;
        }

        internal static void IncrementLeftPos(int amount = 1) {
            if (CursorLeftPosLimit <= CurrentCursorLeftPos + 1)
                return;
            CurrentCursorLeftPos = Interlocked.Add(ref CurrentCursorLeftPos, amount);
            if (SuppressInput) return;
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }

        internal static void DecrementLeftPos(int amount = 1) {
            if (CurrentCursorLeftPos == 0)
                return;
            CurrentCursorLeftPos = Interlocked.Add(ref CurrentCursorLeftPos, -amount);
            if (SuppressInput) return;
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }

        internal static void IncrementTopPos() {
            if (CursorTopPosLimit <= CurrentCursorTopPos + 1)
                return;
            CurrentCursorTopPos = Interlocked.Increment(ref CurrentCursorTopPos);
            if (SuppressInput) return;
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
    }
}