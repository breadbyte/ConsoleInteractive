using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleInteractive {
    public static class ConsoleWriter {
        public static void WriteLine(object? value) {
            InternalWriter.WriteLine(value);
        }

    }

    internal static class InternalWriter {
        private static void Write(object? value) {
            lock (InternalContext.WriteLock) {
                var cursorLeftPrevious = Console.CursorLeft;

                InternalContext.ClearVisibleUserInput();
                Console.Write(value);
                Console.SetCursorPosition(0, Console.CursorTop += 1);
                Console.Write(InternalContext.UserInputBuffer);
                Console.SetCursorPosition(cursorLeftPrevious, Console.CursorTop);
            }
        }

        public static void WriteLine(object? value) {
            Write(value);
        }
    }
}