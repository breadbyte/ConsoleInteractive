using System;
using System.Text;
using System.Threading;

namespace ConsoleInteractive {
    internal static class ConsoleBuffer {
        internal static StringBuilder UserInputBuffer = new();
        internal static volatile int CurrentInputPos = 0;
        internal static volatile int ConsoleToBufferPos = 0;
        private static volatile int ConsoleWriteLimit = InternalContext.CursorLeftPosLimit - 1;

        internal static void AppendToBuffer(char c) {
            UserInputBuffer.Insert(CurrentInputPos, c);
            InternalContext.IncrementLeftPos();
            MoveCursorForward(InternalContext.CursorLeftPos);
        }

        internal static void AddAtPosition(int pos, char c) {
            
        }

        internal static void RedrawInput(int leftCursorPosition) {
            Console.CursorVisible = false;
            int length;
            
            if (InternalContext.CursorLeftPosLimit > UserInputBuffer.Length)
                length = UserInputBuffer.Length;
            else length = InternalContext.CursorLeftPosLimit - 1; 
                
            Console.Write(UserInputBuffer.ToString()[^length..]);
            InternalContext.SetLeftCursorPosition(leftCursorPosition);
            Console.CursorVisible = true;
        }

        internal static void MoveCursorForward(int currentCursorPosition) {
            CurrentInputPos = Interlocked.Increment(ref CurrentInputPos);
            InternalContext.IncrementLeftPos();
            DetermineCurrentInputPos();
            RedrawInput(currentCursorPosition);
        }

        internal static void MoveCursorBackward(int currentCursorPosition) {
            CurrentInputPos = Interlocked.Decrement(ref CurrentInputPos);
            InternalContext.DecrementLeftPos();
            DetermineCurrentInputPos();
            RedrawInput(currentCursorPosition);
        }
        
        private static void DetermineCurrentInputPos() {
            if (CurrentInputPos < ConsoleWriteLimit)
                ConsoleToBufferPos = 0;
            if (CurrentInputPos >= ConsoleWriteLimit)
                ConsoleToBufferPos = ConsoleToBufferPos + ConsoleWriteLimit;
        }

        internal static string FlushBuffer() {
            ClearVisibleUserInput();
            Interlocked.Exchange(ref CurrentInputPos, 0);
            Interlocked.Exchange(ref ConsoleToBufferPos, 0);
            var retval = UserInputBuffer.ToString();
            UserInputBuffer.Clear();
            return retval;
        }

        /// <summary>
        /// Clears the visible user input but does not clear the internal buffer.
        /// </summary>
        internal static void ClearVisibleUserInput() {
            lock (InternalContext.WriteLock) {
                Console.SetCursorPosition(0, InternalContext.CursorTopPos);
                for (int i = 0; i <= ConsoleWriteLimit; i++) {
                    Console.Write(' ');
                }

                Console.SetCursorPosition(0, InternalContext.CursorTopPos);
                Interlocked.Exchange(ref InternalContext.CursorLeftPos, 0);
            }
        }
    }
}