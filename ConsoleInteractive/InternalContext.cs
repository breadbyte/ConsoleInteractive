using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConsoleInteractive {
    internal static class InternalContext {
        internal static object WriteLock = new();
        internal static StringBuilder UserInputBuffer = new();
        internal static Regex FormatRegex = new Regex("(§[0-9a-fk-or])((?:[^§]|§[^0-9a-fk-or])*)", RegexOptions.Compiled);
        internal static volatile int CursorLeftPos = 0;
        internal static volatile int CursorTopPos = 0;
        internal static volatile int CursorLeftPosLimit = Console.BufferWidth;
        internal static volatile int CursorTopPosLimit = Console.BufferHeight;

        /// <summary>
        /// Clears the visible user input but does not clear the internal buffer.
        /// Sets the console cursor to 0 afterwards.
        /// </summary>
        internal static void ClearVisibleUserInput() {
            lock (WriteLock) {
                Console.SetCursorPosition(0, CursorTopPos);
                for (int i = 0; i <= UserInputBuffer.Length; i++) {
                    Console.Write(' ');
                }

                Console.SetCursorPosition(0, CursorTopPos);
                Interlocked.Exchange(ref CursorLeftPos, 0);
            }
        }

        internal static void IncrementLeftPos() {
            if (CursorLeftPosLimit <= CursorLeftPos + 1)
                return;
            CursorLeftPos = Interlocked.Increment(ref CursorLeftPos);
        }

        internal static void DecrementLeftPos() {
            if (0 >= CursorLeftPos - 1)
                return;
            CursorLeftPos = Interlocked.Decrement(ref CursorLeftPos);

        }

        internal static void IncrementTopPos() {
            if (CursorTopPosLimit <= CursorTopPos + 1)
                return;
            CursorTopPos = Interlocked.Increment(ref CursorTopPos);
        }
    }
}