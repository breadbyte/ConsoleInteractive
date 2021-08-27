using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleInteractive {
    public static class ConsoleReader {
        /// <summary>
        /// Invoked when a message is received.
        /// </summary>
        public static event EventHandler<string> MessageReceived;

        /// <summary>
        /// Starts a new Console Reader thread.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public static void BeginReadThread(CancellationToken cancellationToken) {
            var t = new Thread(new ParameterizedThreadStart(KeyListener));
            t.Name = "ConsoleInteractive.ConsoleReader Reader Thread";
            t.Start(cancellationToken);
        }

        /// <summary>
        /// Listens for keypresses and acts accordingly.
        /// </summary>
        /// <param name="cancellationToken"></param>
        private static void KeyListener(object cancellationToken) {
            CancellationToken token = (CancellationToken)cancellationToken!;

            while (!token.IsCancellationRequested) {
                token.ThrowIfCancellationRequested();

                // Guard against window resize
                InternalContext.CursorLeftPosLimit = Console.BufferWidth;
                InternalContext.CursorTopPosLimit = Console.BufferHeight;
                
                while (Console.KeyAvailable == false) {
                    token.ThrowIfCancellationRequested();
                    continue;
                }

                ConsoleKeyInfo k;
                k = Console.ReadKey(true);

                token.ThrowIfCancellationRequested();

                switch (k.Key) {
                    case ConsoleKey.Enter:
                        token.ThrowIfCancellationRequested();

                        lock (InternalContext.WriteLock) {
                            InternalContext.ClearVisibleUserInput();

                            MessageReceived?.Invoke(null, InternalContext.UserInputBuffer.ToString());

                            /*
                             * The user can call cancellation after a command on enter.
                             * This helps us safely exit the reader thread.
                             */
                            if (token.IsCancellationRequested) {
                                InternalContext.UserInputBuffer.Clear();
                                break;
                            }

                            InternalContext.UserInputBuffer.Clear();
                            Interlocked.Exchange(ref InternalContext.CursorLeftPos, 0);
                        }

                        break;
                    case ConsoleKey.Backspace:
                        token.ThrowIfCancellationRequested();
                        if (InternalContext.CursorLeftPos == 0)
                            break;
                        
                        lock (InternalContext.WriteLock) {
                            Console.CursorVisible = false;
                            
                            if (InternalContext.CursorLeftPos == 1)
                                InternalContext.UserInputBuffer.Remove(0, 1);
                            else
                                InternalContext.UserInputBuffer.Remove(InternalContext.CursorLeftPos - 1, 1);
                            
                            Console.Write("\b \b");
                            InternalContext.DecrementLeftPos();

                            Console.Write(InternalContext.UserInputBuffer.ToString()[InternalContext.CursorLeftPos..]);
                            Console.CursorVisible = true;
                        }

                        break;
                    case ConsoleKey.Delete:
                        token.ThrowIfCancellationRequested();
                        if (Console.CursorLeft == InternalContext.UserInputBuffer.Length)
                            break;

                        lock (InternalContext.WriteLock) {
                            var prevPos = InternalContext.CursorLeftPos;
                            Console.CursorVisible = false;

                            InternalContext.UserInputBuffer.Remove(InternalContext.CursorLeftPos, 1);
                            Console.Write(InternalContext.UserInputBuffer.ToString()[InternalContext.CursorLeftPos..] + " ");

                            InternalContext.SetCursorPosition(prevPos);
                            Console.CursorVisible = true;
                        }

                        break;
                    case ConsoleKey.End:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock) {
                            InternalContext.SetCursorPosition(InternalContext.UserInputBuffer.Length);
                        }
                        break;
                    case ConsoleKey.Home:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock) {
                            InternalContext.SetCursorPosition(0);
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        token.ThrowIfCancellationRequested();
                        if (InternalContext.CursorLeftPos == 0)
                            break;

                        InternalContext.DecrementLeftPos();

                        lock (InternalContext.WriteLock) {
                            // todo fixme
                            if (k.Modifiers.HasFlag(ConsoleModifiers.Control)) {
                                var cts = InternalContext.UserInputBuffer.ToString()[..(InternalContext.CursorLeftPos - 1 < 0 ? 0 : InternalContext.CursorLeftPos - 1)];
                                Console.SetCursorPosition((cts.LastIndexOf(' ') + 1), InternalContext.CursorTopPos);
                                break;
                            }
                        }

                        break;
                    case ConsoleKey.RightArrow:
                        token.ThrowIfCancellationRequested();
                        if (InternalContext.UserInputBuffer.Length <= InternalContext.CursorLeftPos)
                            break;
                        
                        InternalContext.IncrementLeftPos();

                        lock (InternalContext.WriteLock) {
                            // todo fixme
                            if (k.Modifiers.HasFlag(ConsoleModifiers.Control)) {
                                var cts = InternalContext.UserInputBuffer.ToString()[InternalContext.CursorLeftPos..];
                                var indexOf = cts.IndexOf(' ');
                                Console.SetCursorPosition(
                                    indexOf == -1
                                        ? InternalContext.UserInputBuffer.Length
                                        : indexOf + 1 + InternalContext.CursorLeftPos,
                                    Console.CursorTop);
                                break;
                            }
                        }

                        break;
                    default:
                        token.ThrowIfCancellationRequested();

                        // For special events where the keypress actually sends a keycode.
                        switch (k.Key) {
                            case ConsoleKey.Tab:
                                continue;
                        }

                        // If the keypress doesn't map to any Unicode characters.
                        if (k.KeyChar == '\0')
                            break;

                        lock (InternalContext.WriteLock) {
                            Console.CursorVisible = false;
                            InternalContext.UserInputBuffer.Insert(InternalContext.CursorLeftPos, k.KeyChar);
                            Console.Write(k.KeyChar);
                            InternalContext.IncrementLeftPos();

                            // If we have more things behind the buffer, print it out
                            if (InternalContext.UserInputBuffer.Length > InternalContext.CursorLeftPos)
                                Console.Write(InternalContext.UserInputBuffer.ToString()[InternalContext.CursorLeftPos..]);

                            Console.CursorVisible = true;
                        }
                        break;
                }
            }
        }
    }
}