using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleInteractive {
    internal static class InternalContext {
        internal static object WriteLock = new();
        internal static StringBuilder UserInputBuffer = new();
        internal static Regex FormatRegex = new Regex("(§[0-9a-fk-or])((?:[^§]|§[^0-9a-fk-or])*)", RegexOptions.Compiled);

        /// <summary>
        /// Clears the visible user input but does not clear the internal buffer.
        /// Sets the console cursor to 0 afterwards.
        /// </summary>
        internal static void ClearVisibleUserInput() {
            lock (WriteLock) {
                Console.SetCursorPosition(0, Console.CursorTop);
                for (int i = 0; i <= UserInputBuffer.Length; i++) {
                    Console.Write(' ');
                }

                Console.SetCursorPosition(0, Console.CursorTop);
            }
        }
    }
}