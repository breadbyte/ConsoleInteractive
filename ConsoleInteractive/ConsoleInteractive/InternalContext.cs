using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConsoleInteractive {
    internal static class InternalContext {
        internal static object WriteLock = new();
        internal static Regex FormatRegex = new Regex("(§[0-9a-fk-or])((?:[^§]|§[^0-9a-fk-or])*)", RegexOptions.Compiled);
        internal static volatile int CurrentCursorLeftPos = 0;
        internal static volatile int CurrentCursorTopPos = 0;
        internal static volatile int CursorLeftPosLimit = Console.BufferWidth;
        internal static volatile int CursorTopPosLimit = Console.BufferHeight;
        internal static volatile bool _suppressInput = false;
        internal static volatile bool BufferInitialized = false;
        internal static volatile bool IsInitialized = false;
        internal static volatile bool IsUsingSystemConsole = false;

        internal static bool SuppressInput {
            get { return _suppressInput; }
            set {
                _suppressInput = value;
                ThrowIfUsingSystemConsole();
                if (value) {
                    ConsoleBuffer.ClearVisibleUserInput();
                }
                ConsoleBuffer.RedrawInput();
                ConsoleBuffer.MoveToEndBufferPosition();
            }
        }
        
        internal static void ThrowIfUsingSystemConsole() {
            if (IsUsingSystemConsole)
                throw new InvalidOperationException("Cannot use called function in the current console context.");
        }

        internal static void CheckIfContextIsInitialized() {
            if (!InternalContext.IsInitialized) {
                throw new InvalidOperationException("ConsoleWriter.Init() must be called at least once in the application lifetime.");
            }
        }
        
        internal static void SetLeftCursorPosition(int leftPos) {
            ThrowIfUsingSystemConsole();

            if (CurrentCursorTopPos == CursorTopPosLimit) {
                Interlocked.Exchange(ref CurrentCursorTopPos, CursorTopPosLimit - 1);
            }
            
            Console.SetCursorPosition(leftPos, CurrentCursorTopPos);
            Interlocked.Exchange(ref CurrentCursorLeftPos, leftPos);
        }

        internal static void SetTopCursorPosition(int topPos) {
            ThrowIfUsingSystemConsole();

            if (CurrentCursorTopPos == CursorTopPosLimit) {
                Interlocked.Exchange(ref CurrentCursorTopPos, CursorTopPosLimit - 1);
            }
            
            Console.SetCursorPosition(CurrentCursorLeftPos, topPos);
            Interlocked.Exchange(ref CurrentCursorTopPos, topPos);
        }

        internal static void SetCursorPosition(int leftPos, int topPos) {
            ThrowIfUsingSystemConsole();

            SetLeftCursorPosition(leftPos);
            SetTopCursorPosition(topPos);
        }
        
        internal static void SetCursorVisible(bool visible) {
            ThrowIfUsingSystemConsole();

            // It's useful to have the cursor visible in debug situations
            #if DEBUG
                return;
            #endif
            
            Console.CursorVisible = visible;
        }

        internal static void IncrementLeftPos() {
            ThrowIfUsingSystemConsole();

            if (CursorLeftPosLimit <= CurrentCursorLeftPos + 1)
                return;
            CurrentCursorLeftPos = Interlocked.Increment(ref CurrentCursorLeftPos);
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
        
        internal static void DecrementLeftPos() {
            ThrowIfUsingSystemConsole();

            if (CurrentCursorLeftPos == 0)
                return;
            CurrentCursorLeftPos = Interlocked.Decrement(ref CurrentCursorLeftPos);
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
        
        internal static void IncrementTopPos() {
            ThrowIfUsingSystemConsole();

            if (CursorTopPosLimit <= CurrentCursorTopPos + 1)
                return;
            CurrentCursorTopPos = Interlocked.Increment(ref CurrentCursorTopPos);
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
    }
}