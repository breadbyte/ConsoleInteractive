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
                            ConsoleBuffer.ClearVisibleUserInput();

                            MessageReceived?.Invoke(null, ConsoleBuffer.UserInputBuffer.ToString());

                            /*
                             * The user can call cancellation after a command on enter.
                             * This helps us safely exit the reader thread.
                             */
                            if (token.IsCancellationRequested) {
                                ConsoleBuffer.UserInputBuffer.Clear();
                                break;
                            }

                            ConsoleBuffer.UserInputBuffer.Clear();
                            Interlocked.Exchange(ref InternalContext.CursorLeftPos, 0);
                        }

                        break;
                    case ConsoleKey.Backspace:
                        //fixme
                        token.ThrowIfCancellationRequested();
                        ConsoleWriter.WriteLine($"LP: {InternalContext.CursorLeftPos} | TP {InternalContext.CursorTopPos} | BP {ConsoleBuffer.CurrentBufferPos}"); // todo remove devel line
                        if (InternalContext.CursorLeftPos == 0)
                            break;
                        ConsoleBuffer.RemoveBackward();
                        break;
                    case ConsoleKey.Delete:
                        //fixme
                        token.ThrowIfCancellationRequested();
                        ConsoleWriter.WriteLine($"LP: {InternalContext.CursorLeftPos} | TP {InternalContext.CursorTopPos} | BP {ConsoleBuffer.CurrentBufferPos}"); // todo remove devel line
                        if (Console.CursorLeft == ConsoleBuffer.UserInputBuffer.Length)
                            break;
                        ConsoleBuffer.RemoveForward();
                        break;
                    case ConsoleKey.End:
                        // fixme
                        token.ThrowIfCancellationRequested();

                        lock (InternalContext.WriteLock) {
                            InternalContext.SetLeftCursorPosition(ConsoleBuffer.UserInputBuffer.Length);
                        }
                        break;
                    case ConsoleKey.Home:
                        //fixme
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock) {
                            InternalContext.SetLeftCursorPosition(0);
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        token.ThrowIfCancellationRequested();
                        ConsoleBuffer.MoveCursorBackward();
                        ConsoleWriter.WriteLine($"LP: {InternalContext.CursorLeftPos} | TP {InternalContext.CursorTopPos} | BP {ConsoleBuffer.CurrentBufferPos}"); // todo remove devel line

                        // fixme
                        lock (InternalContext.WriteLock) {
                            if (k.Modifiers.HasFlag(ConsoleModifiers.Control)) {
                                var cts = ConsoleBuffer.UserInputBuffer.ToString()[..(InternalContext.CursorLeftPos - 1 < 0 ? 0 : InternalContext.CursorLeftPos - 1)];
                                Console.SetCursorPosition((cts.LastIndexOf(' ') + 1), InternalContext.CursorTopPos);
                                break;
                            }
                        }

                        break;
                    case ConsoleKey.RightArrow:
                        token.ThrowIfCancellationRequested();
                        ConsoleBuffer.MoveCursorForward();
                        ConsoleWriter.WriteLine($"LP: {InternalContext.CursorLeftPos} | TP {InternalContext.CursorTopPos} | BP {ConsoleBuffer.CurrentBufferPos}"); // todo remove devel line

                        // fixme
                        lock (InternalContext.WriteLock) {
                            if (k.Modifiers.HasFlag(ConsoleModifiers.Control)) {
                                var cts = ConsoleBuffer.UserInputBuffer.ToString()[InternalContext.CursorLeftPos..];
                                var indexOf = cts.IndexOf(' ');
                                Console.SetCursorPosition(
                                    indexOf == -1
                                        ? ConsoleBuffer.UserInputBuffer.Length
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
                            ConsoleBuffer.Insert(k.KeyChar);
                            ConsoleWriter.WriteLine($"{k.KeyChar} | LP: {InternalContext.CursorLeftPos} | TP {InternalContext.CursorTopPos} | BP {ConsoleBuffer.CurrentBufferPos}"); // todo remove devel line
                        }
                        break;
                }
            }
        }
    }
}