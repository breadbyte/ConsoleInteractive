using System;
using System.Diagnostics;
using System.Threading;

namespace ConsoleInteractive {
    public static class ConsoleReader {
        /// <summary>
        /// Invoked when a message is received.
        /// </summary>
        public static event EventHandler<string>? MessageReceived;

        private static Buffer LastInputBuffer = new(string.Empty, 0);
        public static event EventHandler<Buffer>? OnInputChange;

        private static Thread? _readerThread;
        private static CancellationTokenSource? _cancellationTokenSource;
        private static object ThreadLock = new();

        public static void SetInputVisible(bool visible) {
            InternalContext.SuppressInput = !visible;
        }

        public static Buffer GetBufferContent() {
            return new Buffer(ConsoleBuffer.UserInputBuffer.ToString()!, ConsoleBuffer.BufferPosition);
        }

        public static void ClearBuffer() {
            ConsoleBuffer.FlushBuffer();
        }

        internal static void CheckInputBufferUpdate() {
            ConsoleSuggestion.OnInputUpdate();
            Buffer InputBuffer = GetBufferContent();
            if (InputBuffer != LastInputBuffer) {
                LastInputBuffer = InputBuffer;
                OnInputChange?.Invoke(null, LastInputBuffer);
            }
        }

        /// <summary>
        /// Starts a new Console Reader thread.
        /// </summary>
        /// <param name="cancellationToken">Exits from the reader thread when cancelled.</param>
        public static void BeginReadThread(CancellationTokenSource cancellationTokenSource) {
            lock (ThreadLock) {
                if (_readerThread is { IsAlive: true }) {
                    throw new InvalidOperationException("Console Reader thread is already running.");
                }

                _cancellationTokenSource = cancellationTokenSource;
                _readerThread = new Thread(new ParameterizedThreadStart(KeyListener!)) {
                    Name = "ConsoleInteractive.ConsoleReader Reader Thread"
                };
                _readerThread.Start(_cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// Stops an existing Console Reader thread, if any.
        /// </summary>
        public static void StopReadThread() {
            lock (ThreadLock) {
                if (_readerThread is { IsAlive: false }) {
                    return;
                }

                _cancellationTokenSource?.Cancel();
                InternalContext.BufferInitialized = false;
                ConsoleBuffer.ClearBackreadBuffer();
                ConsoleBuffer.FlushBuffer();
                ConsoleBuffer.ClearCurrentLine();
            }
        }

        public static string RequestImmediateInput() {
            AutoResetEvent autoEvent = new(false);
            var bufferString = string.Empty;

            BeginReadThread(new CancellationTokenSource());
            MessageReceived += (sender, s) => {
                bufferString = s;
                autoEvent.Set();
            };

            autoEvent.WaitOne();
            StopReadThread();
            _readerThread!.Join();
            InternalContext.BufferInitialized = false;
            return bufferString;
        }

        /// <summary>
        /// Listens for keypresses and acts accordingly.
        /// </summary>
        /// <param name="cancellationToken">Exits from the key listener once cancelled.</param>
        private static void KeyListener(object cancellationToken) {
            CancellationToken token = (CancellationToken)cancellationToken!;
            ConsoleBuffer.Init();

            while (!token.IsCancellationRequested) {
                if (token.IsCancellationRequested) return;

                // TODO: Guard against window resize
                // this is not entirely foolproof
                // need to interact internally with InternalContext
                // and mess with the ConsoleBuffer to make this work
                InternalContext.CursorLeftPosLimit = Console.BufferWidth;
                InternalContext.CursorTopPosLimit = Console.BufferHeight;

                while (Console.KeyAvailable == false) {
                    if (token.IsCancellationRequested) return;
                    Thread.Sleep(8);
                    continue;
                }

                ConsoleKeyInfo k = Console.ReadKey(true);

                if (token.IsCancellationRequested) return;

                switch (k.Key) {
                    case ConsoleKey.Enter:
                        if (token.IsCancellationRequested) return;
                        ConsoleSuggestion.HandleEnter();

                        lock (InternalContext.WriteLock) {
                            ConsoleBuffer.ClearVisibleUserInput();
                            var input = ConsoleBuffer.FlushBuffer();

                            ConsoleBuffer.AddToBackreadBuffer(input);
                            MessageReceived?.Invoke(null, input);

                            /*
                             * The user can call cancellation after a command on enter.
                             * This helps us safely exit the reader thread.
                             */
                            if (token.IsCancellationRequested) return;
                        }
                        CheckInputBufferUpdate();

                        break;
                    case ConsoleKey.Backspace:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock) {
                            if (k.Modifiers == ConsoleModifiers.Control)
                                ConsoleBuffer.RemoveBackward(inWords: true);
                            else
                                ConsoleBuffer.RemoveBackward();
                        }
                        CheckInputBufferUpdate();

                        break;
                    case ConsoleKey.Delete:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock) {
                            if (k.Modifiers == ConsoleModifiers.Control)
                                ConsoleBuffer.RemoveForward(inWords: true);
                            else
                                ConsoleBuffer.RemoveForward();
                        }
                        CheckInputBufferUpdate();

                        break;
                    case ConsoleKey.End:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock)
                            ConsoleBuffer.MoveToEndBufferPosition();
                        CheckInputBufferUpdate();

                        break;
                    case ConsoleKey.Home:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock)
                            ConsoleBuffer.MoveToStartBufferPosition();
                        CheckInputBufferUpdate();

                        break;
                    case ConsoleKey.LeftArrow:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock) {
                            if (k.Modifiers == ConsoleModifiers.Control)
                                ConsoleBuffer.MoveCursorBackward(inWords: true);
                            else
                                ConsoleBuffer.MoveCursorBackward();
                        }
                        CheckInputBufferUpdate();

                        break;
                    case ConsoleKey.RightArrow:
                        if (token.IsCancellationRequested) return;
                        lock (InternalContext.WriteLock) {
                            if (k.Modifiers == ConsoleModifiers.Control)
                                ConsoleBuffer.MoveCursorForward(inWords: true);
                            else
                                ConsoleBuffer.MoveCursorForward();
                        }
                        CheckInputBufferUpdate();

                        break;
                    case ConsoleKey.UpArrow:
                        if (token.IsCancellationRequested) return;
                        if (ConsoleSuggestion.HandleUpArrow()) break;
                        lock (InternalContext.WriteLock) {
                            if (ConsoleBuffer.BackreadBuffer.Count == 0) break;

                            var backread = ConsoleBuffer.GetBackreadBackwards();
                            var backreadCopied = ConsoleBuffer.isCurrentBufferCopied;
                            var backreadString = ConsoleBuffer.UserInputBufferCopy;
                            ConsoleBuffer.SetBufferContent(backread);
                            ConsoleBuffer.MoveToEndBufferPosition();

                            // SetBufferContent clears the backread, so we need to pass it again
                            if (backreadCopied) {
                                ConsoleBuffer.isCurrentBufferCopied = backreadCopied;
                                ConsoleBuffer.UserInputBufferCopy = backreadString;

                                Trace.Assert(ConsoleBuffer.isCurrentBufferCopied);
                            }
                        }

                        break;
                    case ConsoleKey.DownArrow:
                        if (token.IsCancellationRequested) return;
                        if (ConsoleSuggestion.HandleDownArrow()) break;
                        lock (InternalContext.WriteLock) {
                            if (ConsoleBuffer.BackreadBuffer.Count == 0) break;

                            var backread = ConsoleBuffer.GetBackreadForwards();
                            var backreadCopied = ConsoleBuffer.isCurrentBufferCopied;
                            var backreadString = ConsoleBuffer.UserInputBufferCopy;
                            ConsoleBuffer.SetBufferContent(backread);
                            ConsoleBuffer.MoveToEndBufferPosition();


                            // SetBufferContent clears the backread, so we need to pass it again
                            if (backreadCopied) {
                                ConsoleBuffer.isCurrentBufferCopied = backreadCopied;
                                ConsoleBuffer.UserInputBufferCopy = backreadString;

                                Trace.Assert(ConsoleBuffer.isCurrentBufferCopied);
                            }
                        }

                        break;
                    case ConsoleKey.Tab:
                        if (token.IsCancellationRequested) return;
                        ConsoleSuggestion.HandleTab();

                        break;
                    case ConsoleKey.Escape:
                        if (token.IsCancellationRequested) return;
                        ConsoleSuggestion.HandleEscape();

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

                        CheckInputBufferUpdate();

                        if (token.IsCancellationRequested) return;
                        break;
                }
            }
            InternalContext.BufferInitialized = false;
        }

        public record Buffer {
            public string Text { get; init; }
            public int CursorPosition { get; init; }

            public Buffer(string text, int cursorPosition) {
                Text = text;
                CursorPosition = cursorPosition;
            }
        }
    }
}