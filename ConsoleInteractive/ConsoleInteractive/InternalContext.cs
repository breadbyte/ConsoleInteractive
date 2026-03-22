using System;
using System.Text.RegularExpressions;

namespace ConsoleInteractive {
    internal static partial class InternalContext {
        internal static object WriteLock = new();
        internal static object UserInputBufferLock = new();
        internal static object BackreadBufferLock = new();

        internal readonly static Regex VT100CodeRegex = GetVT100CodeRegex();
        internal readonly static Regex FormatRegex = GetFormatRegex();

        internal static volatile bool _suppressInput = false;
        internal static volatile bool BufferInitialized = false;

        internal static readonly bool IsInteractiveConsole = DetectInteractiveConsole();

        private static bool DetectInteractiveConsole() {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
                return false;
            try {
                _ = Console.BufferWidth;
                _ = Console.KeyAvailable;
                return true;
            } catch {
                return false;
            }
        }

        internal static int SafeBufferWidth {
            get {
                if (!IsInteractiveConsole) return 120;
                try { return Console.BufferWidth; }
                catch { return 120; }
            }
        }

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
            if (!IsInteractiveConsole) return;

            // It's useful to have the cursor visible in debug situations
            #if !DEBUG
            try { Console.CursorVisible = visible; } catch { }
            #endif
        }

        [GeneratedRegex("\\u001B\\[[\\d;]+m", RegexOptions.Compiled)]
        private static partial Regex GetVT100CodeRegex();

        [GeneratedRegex("(§([0-9a-fk-or]|(?:§[0-9a-fr])))", RegexOptions.Compiled)]
        private static partial Regex GetFormatRegex();
    }
}
