using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

[assembly: InternalsVisibleTo("ConsoleBufferTest")]
namespace ConsoleInteractive {
    internal static class InternalContext {
        internal static object WriteLock = new();
        internal static Regex FormatRegex = new Regex("(§[0-9a-fk-or])((?:[^§]|§[^0-9a-fk-or])*)", RegexOptions.Compiled);
        internal static volatile int CurrentCursorLeftPos = 0;
        internal static volatile int CurrentCursorTopPos = 0;
        internal static volatile int CursorLeftPosLimit = Console.BufferWidth;
        internal static volatile int CursorTopPosLimit = Console.BufferHeight;
        internal static volatile bool _suppressInput = false;
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

        internal static void IncrementLeftPos() {
            if (CursorLeftPosLimit <= CurrentCursorLeftPos + 1)
                return;
            CurrentCursorLeftPos = Interlocked.Increment(ref CurrentCursorLeftPos);
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
        
        internal static void DecrementLeftPos() {
            if (CurrentCursorLeftPos == 0)
                return;
            CurrentCursorLeftPos = Interlocked.Decrement(ref CurrentCursorLeftPos);
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
        
        internal static void IncrementTopPos() {
            if (CursorTopPosLimit <= CurrentCursorTopPos + 1)
                return;
            CurrentCursorTopPos = Interlocked.Increment(ref CurrentCursorTopPos);
            Console.SetCursorPosition(CurrentCursorLeftPos, CurrentCursorTopPos);
        }
    }
}