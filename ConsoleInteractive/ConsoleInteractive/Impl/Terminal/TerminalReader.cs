using System;
using System.Diagnostics;
using System.Threading;
using ConsoleInteractive.Interface;
using ConsoleInteractive.Interface.Abstract;

namespace ConsoleInteractive.Impl.Terminal; 

public class TerminalReader : InputReader {
    CancellationTokenSource? CancellationTokenSource { get; set; }

    public InputBuffer? InputBuffer { get; set; }
    public Cursor Cursor { get; set; }
    public Thread? ReaderThread { get; set; }

    public override string RequestImmediateInput(bool password = false) {
        lock (InternalContext.ReaderThreadLock) {
            AutoResetEvent autoEvent = new(false);
            var bufferString = string.Empty;

            BeginReaderThread();
            ConsoleBuffer.Init();
            MessageReceived += (sender, s) =>
            {
                bufferString = s;
                autoEvent.Set();
            };

            autoEvent.WaitOne();
            StopReaderThread();
            return bufferString;
        }
    }

    public override void BeginReaderThread() {
        lock (InternalContext.ReaderThreadLock) {
            if (ReaderThread is { IsAlive: true }) {
                throw new InvalidOperationException("Console Reader thread is already running.");
            }

            CancellationTokenSource = new();
            ReaderThread = new Thread(new ParameterizedThreadStart(KeyListener)) {
                Name = "ConsoleInteractive.ConsoleReader Reader Thread"
            };
            ReaderThread.Start(CancellationTokenSource.Token);
        }
    }

    public override void StopReaderThread() {
        lock (InternalContext.ReaderThreadLock) {
            if (ReaderThread is { IsAlive: false }) {
                return;
            }

            CancellationTokenSource?.Cancel();
            ReaderThread!.Join();
            InputBuffer?.Close();
        }
    }

    public override void KeyListener(object? cancellationToken) {
        var token = (CancellationToken)cancellationToken!;
        bool dirtyBuffer = false;

        while (!Console.KeyAvailable) {
            if (token.IsCancellationRequested) return;
            Thread.Sleep(8);
        }

        while (Console.KeyAvailable) {
            ConsoleKeyInfo k = Console.ReadKey(true);
            if (token.IsCancellationRequested) return;

            switch (k.Key) {
                case ConsoleKey.Enter:
                    InputSuggestion.HandleEnter();

                    CompleteMessage();
                    if (token.IsCancellationRequested) return;
                    dirtyBuffer = true;
                    break;
                
                case ConsoleKey.Backspace:
                    Cursor.DeleteLeft(inWords: k.Modifiers == ConsoleModifiers.Control);
                    dirtyBuffer = true;
                    break;
                
                case ConsoleKey.Delete:
                    Cursor.DeleteRight(inWords: k.Modifiers == ConsoleModifiers.Control);
                    dirtyBuffer = true;
                    break;
                
                case ConsoleKey.End:
                    Cursor.MoveCursorEnd();
                    dirtyBuffer = true;
                    break;
                
                case ConsoleKey.Home:
                    Cursor.MoveCursorStart();
                    dirtyBuffer = true;

                    break;
                
                case ConsoleKey.LeftArrow:
                    InputBuffer.MoveCursorLeft(inWords: k.Modifiers == ConsoleModifiers.Control);
                    dirtyBuffer = true;
                    break;
                
                case ConsoleKey.RightArrow:
                    InputBuffer.MoveCursorRight(inWords: k.Modifiers == ConsoleModifiers.Control);
                    dirtyBuffer = true;

                    break;
                
                case ConsoleKey.UpArrow:
                    if (ConsoleSuggestion.HandleUpArrow()) break;
                    lock (InternalContext.BackreadBufferLock) {
                        if (ConsoleBuffer.BackreadBuffer.Count == 0) break;

                        var backread = ConsoleBuffer.GetBackreadBackwards();
                        var backreadCopied = ConsoleBuffer.isCurrentBufferCopied;
                        var backreadString = ConsoleBuffer.UserInputBufferCopy;
                        ConsoleBuffer.SetBufferContent(backread);

                        // SetBufferContent clears the backread, so we need to pass it again
                        if (backreadCopied) {
                            ConsoleBuffer.isCurrentBufferCopied = backreadCopied;
                            ConsoleBuffer.UserInputBufferCopy = backreadString;

                            Trace.Assert(ConsoleBuffer.isCurrentBufferCopied);
                        }
                    }

                    dirtyBuffer = true;
                    break;
                
                case ConsoleKey.DownArrow:
                    if (ConsoleSuggestion.HandleDownArrow()) break;
                    lock (InternalContext.BackreadBufferLock) {
                        if (ConsoleBuffer.BackreadBuffer.Count == 0) break;

                        var backread = ConsoleBuffer.GetBackreadForwards();
                        var backreadCopied = ConsoleBuffer.isCurrentBufferCopied;
                        var backreadString = ConsoleBuffer.UserInputBufferCopy;
                        ConsoleBuffer.SetBufferContent(backread);


                        // SetBufferContent clears the backread, so we need to pass it again
                        if (backreadCopied) {
                            ConsoleBuffer.isCurrentBufferCopied = backreadCopied;
                            ConsoleBuffer.UserInputBufferCopy = backreadString;

                            Trace.Assert(ConsoleBuffer.isCurrentBufferCopied);
                        }
                    }

                    dirtyBuffer = true;
                    break;
                
                case ConsoleKey.Tab:
                    ConsoleSuggestion.HandleTab();

                    break;
                
                case ConsoleKey.Escape:
                    ConsoleSuggestion.HandleEscape();

                    break;
                
                case ConsoleKey.P:
                    if (k.Modifiers == ConsoleModifiers.Control)
                        InputBuffer.PrintUserInput();
                    else
                        goto default;

                    break;
                
                default:
                    if (InputBuffer.Insert(k.KeyChar))
                        dirtyBuffer = true;
                    break;
            }

            if (token.IsCancellationRequested) return;
            if (dirtyBuffer) {
                InputBuffer.RedrawInputArea();
                CheckInputBufferUpdate();
            }
        }
    }

    internal override void CompleteMessage() {
        var input = InputBuffer.FlushBuffer();
        InputBuffer.History.AddToHistory(input);
        FireMessageReceived(input.Replace("\0", string.Empty));
    }
}