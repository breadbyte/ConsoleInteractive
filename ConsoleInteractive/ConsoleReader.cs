using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleInteractive.Buffer;

namespace ConsoleInteractive {
    public static class ConsoleReader {
        /// <summary>
        /// Invoked when a message is received.
        /// </summary>
        public static event EventHandler<string> MessageReceived;

        /// <summary>
        /// Starts a new Console Reader thread.
        /// </summary>
        /// <param name="cancellationToken">Exits from the reader thread when cancelled.</param>
        public static void BeginReadThread(CancellationToken cancellationToken) {
            var t = new Thread(new ParameterizedThreadStart(KeyListener));
            t.Name = "ConsoleInteractive.ConsoleReader Reader Thread";
            t.Start(cancellationToken);
        }

        /// <summary>
        /// Listens for keypresses and acts accordingly.
        /// </summary>
        /// <param name="cancellationToken">Exits from the key listener once cancelled.</param>
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
                            InternalContext.Buffer.ClearVisibleUserInput();
                            var input = InternalContext.Buffer.FlushBuffer();
                            
                            MessageReceived?.Invoke(null, input);

                            /*
                             * The user can call cancellation after a command on enter.
                             * This helps us safely exit the reader thread.
                             */
                            if (token.IsCancellationRequested) {
                                return;
                            }
                        }
                        break;
                    case ConsoleKey.Backspace:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock) 
                            InternalContext.Buffer.RemoveBackward();
                        
                        break;
                    case ConsoleKey.Delete:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock) 
                            InternalContext.Buffer.RemoveForward();
                        
                        break;
                    case ConsoleKey.End:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock) 
                            InternalContext.Buffer.MoveToEndBufferPosition();
                        
                        break;
                    case ConsoleKey.Home:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock)
                            InternalContext.Buffer.MoveToStartBufferPosition();
                        
                        break;
                    case ConsoleKey.LeftArrow:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock)
                            InternalContext.Buffer.MoveCursorBackward();
                        
                        break;
                    case ConsoleKey.RightArrow:
                        token.ThrowIfCancellationRequested();
                        lock (InternalContext.WriteLock)
                            InternalContext.Buffer.MoveCursorForward();
                        
                        break;
                    default:
                        token.ThrowIfCancellationRequested();

                        // If the keypress doesn't map to any Unicode characters.
                        switch (k.KeyChar) {
                            case '\0':
                            case '\t':
                            case '\r':
                            case '\n':
                                break;
                        }

                        lock (InternalContext.WriteLock) {
                            InternalContext.Buffer.Insert(k.KeyChar);
                        }
                        break;
                }
            }
        }
    }
}