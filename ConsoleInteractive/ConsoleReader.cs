using System;
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

                ConsoleKeyInfo k;
                k = Console.ReadKey(true);
                
                token.ThrowIfCancellationRequested();
                
                var cursorPos = Console.CursorLeft;

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
                        }

                        break;
                    case ConsoleKey.Backspace:
                        token.ThrowIfCancellationRequested();
                        if (cursorPos == 0)
                            break;

                        lock (InternalContext.WriteLock) {
                            Console.CursorVisible = false;
                            InternalContext.UserInputBuffer.Remove(cursorPos - 1, 1);
                            Console.Write("\b \b");
                            Console.Write(InternalContext.UserInputBuffer.ToString()[Console.CursorLeft..] + " ");

                            Console.SetCursorPosition(cursorPos - 1, Console.CursorTop);
                            Console.CursorVisible = true;
                        }

                        break;
                    case ConsoleKey.Delete:
                        token.ThrowIfCancellationRequested();
                        if (cursorPos == InternalContext.UserInputBuffer.Length)
                            break;
                        
                        lock (InternalContext.WriteLock) {
                            Console.CursorVisible = false;

                            InternalContext.UserInputBuffer.Remove(cursorPos, 1);
                            Console.Write(InternalContext.UserInputBuffer.ToString()[Console.CursorLeft..] + " ");

                            Console.SetCursorPosition(cursorPos, Console.CursorTop);
                            Console.CursorVisible = true;
                        }
                        break;
                    case ConsoleKey.End:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock) 
                            Console.CursorLeft = InternalContext.UserInputBuffer.Length;
                        break;
                    case ConsoleKey.Home:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock) 
                            Console.CursorLeft = 0;
                        break;
                    case ConsoleKey.LeftArrow:
                        token.ThrowIfCancellationRequested();
                        if (Console.CursorLeft == 0)
                            break;

                        lock (InternalContext.WriteLock) {

                            if (k.Modifiers.HasFlag(ConsoleModifiers.Control)) {
                                var cts = InternalContext.UserInputBuffer.ToString()[..(Console.CursorLeft - 1 < 0 ? 0 : Console.CursorLeft - 1)];
                                Console.SetCursorPosition((cts.LastIndexOf(' ') + 1), Console.CursorTop);
                                break;
                            }

                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        token.ThrowIfCancellationRequested();
                        if (InternalContext.UserInputBuffer.Length <= Console.CursorLeft)
                            break;

                        lock (InternalContext.WriteLock) {
                            if (k.Modifiers.HasFlag(ConsoleModifiers.Control)) {
                                var cts = InternalContext.UserInputBuffer.ToString()[(Console.CursorLeft)..];
                                var indexOf = cts.IndexOf(' ');
                                Console.SetCursorPosition(
                                    indexOf == -1 ? InternalContext.UserInputBuffer.Length : indexOf + 1 + Console.CursorLeft,
                                    Console.CursorTop);
                                break;
                            }

                            Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
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
                            InternalContext.UserInputBuffer.Insert(cursorPos, k.KeyChar);
                            Console.Write(InternalContext.UserInputBuffer.ToString()[cursorPos..]);

                            Console.SetCursorPosition(cursorPos + 1, Console.CursorTop);

                            Console.CursorVisible = true;
                        }
                        break;
                }
            }
        }
    }
}