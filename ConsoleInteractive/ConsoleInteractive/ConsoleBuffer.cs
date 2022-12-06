using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// The prefix string.
        /// </summary>
        private const string Prefix = ">";

        /// <summary>
        /// The prefix string, reversed.
        /// </summary>
        private const string PrefixOpposite = "<";

        /// <summary>
        /// The amount of spaces to add after the prefix.
        /// </summary>
        private const int PrefixSpaces = 1;

        /// <summary>
        /// The total length of the prefix, including the spaces.
        /// </summary>
        internal static volatile int PrefixTotalLength = Prefix.Length + PrefixSpaces;

        /// <summary>
        /// The current position in the UserInputBuffer
        /// </summary>
        internal static volatile int BufferPosition = 0;

        /// <summary>
        /// The starting position of the output in the UserInputBuffer
        /// </summary>
        internal static volatile int BufferOutputAnchor = 0;

        /// <summary>
        /// The length of the output to be shown to the user.
        /// </summary>
        private static volatile int BufferOutputLength = 0;

        // Console width is actually 0 indexed, so we need to subtract 1 from the width.
        private static volatile int ConsoleMaxLength = InternalContext.CursorLeftPosLimit - 1;
        private static volatile int UserInputBufferMaxLength = ConsoleMaxLength - (Prefix.Length + PrefixSpaces);

        /// <summary>
        /// Initializes the ConsoleBuffer. Required to be called before using the ConsoleBuffer.
        /// </summary>
        internal static void Init() {
            DrawPrefix();
            InternalContext.BufferInitialized = true;
        }

        /// <summary>
        /// Inserts a character in the user input buffer.
        /// </summary>
        /// <param name="c">The character to insert.</param>
        internal static void Insert(char c) {

            // Insert at the current buffer pos.
            UserInputBuffer.Insert(BufferPosition, c);
            Interlocked.Increment(ref BufferPosition);

            // Check the output length and adjust the anchor and length.
            int outputLenValue = BufferOutputLength;
            int outputMaxLength = UserInputBufferMaxLength;

            // If we haven't hit the end of the output buffer, we can just increment BufferOutputLength.
            if (outputLenValue < outputMaxLength) {
                Interlocked.Increment(ref BufferOutputLength);

                // If the current buffer pos is less than the current buffer length,
                // we need to redraw because we have input that needs to be displayed.
                // abcdefghjiklmnopqrstuvwxyz
                //      ^
                //      If the current buffer pos was here, we would need to redraw
                //      as the buffer length is greater than the current buffer pos.
                if (BufferPosition < UserInputBuffer.Length) {
                    RedrawInput();
                }

                // Don't draw if the current input is suppressed.
                else if (!InternalContext.SuppressInput)
                    Console.Write(c);

            } else {
                // We have hit the end of the output, so we need to move the output anchor forward.
                Interlocked.Increment(ref BufferOutputAnchor);
                RedrawInput();
                return;
            }

            // Increment the console cursor.
            InternalContext.IncrementLeftPos();
        }

        /// <summary>
        /// Replaces a substring in the user input buffer with a new string.
        /// </summary>
        /// <param name="start">The starting index of the substring.</param>
        /// <param name="end">The ending index of the substring (not containing this character).</param>
        /// <param name="newString">The string to replace it with.</param>
        internal static void Replace(int start, int end, string newString) {
            start = Math.Max(0, start);
            end = Math.Min(UserInputBuffer.Length, end);
            int lenDif = newString.Length - (end - start);
            UserInputBuffer.Remove(start, end - start);
            UserInputBuffer.Insert(start, newString);

            int cursorPos, newEnd = start + newString.Length;
            Interlocked.Exchange(ref BufferPosition, newEnd);
            if (PrefixTotalLength + UserInputBuffer.Length < InternalContext.CursorLeftPosLimit) {
                cursorPos = newEnd + PrefixTotalLength;
                if (lenDif < 0) {
                    int moveLeft = InternalContext.CurrentCursorLeftPos - cursorPos;
                    InternalContext.DecrementLeftPos(moveLeft);
                    RemoveTrailingLetters(-lenDif, moveLeft);
                }
                Interlocked.Exchange(ref BufferOutputAnchor, 0);
                Interlocked.Exchange(ref BufferOutputLength, UserInputBuffer.Length);
            } else {
                if (lenDif < 0) {
                    int newAnchor = Math.Max(0, BufferOutputAnchor + lenDif);
                    cursorPos = newEnd - newAnchor + PrefixTotalLength;
                    int moveLeft = InternalContext.CurrentCursorLeftPos - cursorPos;
                    InternalContext.DecrementLeftPos(moveLeft);
                    RemoveTrailingLetters(-lenDif, moveLeft);
                    Interlocked.Exchange(ref BufferOutputAnchor, newAnchor);
                } else if (lenDif > 0) {
                    Interlocked.Exchange(ref BufferOutputAnchor, Math.Min(UserInputBuffer.Length - UserInputBufferMaxLength, BufferOutputAnchor + lenDif));
                    cursorPos = newEnd - BufferOutputAnchor + PrefixTotalLength;
                } else {
                    cursorPos = newEnd - BufferOutputAnchor + PrefixTotalLength;
                }
                Interlocked.Exchange(ref BufferOutputLength, UserInputBufferMaxLength);
            }

            if (InternalContext.SuppressInput) return;

            InternalContext.SetCursorVisible(false);
            InternalContext.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);

            // Redraw the prefix.
            DrawPrefix();

            // Redraw the user input.
            if (UserInputBuffer.Length != 0)
                Console.Write($"{UserInputBuffer.ToString(BufferOutputAnchor, BufferOutputLength)}");

            InternalContext.SetCursorPosition(cursorPos, InternalContext.CurrentCursorTopPos);
            InternalContext.SetCursorVisible(true);
        }

        internal static void SetBufferContent(string content) {
            FlushBuffer();
            UserInputBuffer.Append(content);
            MoveToStartBufferPosition();
            RedrawInput();
        }

        /// <summary>
        /// Redraws the current user input state.
        /// </summary>
        internal static void RedrawInput() {
            lock (InternalContext.WriteLock) {

                if (InternalContext.SuppressInput) return;

                // Don't redraw if we don't have anything to redraw.
                if (UserInputBuffer.Length == 0)
                    return;

                InternalContext.SetCursorVisible(false);

                // We need to store the previous pos in a temp variable
                // because DrawPrefix() will move the cursor.
                var previousPos = InternalContext.CurrentCursorLeftPos;

                Trace.Assert(BufferOutputLength != 0);

                // Redraw the prefix.
                InternalContext.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);
                DrawPrefix();

                Console.Write($"{UserInputBuffer.ToString(BufferOutputAnchor, BufferOutputLength)}");

                InternalContext.SetCursorPosition(previousPos, InternalContext.CurrentCursorTopPos);
                InternalContext.SetCursorVisible(true);
            }
        }

        /// <summary>
        /// Draws the prefix.
        /// </summary>
        internal static void DrawPrefix() {
            lock (InternalContext.WriteLock) {
                // Ensure the prefix is drawn at the correct position.
                Trace.Assert(InternalContext.CurrentCursorLeftPos == 0);

                // Determine if we need to use the opposite prefix.
                var prefix = BufferOutputAnchor > 0 ? PrefixOpposite : Prefix;

                Console.Write(prefix + new string(' ', PrefixSpaces));
                Interlocked.Exchange(ref InternalContext.CurrentCursorLeftPos, Prefix.Length + PrefixSpaces);
            }
        }

        /// <summary>
        /// Get the length of the word before the current cursor, used for Ctrl+Backspace.
        /// </summary>
        private static int GetLengthOfPreviousWord() {
            int counter = 0, index = BufferPosition - 1;

            while (index >= 0 && char.IsWhiteSpace(UserInputBuffer[index])) {
                --index;
                ++counter;
            }

            if (index >= 0) {
                if (char.IsLetterOrDigit(UserInputBuffer[index])) {
                    while (index >= 0 && char.IsLetterOrDigit(UserInputBuffer[index])) {
                        --index;
                        ++counter;
                    }
                } else if (char.IsSymbol(UserInputBuffer[index]) || char.IsPunctuation(UserInputBuffer[index])) {
                    while (index >= 0 && (char.IsSymbol(UserInputBuffer[index]) || char.IsPunctuation(UserInputBuffer[index]))) {
                        --index;
                        ++counter;
                    }
                }
            }

            return Math.Max(1, counter);
        }

        /// <summary>
        /// Get the length of the word after the current cursor, used for Ctrl+delete.
        /// </summary>
        private static int GetLengthOfNextWord() {
            int counter = 0, index = BufferPosition;

            if (char.IsLetterOrDigit(UserInputBuffer[index])) {
                while (index < UserInputBuffer.Length && char.IsLetterOrDigit(UserInputBuffer[index])) {
                    ++index;
                    ++counter;
                }
            } else if (char.IsSymbol(UserInputBuffer[index]) || char.IsPunctuation(UserInputBuffer[index])) {
                while (index < UserInputBuffer.Length && (char.IsSymbol(UserInputBuffer[index]) || char.IsPunctuation(UserInputBuffer[index]))) {
                    ++index;
                    ++counter;
                }
            }

            while (index < UserInputBuffer.Length && char.IsWhiteSpace(UserInputBuffer[index])) {
                ++index;
                ++counter;
            }

            return Math.Max(1, counter);
        }

        /// <summary>
        /// Moves the input buffer forward by one char. Equivalent to pressing the right arrow key.
        /// </summary>
        internal static void MoveCursorForward(bool inWords = false) {
            // If we're at the end of the buffer, do nothing.
            if (BufferPosition == UserInputBuffer.Length)
                return;

            int moveCnt = inWords ? GetLengthOfNextWord() : 1;

            Interlocked.Add(ref BufferPosition, moveCnt);

            int backLen = UserInputBuffer.Length - BufferOutputAnchor - BufferOutputLength;
            if (backLen > 0) {
                if (backLen > moveCnt) {
                    Interlocked.Add(ref BufferOutputAnchor, moveCnt);
                } else {
                    Interlocked.Add(ref BufferOutputAnchor, backLen);
                    InternalContext.IncrementLeftPos(moveCnt - backLen);
                }
                RedrawInput();
            } else {
                InternalContext.IncrementLeftPos(moveCnt);
            }
        }

        /// <summary>
        /// Moves the input buffer backward by one char. Equivalent to pressing the left arrow key.
        /// </summary>
        internal static void MoveCursorBackward(bool inWords = false) {
            // If we're at the beginning of the buffer, do nothing.
            if (BufferPosition == 0)
                return;

            int moveCnt = inWords ? GetLengthOfPreviousWord() : 1;

            Interlocked.Add(ref BufferPosition, -moveCnt);

            if (BufferOutputAnchor > 0) {
                if (BufferOutputAnchor >= moveCnt) {
                    Interlocked.Add(ref BufferOutputAnchor, -moveCnt);
                } else {
                    InternalContext.DecrementLeftPos(moveCnt - BufferOutputAnchor);
                    BufferOutputAnchor = 0;
                }
                RedrawInput();
            } else {
                InternalContext.DecrementLeftPos(moveCnt);
            }
        }

        /// <summary>
        /// Removes a char from the buffer 'forwards', equivalent to pressing the Delete key.
        /// </summary>
        internal static void RemoveForward(bool inWords = false) {
            // If we're at the end of the buffer, do nothing.
            if (BufferPosition >= UserInputBuffer.Length || InternalContext.CurrentCursorLeftPos == UserInputBufferMaxLength)
                return;

            int removeCnt = inWords ? GetLengthOfNextWord() : 1;

            UserInputBuffer.Remove(BufferPosition, removeCnt);

            RemoveTrailingLetters(0, 0);
            int backLen = UserInputBuffer.Length - BufferOutputAnchor - BufferOutputLength;
            if (backLen > 0) {
                if (backLen < removeCnt)
                    Interlocked.Add(ref BufferOutputLength, -(removeCnt - backLen));
            } else {
                Interlocked.Add(ref BufferOutputLength, -removeCnt);
            }
            RedrawInput();
        }

        /// <summary>
        /// Removes a char from the buffer 'backwards', equivalent to pressing the Backspace key.
        /// </summary>
        internal static void RemoveBackward(bool inWords = false) {
            // If we're at the start of the buffer, do nothing.
            if (BufferPosition == 0 || InternalContext.CurrentCursorLeftPos == 0 && !InternalContext._suppressInput)
                return;

            int removeCnt = inWords ? GetLengthOfPreviousWord() : 1;

            // Remove 'backward', i.e. backspace
            Interlocked.Add(ref BufferPosition, -removeCnt);

            UserInputBuffer.Remove(BufferPosition, removeCnt);

            if (BufferOutputAnchor > 0) {
                if (BufferOutputAnchor >= removeCnt) {
                    Interlocked.Add(ref BufferOutputAnchor, -removeCnt);
                } else {
                    int moveLeft = removeCnt - BufferOutputAnchor;
                    InternalContext.DecrementLeftPos(moveLeft);
                    RemoveTrailingLetters(removeCnt, moveLeft);
                    Interlocked.Add(ref BufferOutputLength, -moveLeft);
                    BufferOutputAnchor = 0;
                }
                RedrawInput();
            } else {
                InternalContext.DecrementLeftPos(removeCnt);
                RemoveTrailingLetters(removeCnt, removeCnt);
                if (BufferPosition == UserInputBuffer.Length) {
                    Interlocked.Add(ref BufferOutputLength, -removeCnt);
                } else {
                    int backLen = UserInputBuffer.Length - BufferOutputLength;
                    if (backLen > 0) {
                        if (backLen < removeCnt)
                            Interlocked.Add(ref BufferOutputLength, -(removeCnt - backLen));
                    } else {
                        Interlocked.Add(ref BufferOutputLength, -removeCnt);
                    }
                    RedrawInput();
                }
            }
        }

        private static void RemoveTrailingLetters(int removeCharCnt, int moveLeftCnt) {
            if (InternalContext.SuppressInput) return;
            int charAfter = BufferOutputLength - ((BufferPosition + removeCharCnt) - BufferOutputAnchor);
            Console.Write(new string(' ', moveLeftCnt + charAfter));
            InternalContext.ResetCursorPosition();
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
        private static int GetConsoleEndPosition() => InternalContext.CurrentCursorLeftPos + (UserInputBuffer.Length - BufferPosition) % UserInputBufferMaxLength;

        private static void RemoveTrailingLetter() {
            InternalContext.SetCursorVisible(false);

            if (BufferOutputAnchor != 0) {
                Console.SetCursorPosition(GetConsoleEndPosition(), InternalContext.CurrentCursorTopPos);
            } else
                Console.SetCursorPosition(UserInputBuffer.Length + PrefixTotalLength, InternalContext.CurrentCursorTopPos);


            Console.Write(' ');
            Console.SetCursorPosition(InternalContext.CurrentCursorLeftPos, InternalContext.CurrentCursorTopPos);

            InternalContext.SetCursorVisible(true);
        }

        /// <summary>
        /// Flushes the User Input Buffer.
        /// </summary>
        /// <returns>The string contained in the buffer.</returns>
        internal static string FlushBuffer() {
            var retval = UserInputBuffer.ToString();
            ClearVisibleUserInput();
            ClearBuffer();
            RemoveCurrentBufferInBackread();

            if (InternalContext.BufferInitialized) {
                InternalContext.SetCursorVisible(false);
                DrawPrefix();
                InternalContext.SetCursorVisible(true);
            }

            return retval;
        }

        private static void ClearBuffer() {
            // Set the buffer position to 0.
            Interlocked.Exchange(ref BufferPosition, 0);

            // Set the buffer anchor to 0.
            Interlocked.Exchange(ref BufferOutputAnchor, 0);

            // Set the buffer length to 0.
            Interlocked.Exchange(ref BufferOutputLength, 0);

            UserInputBuffer.Clear();
        }

        /// <summary>
        /// Clears the visible user input.
        /// Does not clear the internal buffer.
        /// </summary>
        internal static void ClearVisibleUserInput() {
            lock (InternalContext.WriteLock) {
                Console.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);

                bool requireCompleteClear = UserInputBuffer.Length > UserInputBufferMaxLength;

                for (int i = 0; i < ((requireCompleteClear ? UserInputBufferMaxLength : UserInputBuffer.Length) + PrefixTotalLength + 1); i++) {
                    Console.Write(' ');
                }

                Console.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);
                InternalContext.SetLeftCursorPosition(0);
            }
        }

        internal static void ClearCurrentLine() {
            lock (InternalContext.WriteLock) {
                InternalContext.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);
                for (int i = 0; i < InternalContext.CursorLeftPosLimit; i++) {
                    Console.Write(' ');
                }
                InternalContext.SetCursorPosition(0, InternalContext.CurrentCursorTopPos);
            }
        }

        internal static void MoveToStartBufferPosition() {
            Interlocked.Exchange(ref BufferPosition, 0);
            Interlocked.Exchange(ref BufferOutputAnchor, 0);
            if (UserInputBuffer.Length < UserInputBufferMaxLength)
                Interlocked.Exchange(ref BufferOutputLength, UserInputBuffer.Length);
            else
                Interlocked.Exchange(ref BufferOutputLength, UserInputBufferMaxLength);


            RedrawInput();
            InternalContext.SetLeftCursorPosition(0 + PrefixTotalLength);
        }

        internal static void MoveToEndBufferPosition() {
            if (InternalContext.SuppressInput) return;

            Interlocked.Exchange(ref BufferPosition, UserInputBuffer.Length);

            if (PrefixTotalLength + UserInputBuffer.Length < InternalContext.CursorLeftPosLimit) {
                Interlocked.Exchange(ref BufferOutputAnchor, 0);
                Interlocked.Exchange(ref BufferOutputLength, UserInputBuffer.Length);

                if (UserInputBuffer.Length == 0)
                    InternalContext.SetLeftCursorPosition(PrefixTotalLength);
                else
                    InternalContext.SetLeftCursorPosition(UserInputBuffer.Length + PrefixTotalLength);
                return;
            }

            InternalContext.SetLeftCursorPosition(UserInputBufferMaxLength + PrefixTotalLength);
            Interlocked.Exchange(ref BufferOutputAnchor, UserInputBuffer.Length - UserInputBufferMaxLength);
            Interlocked.Exchange(ref BufferOutputLength, UserInputBufferMaxLength);
            RedrawInput();
        }


        private static volatile int BufferBackreadPos = 0;
        private const int BackreadBufferLimit = 32;
        internal static List<string> BackreadBuffer = new(BackreadBufferLimit + 1);
        internal static string UserInputBufferCopy = string.Empty;
        internal static volatile bool isCurrentBufferCopied;

        public static void IncrementBackreadPos() {
            if (BufferBackreadPos == BackreadBuffer.Count) return;
            Interlocked.Increment(ref BufferBackreadPos);
        }

        private static void DecrementBackreadPos() {
            if (BufferBackreadPos == 0) return;
            Interlocked.Decrement(ref BufferBackreadPos);
        }

        public static void ClearBackreadBuffer() {
            BackreadBuffer.Clear();
            Interlocked.Exchange(ref BufferBackreadPos, 0);
        }

        public static void RemoveCurrentBufferInBackread() {
            isCurrentBufferCopied = false;
            UserInputBufferCopy = string.Empty;
        }

        /// <summary>
        /// Moves the backread backwards. Equivalent to pressing the Up arrow.
        /// </summary>
        /// <returns>The string in the backread buffer.</returns>
        public static string GetBackreadBackwards() {
            if (BackreadBuffer.Count == 0) return UserInputBuffer.ToString();

            if (!isCurrentBufferCopied) {
                UserInputBufferCopy = UserInputBuffer.ToString();
                isCurrentBufferCopied = true;
            }

            if (BufferBackreadPos == 0) {
                return BackreadBuffer[0];
            } else {
                DecrementBackreadPos();
                return BackreadBuffer[BufferBackreadPos];
            }
        }

        /// <summary>
        /// Moves the backread forward. Equivalent to pressing the Down arrow.
        /// </summary>
        /// <returns>The string in the backread buffer.</returns>
        public static string GetBackreadForwards() {
            if (BackreadBuffer.Count == 0) return UserInputBuffer.ToString();

            if (BufferBackreadPos == BackreadBuffer.Count) {
                return isCurrentBufferCopied ? UserInputBufferCopy : UserInputBuffer.ToString();
            } else {
                IncrementBackreadPos();
                if (BufferBackreadPos == BackreadBuffer.Count)
                    return UserInputBufferCopy;
                else
                    return BackreadBuffer[BufferBackreadPos];
            }
        }

        /// <summary>
        /// Adds a string to the backread buffer.
        /// </summary>
        /// <param name="message">The string to be added.</param>
        public static void AddToBackreadBuffer(string message) {
            if (!string.IsNullOrWhiteSpace(message) && (BackreadBuffer.Count == 0 || message != BackreadBuffer[^1])) {
                BackreadBuffer.Add(message);
                if (BackreadBuffer.Count > BackreadBufferLimit)
                    BackreadBuffer.RemoveAt(0);
            }
            Interlocked.Exchange(ref BufferBackreadPos, BackreadBuffer.Count);
        }

    }
}