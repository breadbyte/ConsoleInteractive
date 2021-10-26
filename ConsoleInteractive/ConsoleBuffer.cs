using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ConsoleInteractive {
    /*
     * The goal of this ConsoleBuffer is to create a non-interruptable, unlimited length user input buffer.
     * We use three variables to track the current input-
     * The CurrentBufferPos, which determines the current position in the StringBuffer the user is operating at
     * The ConsoleOutputBeginPos, which determines the starting position of the output in the StringBuffer
     * The ConsoleOutputLength, which determines the length of the string to be shown to the user.
     *
     * All these determine the following:
     *  abcdefg[hijklm|nopqrstuvwxyz]yxwvutsrqp - Input String
     *  0123456[78901234567890123456]7890123456 - Index Number
     *         ^      ^             ^
     *         |      |             |
     *         ConsoleOutputBeginPos|
     *                |             |
     *                CurrentBufferPos
     *                              |
     *                              ConsoleOutputLength
     * 
     * Given this example string, our variable states would be (zero-index):
     * CurrentBufferPos = 13
     * ConsoleOutputBeginPos = 6
     * ConsoleOutputLength = 19 (non-zero index)
     *
     * The contents of the square brackets will be shown to the user,
     * The I bar denominates the current input position,
     * The rest of the string remains untouched.
     *
     * Incrementing the ConsoleOutputBeginPos would move the output forward:
     * abcdefgh[ijklmnopqrstuvwxyzy]xwvutsrqp
     * - This requires us to check if we have hit the end of the string.
     * - The ConsoleOutputLength does not change, only the BeginPos.
     *
     * Decrementing the ConsoleOutputBeginPos would move the output backward:
     * abcdef[ghijklmnopqrstuvwxy]zyxwvutsrqp
     * - This requires us to check if we have hit the beginning of the string.
     * - The ConsoleOutputLength does not change, only the BeginPos.
     *
     * Incrementing the ConsoleOutputLength would move the farthest end of the output forward:
     * abcdefg[hijklmnopqrstuvwxyzy]xwvutsrqp
     * - This requires us to check if we have hit the end of the Console Width.
     * - Attempting to increment further than the Console Width does nothing. Increment the ConsoleOutputBeginPos instead.
     *
     * Decrementing the ConsoleOutputLength would move the farthest end of the output backward:
     * abcdefg[hijklmnopqrstuvwxy]zyxwvutsrqp
     * - Decrementing the ConsoleOutputLength is inadvisable.
     *
     */
    internal static class ConsoleBuffer {
        internal static StringBuilder UserInputBuffer = new();
        private static volatile int CurrentBufferPos = 0;
        private static volatile int ConsoleOutputBeginPos = 0;
        private static volatile int ConsoleOutputLength = 0;
        private static volatile int ConsoleWriteLimit = InternalContext.CursorLeftPosLimit - 1;
        
        /// <summary>
        /// Inserts a character in the user input buffer.
        /// </summary>
        /// <param name="c">The character to insert.</param>
        internal static void Insert(char c) {
            // Insert at the current buffer pos.
            UserInputBuffer.Insert(CurrentBufferPos, c);
            // Increment the buffer pos to reflect this change.
            Interlocked.Increment(ref CurrentBufferPos);

            // If we're inserting outside the buffer (buffer len is gt/eq to console horiz limit)
            if (InternalContext.CursorLeftPosLimit <= UserInputBuffer.Length) {
                Console.SetCursorPosition(0, InternalContext.CursorTopPos);
                
                if (InternalContext.CursorLeftPos == ConsoleWriteLimit)
                    Interlocked.Increment(ref ConsoleOutputBeginPos);

                RedrawInput();
            }
            else {
                Interlocked.Increment(ref ConsoleOutputLength);
                Console.Write(c);
            }

            // Increment the console cursor.
            InternalContext.IncrementLeftPos();
        }

        /// <summary>
        /// Redraws the current user input state.
        /// </summary>
        /// <param name="leftCursorPosition">The position the cursor was previously located.</param>
        internal static void RedrawInput() {
            lock (InternalContext.WriteLock) {
               // Console.CursorVisible = false;
                Console.SetCursorPosition(0, InternalContext.CursorTopPos);
                
                PrintDebugOperation("Redraw requested");
                
                // todo redundant? do we still need this check here
                if (UserInputBuffer.Length + 1 % ConsoleWriteLimit == 0 && ConsoleOutputBeginPos != 0)
                    Interlocked.Exchange(ref ConsoleOutputLength, UserInputBuffer.Length);

                Console.Write(UserInputBuffer.ToString().Substring(ConsoleOutputBeginPos, ConsoleOutputLength));

                Console.SetCursorPosition(InternalContext.CursorLeftPos, InternalContext.CursorTopPos);
                // Console.CursorVisible = true;
            }
        }

        /// <summary>
        /// Moves the input buffer forward by one char. Equivalent to pressing the right arrow key.
        /// </summary>
        internal static void MoveCursorForward() {
            // If we're at the end of the buffer, do nothing.
            if (CurrentBufferPos == UserInputBuffer.Length)
                return;
            
            Interlocked.Increment(ref CurrentBufferPos);
            
            // If we have extra buffer at the end
            if (InternalContext.CursorLeftPos == ConsoleWriteLimit && UserInputBuffer.Length > ConsoleWriteLimit) {
                Interlocked.Increment(ref ConsoleOutputBeginPos);
                RedrawInput();
            }
            
            InternalContext.IncrementLeftPos();
        }

        /// <summary>
        /// Moves the input buffer backward by one char. Equivalent to pressing the left arrow key.
        /// </summary>
        internal static void MoveCursorBackward() {
            // If we're at the beginning of the buffer, do nothing.
            if (CurrentBufferPos == 0)
                return;

            Interlocked.Decrement(ref CurrentBufferPos);

            // If we have extra buffer at the start
            if (InternalContext.CursorLeftPos == 0 && ConsoleOutputBeginPos != 0) {
                Interlocked.Decrement(ref ConsoleOutputBeginPos);
                RedrawInput();
            }

            InternalContext.DecrementLeftPos();
        }

        /// <summary>
        /// Removes a char from the buffer 'forwards', equivalent to pressing the Delete key.
        /// </summary>
        internal static void RemoveForward() {
            // If we're at the end of the buffer, do nothing.
            if (CurrentBufferPos >= UserInputBuffer.Length)
                return;
            
            PrintDebugOperation("RemoveForward was called.");
            
            // Remove 'forward', i.e. the delete button
            UserInputBuffer.Remove(CurrentBufferPos, 1);
            if (UserInputBuffer.Length - CurrentBufferPos != 0) {
                Interlocked.Decrement(ref ConsoleOutputLength);
                RedrawInput();

                RemoveTrailingLetter();
                PrintDebugOperation("RemoveForward was executed, special redraw necessary.");
                return;
            }
            else {
                Console.Write(' ');
                Interlocked.Decrement(ref ConsoleOutputLength);
            }

            RedrawInput();
            PrintDebugOperation("RemoveForward was executed.");
        }

        /// <summary>
        /// Removes a char from the buffer 'backwards', equivalent to pressing the Backspace key.
        /// </summary>
        internal static void RemoveBackward() {
            // If we're at the start of the buffer, do nothing.
            if (CurrentBufferPos == 0)
                return;

            PrintDebugOperation("RemoveBackward was called.");

            // Remove 'backward', i.e. backspace
            Interlocked.Decrement(ref CurrentBufferPos);
            UserInputBuffer.Remove(CurrentBufferPos, 1);

            //Interlocked.Decrement(ref ConsoleOutputLength);
            if (ConsoleOutputBeginPos != 0)
                Interlocked.Decrement(ref ConsoleOutputBeginPos);
            else {
                Interlocked.Exchange(ref ConsoleOutputLength, UserInputBuffer.Length);
                InternalContext.DecrementLeftPos();
                Console.Write(' ');
                Console.Write('\b');

                // We need to redraw because there's content in front of us.
                if (CurrentBufferPos < UserInputBuffer.Length) {
                    RedrawInput();
                    RemoveTrailingLetter();
                    PrintDebugOperation("RemoveBackward was executed, redraw necessary.");
                } else
                    PrintDebugOperation("RemoveBackward was executed, non-redraw.");
                
                return;
            }

            Console.Write('\b');
            Console.Write(' ');
            RedrawInput();

            PrintDebugOperation("RemoveBackward was executed.");
        }

        private static void PrintDebugOperation(string preMessage) {
            Debug.WriteLine(preMessage);
            Debug.WriteLine($"CBP {CurrentBufferPos} | OBP {ConsoleOutputBeginPos} | IBL {UserInputBuffer.Length} | Modulo {UserInputBuffer.Length % ConsoleWriteLimit} | ConPos {InternalContext.CursorLeftPos}");
        }

        private static void RemoveTrailingLetter() {
            if (UserInputBuffer.Length / ConsoleWriteLimit != 0) {
                Console.SetCursorPosition((InternalContext.CursorLeftPos + ((UserInputBuffer.Length - CurrentBufferPos) % ConsoleWriteLimit)), InternalContext.CursorTopPos);
            } else
                Console.SetCursorPosition(UserInputBuffer.Length, InternalContext.CursorTopPos);
            
            
            Console.Write(' ');
            Console.SetCursorPosition(InternalContext.CursorLeftPos, InternalContext.CursorTopPos);
        }

        /// <summary>
        /// Flushes the User Input Buffer.
        /// </summary>
        /// <returns>The string contained in the buffer.</returns>
        internal static string FlushBuffer() {
            ClearVisibleUserInput();
            Interlocked.Exchange(ref CurrentBufferPos, 0);
            Interlocked.Exchange(ref ConsoleOutputBeginPos, 0);
            Interlocked.Exchange(ref ConsoleOutputLength, 0);
            Interlocked.Exchange(ref InternalContext.CursorLeftPos, 0);
            var retval = UserInputBuffer.ToString();
            UserInputBuffer.Clear();
            return retval;
        }

        /// <summary>
        /// Clears the visible user input.
        /// Does not clear the internal buffer.
        /// </summary>
        internal static void ClearVisibleUserInput() {
            lock (InternalContext.WriteLock) {
                Console.SetCursorPosition(0, InternalContext.CursorTopPos);
                for (int i = 0; i <= ConsoleWriteLimit; i++) {
                    Console.Write(' ');
                }

                Console.SetCursorPosition(0, InternalContext.CursorTopPos);
                InternalContext.SetLeftCursorPosition(0);
            }
        }

        internal static void MoveToStartBufferPosition() {
            Interlocked.Exchange(ref CurrentBufferPos, 0);
            Interlocked.Exchange(ref ConsoleOutputBeginPos, 0);
            RedrawInput();
        }

        internal static void MoveToEndBufferPosition() {
            if (UserInputBuffer.Length <= InternalContext.CursorLeftPosLimit) {
                Interlocked.Exchange(ref CurrentBufferPos, UserInputBuffer.Length);
                Interlocked.Exchange(ref ConsoleOutputBeginPos, 0);
                Interlocked.Exchange(ref ConsoleOutputLength, UserInputBuffer.Length);
                RedrawInput();
                return;
            }

            var pos = UserInputBuffer.Length % InternalContext.CursorLeftPosLimit;
            Interlocked.Exchange(ref CurrentBufferPos, UserInputBuffer.Length);
            Interlocked.Exchange(ref ConsoleOutputBeginPos, pos + 1);
            RedrawInput();
        }
    }
}