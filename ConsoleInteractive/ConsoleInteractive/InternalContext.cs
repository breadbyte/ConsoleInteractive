﻿using System;
using System.Text.RegularExpressions;

namespace ConsoleInteractive {
    internal static class InternalContext {
        internal static object WriteLock = new();
        internal static object UserInputBufferLock = new();
        internal static object BackreadBufferLock = new();

        internal readonly static Regex VT100CodeRegex = new(@"\u001B\[[\d;]+m", RegexOptions.Compiled);
        internal readonly static Regex FormatRegex = new("(§([0-9a-fk-or]|(?:§[0-9a-fr])))", RegexOptions.Compiled);

        internal static volatile bool _suppressInput = false;
        internal static volatile bool BufferInitialized = false;

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
    }
}