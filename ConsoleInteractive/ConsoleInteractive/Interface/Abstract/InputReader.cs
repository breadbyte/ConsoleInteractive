using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ConsoleInteractive.Interface.Abstract; 

public abstract class InputReader {
    protected ConcurrentQueue<char> InputQueue = new();                     // The input queue

    public static event EventHandler<string>? MessageReceived;
    public static event EventHandler<char>? OnCharReceived;
    
    // The input buffer attached to this reader
    public InputBuffer? InputBuffer { get; protected set; }
    
    // The input history attached to this reader
    public InputHistory? InputHistory { get; protected set; }
    
    // The input suggestions attached to this reader
    public IInputAutocomplete? InputSuggestion { get; protected set; }
    
    // The reader thread
    protected Thread? ReaderThread { get; set; }
    
    // The cancellation token source
    protected CancellationTokenSource? CancellationTokenSource { get; set; }

    // Request for input once
    public abstract string RequestImmediateInput(bool password = false);
    
    // Request a password input once
    public void RequestPassword() => RequestImmediateInput(true);
    
    // Start the reader thread
    public abstract void BeginReaderThread();
    
    // Stop the reader thread
    public abstract void StopReaderThread();
    
    // Keypress handler
    public abstract void KeyListener(object? cancellationToken);

    // Complete the message
    internal abstract void CompleteMessage();
    
    internal void FireMessageReceived(string message) => MessageReceived?.Invoke(null, message); // Fire the message received event
    internal void FireOnCharReceived(char c) => OnCharReceived?.Invoke(null, c); // Fire the on char received event
}