using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ConsoleInteractive {
    /*
     * The goal of this ConsoleBuffer is to create a non-interruptable, unlimited length user input buffer.
     * We use the following variables to track the current input-
     * The CurrentBufferPos, which controls the current position in the UserInputBuffer the user is operating at
     * The ConsoleOutputBeginPos, which determines the starting position of the output in the UserInputBuffer
     * The ConsoleOutputLength, which controls the length of the string to be shown to the user.
     *
     * The buffer works accordingly:
     * 
     * [> abcdefghijklmnopqrstuvwxyz] - PresentBuffer
     *   [                          ] - UserInputBuffer
     *
     * The PresentBuffer and UserInputBuffer work on separate character counters and buffers.
     * The UserInputBuffer will start at 0, while PresentBuffer starts at 2.
     * For every key inputted, the UserInputBuffer and PresentBuffer will both increment by 1.
     * If the PresentBuffer is at the edge of the terminal, the UserInputBuffer should start moving.
     *
     * [< cdefghijklmnopqrstuvwxyz12] - PresentBuffer
     * ab[cdefghijklmnopqrstuvwxyz12] - UserInputBuffer
     *
     * The Prefix will change to '<' if the buffer is wider than the window, but it will be '>' otherwise.
     *
     * The ConsoleOutputBeginPos and ConsoleOutputLength work as follows:
     *
     * Given a UserInputBuffer with the following input string:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     * 
     * Incrementing the ConsoleOutputBeginPos would move the output forward:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     *        ->
     * abcdefgh[ijklmnopqrstuvwxyzy]xwvutsrqp
     * - This requires us to check if we have hit the end of the string.
     * - The ConsoleOutputLength does not change, only the BeginPos.
     *
     * Decrementing the ConsoleOutputBeginPos would move the output backward:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     *       <-
     * abcdef[ghijklmnopqrstuvwxy]zyxwvutsrqp
     * - This requires us to check if we have hit the beginning of the string.
     * - The ConsoleOutputLength does not change, only the BeginPos.
     *
     * Incrementing the ConsoleOutputLength would move the farthest end of the output forward:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     *                           ->
     * abcdefg[hijklmnopqrstuvwxyzy]xwvutsrqp
     * - This requires us to check if we have hit the end of the Console.
     * - Attempting to increment further than the Console Width does nothing.
     *
     * Decrementing the ConsoleOutputLength would move the farthest end of the output backward:
     * abcdefg[hijklmnopqrstuvwxyz]yxwvutsrqp
     *                            <-
     * abcdefg[hijklmnopqrstuvwxy]zyxwvutsrqp
     * - This requires us to check if we have hit the beginning of the Console.
     * - Attempting to decrement further than the Console Width does nothing.
     *
     */
    internal static class ConsoleBuffer {
        internal static StringBuilder UserInputBuffer = new();
        internal static StringBuilder UserInputBufferCopy = new();
        
        private static string Prefix = "> ";
        internal static int PrefixLength = 2;
        internal static StringBuilder PresentBuffer = new(Prefix);
        
        internal static volatile int CurrentBufferPos = 0;
        private static volatile int ConsoleOutputBeginPos = 1;
        private static volatile int ConsoleOutputLength = 0;
        
        private static volatile int PresentBufferMaxLength = InternalContext.CursorLeftPosLimit - 1;
        private static volatile int UserInputBufferMaxLength = PresentBufferMaxLength - PrefixLength;

        internal static void PrintDebug([CallerMemberName] string caller = "") {
            Debug.WriteLine($"----\n" +
                            $"Called from: {caller}\n" +
                            $"ConsoleCurrentLeftPos {InternalContext.CurrentCursorLeftPos}\n" +
                            $"CurrentBufferPos: {CurrentBufferPos}\n" +
                            $"ConsoleOutputBeginPos: {ConsoleOutputBeginPos}\n" +
                            $"ConsoleOutputLength: {ConsoleOutputLength}\n" +
                            $"CursorLeftPosLimit: {InternalContext.CursorLeftPosLimit}\n" +
                            $"----\n");
        }

        internal static void Init() {
            Console.Write(PresentBuffer);
            Interlocked.Exchange(ref InternalContext.CurrentCursorLeftPos, PrefixLength);
            PrintDebug();
        }

        /// <summary>
        /// Inserts a character in the user input buffer.
        /// </summary>
        /// <param name="c">The character to insert.</param>
        internal static void Insert(char c) {
            // Insert at the current buffer pos.
            UserInputBuffer.Insert(CurrentBufferPos, c);
            Interlocked.Increment(ref CurrentBufferPos);

            // If we're inserting outside the buffer (buffer len is gt/eq to console width)
            if (PresentBufferMaxLength <= UserInputBuffer.Length + 1) {
                
                // If we're not at the end of the string, move the output forward.
                if (ConsoleOutputLength != UserInputBufferMaxLength)
                    Interlocked.Increment(ref ConsoleOutputLength);
                
                Console.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);
                
                // If we're at the end of the console, increment ConsoleOutputBeginPos.
                if (InternalContext.CurrentCursorLeftPos == PresentBufferMaxLength)
                    Interlocked.Increment(ref ConsoleOutputBeginPos);

                RedrawInput();
            }
            // If we're inserting inside the buffer
            else {
                Interlocked.Increment(ref ConsoleOutputLength);
                
                // Don't redraw if we don't need to
                if (!InternalContext.SuppressInput)
                    Console.Write(c);

                if (CurrentBufferPos < UserInputBuffer.Length) {
                    RedrawInput();
                }
            }

            if (InternalContext.SuppressInput) return;

            // Increment the console cursor.
            InternalContext.IncrementLeftPos();
            PrintDebug();
        }
        
        internal static void SetBufferContent(string content) {
            FlushBuffer();
            UserInputBuffer.Append(content);
            MoveToEndBufferPosition();
            RedrawInput();
        }

        /// <summary>
        /// Redraws the current user input state.
        /// </summary>
        /// <param name="leftCursorPosition">The position the cursor was previously located.</param>
        internal static void RedrawInput() {
            lock (InternalContext.WriteLock) {
                if (InternalContext.SuppressInput) return;
                
                // Don't redraw if we don't have anything to redraw.
                if (UserInputBuffer.Length == 0)
                    return;

                Console.CursorVisible = false;
                Console.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);

                Debug.Assert(ConsoleOutputLength != 0);
                
                PrintDebug();

                RedrawPresentBuffer();
                Console.Write(PresentBuffer);
                Console.SetCursorPosition(InternalContext.CurrentCursorLeftPos, InternalContext.CurrentCursorTopPos);

                Console.CursorVisible = true;
            }
        }

        private static void RedrawPresentBuffer() {
            Prefix = ConsoleOutputBeginPos > 0 ? "< " : "> ";
            PresentBuffer.Clear();
            PresentBuffer.Append($"{Prefix}{UserInputBuffer.ToString(ConsoleOutputBeginPos, ConsoleOutputLength - 1)}");
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
            if (InternalContext.CurrentCursorLeftPos == UserInputBufferMaxLength && UserInputBuffer.Length > UserInputBufferMaxLength) {
                Interlocked.Increment(ref ConsoleOutputBeginPos);
                RedrawInput();
            }
            
            if (InternalContext.SuppressInput) return;
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
            if (InternalContext.CurrentCursorLeftPos == 0 && ConsoleOutputBeginPos != 0) {
                Interlocked.Decrement(ref ConsoleOutputBeginPos);

                if (ConsoleOutputLength != UserInputBuffer.Length && ConsoleOutputLength < UserInputBufferMaxLength)
                    Interlocked.Increment(ref ConsoleOutputLength);
                
                RedrawInput();
            }
            
            if (InternalContext.SuppressInput) return;
            InternalContext.DecrementLeftPos();
        }

        /// <summary>
        /// Removes a char from the buffer 'forwards', equivalent to pressing the Delete key.
        /// </summary>
        internal static void RemoveForward() {
            // If we're at the end of the buffer, do nothing.
            if (CurrentBufferPos >= UserInputBuffer.Length || InternalContext.CurrentCursorLeftPos == UserInputBufferMaxLength)
                return;

            UserInputBuffer.Remove(CurrentBufferPos, 1);
            RedrawWithTrailingCheck();
        }

        /// <summary>
        /// Removes a char from the buffer 'backwards', equivalent to pressing the Backspace key.
        /// </summary>
        internal static void RemoveBackward() {
            // If we're at the start of the buffer, do nothing.
            if (CurrentBufferPos == 0 || InternalContext.CurrentCursorLeftPos == 0 && !InternalContext._suppressInput)
                return;

            // Remove 'backward', i.e. backspace
            Interlocked.Decrement(ref CurrentBufferPos);
            UserInputBuffer.Remove(CurrentBufferPos, 1);
            
            if (!InternalContext.SuppressInput)
                InternalContext.DecrementLeftPos();
            
            if (CurrentBufferPos == UserInputBuffer.Length) {
                if (!InternalContext.SuppressInput) {
                    Console.Write(' ');
                    Console.Write('\b');
                }

                Interlocked.Decrement(ref ConsoleOutputLength);
            } else
                RedrawWithTrailingCheck();
        }

        private static void RedrawWithTrailingCheck() {
            if (InternalContext.SuppressInput) return;            
            
            bool isBufferShorterThanWriteLimit = (UserInputBuffer.Length - CurrentBufferPos) + InternalContext.CurrentCursorLeftPos < UserInputBufferMaxLength;
            int cEndPos = GetConsoleEndPosition();
            
            // If the buffer isn't longer than the write limit
            if (cEndPos < UserInputBufferMaxLength && isBufferShorterThanWriteLimit) {
                Interlocked.Decrement(ref ConsoleOutputLength); // Shorten the length so we don't get an OutOfBoundsException.
                RemoveTrailingLetter();
                RedrawInput();
            }
            
            // Redraw by default.
            else
                RedrawInput();
        }

        // Magic math I came up with.
        // CursorPosition + (BufferLength - BufferPosition) % ConsoleWidth
        // 1. [BufferLength - BufferPosition]
        // The buffer length is subtracted from the buffer position to get the length after the buffer position.
        //                  If my buffer length was here  ^
        //      I would get the string length from here ->
        //
        // 2. [.. % ConsoleWidth]
        // We get the modulo of this length with the width of the console.
        // This allows us to trim the length further, with one that aligns to the length of the console.
        //
        // 3. [CursorPosition + ..]
        // We finally add the position of the console to align the actual cursor position, as the modulo of the width 
        // does not necessarily fall into the correct position, as it is aligned to the console width. 
        private static int GetConsoleEndPosition() => InternalContext.CurrentCursorLeftPos + (UserInputBuffer.Length - CurrentBufferPos) % UserInputBufferMaxLength;

        private static void RemoveTrailingLetter() {
            Console.CursorVisible = false;

            if (ConsoleOutputBeginPos != 0) {
                Console.SetCursorPosition(GetConsoleEndPosition(), InternalContext.CurrentCursorTopPos);
            } else
                Console.SetCursorPosition(UserInputBuffer.Length, InternalContext.CurrentCursorTopPos);
            
            
            Console.Write(' ');
            Console.SetCursorPosition(InternalContext.CurrentCursorLeftPos, InternalContext.CurrentCursorTopPos);
            
            Console.CursorVisible = true;
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
            var retval = UserInputBuffer.ToString();
            UserInputBuffer.Clear();
            
            PresentBuffer.Clear();
            PresentBuffer.Append(Prefix);
            Interlocked.Exchange(ref InternalContext.CurrentCursorLeftPos, PrefixLength);
            
            Console.CursorVisible = false;
            Console.Write(PresentBuffer);
            Console.CursorVisible = true;
            Debug.WriteLine("Flushed buffer with string: " + retval);
            return retval;
        }

        /// <summary>
        /// Clears the visible user input.
        /// Does not clear the internal buffer.
        /// </summary>
        internal static void ClearVisibleUserInput() {
            lock (InternalContext.WriteLock) {
                Console.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);

                bool requireCompleteClear = UserInputBuffer.Length > UserInputBufferMaxLength;

                for (int i = 0; i <= (requireCompleteClear ? UserInputBufferMaxLength : UserInputBuffer.Length + 1); i++) {
                    Console.Write(' ');
                }

                Console.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);
                InternalContext.SetLeftCursorPosition(0);
            }
        }

        internal static void MoveToStartBufferPosition() {
            Interlocked.Exchange(ref CurrentBufferPos, 0);
            Interlocked.Exchange(ref ConsoleOutputBeginPos, 0);
            if (UserInputBuffer.Length < UserInputBufferMaxLength)
                Interlocked.Exchange(ref ConsoleOutputLength, UserInputBuffer.Length);
            else
                Interlocked.Exchange(ref ConsoleOutputLength, UserInputBufferMaxLength);


            RedrawInput();
            InternalContext.SetLeftCursorPosition(0);
        }

        internal static void MoveToEndBufferPosition() {
            if (UserInputBuffer.Length < InternalContext.CursorLeftPosLimit) {
                Interlocked.Exchange(ref CurrentBufferPos, UserInputBuffer.Length);
                Interlocked.Exchange(ref ConsoleOutputBeginPos, 0);
                Interlocked.Exchange(ref ConsoleOutputLength, UserInputBuffer.Length);
                InternalContext.SetLeftCursorPosition(UserInputBuffer.Length);
                return;
            }
            
            InternalContext.SetLeftCursorPosition(UserInputBufferMaxLength);
            Interlocked.Exchange(ref ConsoleOutputLength, UserInputBufferMaxLength);
            Interlocked.Exchange(ref ConsoleOutputBeginPos, UserInputBuffer.Length - UserInputBufferMaxLength);
            Interlocked.Exchange(ref CurrentBufferPos, UserInputBuffer.Length);
            RedrawInput();
        }
        
        
        internal static volatile int BufferBackreadPos = -1;
        private const int BackreadBufferLimit = 8;
        internal static List<string> BackreadBuffer = new List<string>(BackreadBufferLimit);
        
        public static void IncrementBackreadPos() {
            Interlocked.Increment(ref BufferBackreadPos);
            
            if (BufferBackreadPos >= BackreadBuffer.Count)
                Interlocked.Exchange(ref BufferBackreadPos, 0);
        }
        
        private static void DecrementBackreadPos() {
            if (BufferBackreadPos == -1) return;
            Interlocked.Decrement(ref BufferBackreadPos);
        }
        
        public static void ClearBackreadBuffer() {
            BackreadBuffer.Clear();
            UserInputBufferCopy.Clear();
            BufferBackreadPos = -1;
        }

        /// <summary>
        /// Moves the backread backwards. Equivalent to pressing the Up arrow.
        /// </summary>
        /// <returns>The string in the backread buffer.</returns>
        public static string GetBackreadBackwards() {
            if (BackreadBuffer.Count == 0) return UserInputBuffer.ToString();
            
            if (BufferBackreadPos == -1)
                UserInputBufferCopy = new StringBuilder(UserInputBuffer.ToString());
            
            IncrementBackreadPos();
            return BackreadBuffer[BufferBackreadPos];
        }
        
        /// <summary>
        /// Moves the backread forward. Equivalent to pressing the Down arrow.
        /// </summary>
        /// <returns>The string in the backread buffer.</returns>
        public static string GetBackreadForwards() {
            if (BufferBackreadPos is -1 or 0) {
                if (BufferBackreadPos == 0)
                    DecrementBackreadPos();
                
                return UserInputBufferCopy.ToString();
            }

            DecrementBackreadPos();
            return BackreadBuffer[BufferBackreadPos];
        }
        
        /// <summary>
        /// Adds a string to the backread buffer.
        /// </summary>
        /// <param name="message">The string to be added.</param>
        public static void AddToBackreadBuffer(string message) {
            BackreadBuffer.Add(message);
            if (BackreadBuffer.Count > BackreadBufferLimit)
                BackreadBuffer.RemoveAt(0);
        }

    }
}