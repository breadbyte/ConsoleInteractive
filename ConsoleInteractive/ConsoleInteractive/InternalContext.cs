using System;
using System.Text.RegularExpressions;

namespace ConsoleInteractive {
    internal static partial class InternalContext {
        internal static readonly object WriterLock = new();
        internal static readonly object ReaderThreadLock = new();
        internal static readonly object HistoryLock = new();
        internal static readonly object SuggestionLock = new();
        
        internal static object WriteLock = new();
        internal static object BackreadBufferLock = new();

        internal readonly static Regex VT100CodeRegex = GetVT100CodeRegex();
        internal readonly static Regex FormatRegex = GetFormatRegex();

        internal static volatile bool _suppressInput = false;
        internal static volatile bool BufferInitialized = false;
        internal static volatile bool DirtyBuffer = true;

        internal static bool SuppressInput {
            get { return _suppressInput; }
            set {
                _suppressInput = value;
                if (value) {
                    ConsoleBuffer.ClearVisibleUserInput();
                }
                ConsoleBuffer.RedrawInputArea();
                ConsoleBuffer.MoveToEndBufferPosition();
            }
        }

        internal static void SetCursorVisible(bool visible) {
            // It's useful to have the cursor visible in debug situations
            #if DEBUG
                return;
            #endif

            Console.CursorVisible = visible;
        }

        [GeneratedRegex("\\u001B\\[[\\d;]+m", RegexOptions.Compiled)]
        private static partial Regex GetVT100CodeRegex();

        [GeneratedRegex("(§([0-9a-fk-or]|(?:§[0-9a-fr])))", RegexOptions.Compiled)]
        private static partial Regex GetFormatRegex();
    }
}