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
        public static event EventHandler<string>? MessageReceived;
        public static event EventHandler<ConsoleKey>? OnKeyInput;
        
        private static Thread? _readerThread;
        private static CancellationTokenSource? _cancellationTokenSource;

        public static void SetInputVisible(bool visible) {
            InternalContext.SuppressInput = !visible;
        }

        /// <summary>
        /// Starts a new Console Reader thread.
        /// </summary>
        /// <param name="cancellationToken">Exits from the reader thread when cancelled.</param>
        public static void BeginReadThread(CancellationTokenSource cancellationTokenSource) {
            if (_readerThread is { IsAlive: true })  {
                throw new InvalidOperationException("Console Reader thread is already running.");
            }
            
            _cancellationTokenSource = cancellationTokenSource;
            _readerThread = new Thread(new ParameterizedThreadStart(KeyListener!));
            _readerThread.Name = "ConsoleInteractive.ConsoleReader Reader Thread";
            _readerThread.Start(_cancellationTokenSource.Token);
        }
        
        /// <summary>
        /// Stops an existing Console Reader thread, if any.
        /// </summary>
        public static void StopReadThread() {
            if (_readerThread is { IsAlive: false }) {
                return;
            }
            
            _cancellationTokenSource?.Cancel();
        }

        public static string RequestImmediateInput() {
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            var bufferString = string.Empty;
            
            BeginReadThread(new CancellationTokenSource());
            MessageReceived += (sender, s) => {
                bufferString = s;
                autoEvent.Set();
            };
            
            autoEvent.WaitOne();
            StopReadThread();
            _readerThread!.Join();
            return bufferString;
        }

        /// <summary>
        /// Listens for keypresses and acts accordingly.
        /// </summary>
        /// <param name="cancellationToken">Exits from the key listener once cancelled.</param>
        private static void KeyListener(object cancellationToken) {
            CancellationToken token = (CancellationToken)cancellationToken!;

            while (!token.IsCancellationRequested) {
                if (token.IsCancellationRequested) return;

                // Guard against window resize
                InternalContext.CursorLeftPosLimit = Console.BufferWidth;
                InternalContext.CursorTopPosLimit = Console.BufferHeight;

                while (Console.KeyAvailable == false) {
                    if (token.IsCancellationRequested) return;
                    continue;
                }

                ConsoleKeyInfo k;
                k = Console.ReadKey(true);

                if (token.IsCancellationRequested) return;

                OnKeyInput?.Invoke(null, k.Key);
                
                switch (k.Key) {
                    case ConsoleKey.Enter:
                        if (token.IsCancellationRequested) return;

                        lock (InternalContext.WriteLock) {
                            ConsoleBuffer.ClearVisibleUserInput();
                            var input = ConsoleBuffer.FlushBuffer();

                            MessageReceived?.Invoke(null, input);

                            /*
                             * The user can call cancellation after a command on enter.
                             * This helps us safely exit the reader thread.
                             */
                            if (token.IsCancellationRequested) return;
                        }

                        break;
                    case ConsoleKey.Backspace:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock)
                            ConsoleBuffer.RemoveBackward();

                        break;
                    case ConsoleKey.Delete:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock)
                            ConsoleBuffer.RemoveForward();

                        break;
                    case ConsoleKey.End:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock)
                            ConsoleBuffer.MoveToEndBufferPosition();
                        
                        break;
                    case ConsoleKey.Home:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock)
                            ConsoleBuffer.MoveToStartBufferPosition();

                        break;
                    case ConsoleKey.LeftArrow:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock)
                            ConsoleBuffer.MoveCursorBackward();
                        
                        break;
                    case ConsoleKey.RightArrow:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock)
                            ConsoleBuffer.MoveCursorForward();
                        
                        break;
                    default:
                        if (token.IsCancellationRequested) return;

                        // If the keypress doesn't map to any Unicode characters, or invalid characters.
                        switch (k.KeyChar) {
                            case '\0':
                            case '\t':
                            case '\r':
                            case '\n':
                                continue;
                        }


                        lock (InternalContext.WriteLock)
                            ConsoleBuffer.Insert(k.KeyChar);

                        if (token.IsCancellationRequested) return;
                        break;
                }
            }
        }
    }
}