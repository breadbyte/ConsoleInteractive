using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleInteractive
{
    /// <summary>
    /// The OutputRedirectedBuffer is a helper function for when IsOutputRedirected is true.
    /// All cursor functions are removed, and all use of the InternalContext is removed.
    /// 
    /// We are operating under the assumption that Input will be passed line by line or character by character,
    /// and that all output is to be sent to the console by entire lines.
    /// 
    /// The prefix is not drawn for this reason.
    /// </summary>
    internal static class OutputRedirectedBuffer
    {
        internal static StringBuilder UserInputBuffer = new();

        /// <summary>
        /// Initializes the ConsoleBuffer. Required to be called before using the ConsoleBuffer.
        /// </summary>
        internal static void Init() {
            InternalContext.BufferInitialized = true;
        }

        /// <summary>
        /// Inserts a character in the user input buffer.
        /// </summary>
        /// <param name="c">The character to insert.</param>
        internal static void Insert(char c) {
            // Insert at the current buffer pos.
            UserInputBuffer.Append(c);
        }

        internal static void SetBufferContent(string content) {
            FlushBuffer();
            UserInputBuffer.Append(content);
        }

        /// <summary>
        /// Flushes the User Input Buffer.
        /// </summary>
        /// <returns>The string contained in the buffer.</returns>
        internal static string FlushBuffer()
        {
            var retval = UserInputBuffer.ToString();
            ClearBuffer();

            return retval;
        }

        private static void ClearBuffer() {
            UserInputBuffer.Clear();
        }
    }
}