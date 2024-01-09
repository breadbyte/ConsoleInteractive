using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using ConsoleInteractive.Helper;

namespace ConsoleInteractive.Interface.Abstract; 

public abstract partial class InputBuffer {
    
    //
    // PREAMBLE
    //
    // The InputBuffer is the heart of the ConsoleInteractive library.
    // It represents the buffer that stores the user input.
    // 
    // The buffer itself is a StringBuilder.
    //
    
    StringBuilder UserInputBuffer { get; }
    public int Length => UserInputBuffer.Length;
    
    // If true, only ASCII characters are present in the buffer
    // False if it contains non-ASCII characters (like CJK characters)
    public bool AsciiMode { get; set; } = true;
    
    internal readonly object UserInputBufferLock = new();
    public event EventHandler<string>? OnBufferChanged;
    public event EventHandler<string>? OnBufferFlushed;
    
    #region Buffer Operations
    
    // Initialize the buffer
    public abstract void Init();
    
    // Remove a character at the specified index
    public abstract void RemoveAt(int index);
    
    // Remove a string at the specified index
    public abstract void Remove(int index, int length);
    
    // Remove buffer contents at the specified index
    public abstract void Remove(Index index);
    
    // Set the buffer content
    public abstract void SetBufferContent(string content);
    
    // Flush the buffer content
    public abstract string FlushBuffer();
    
    // Peek at the buffer content
    public abstract string PeekBuffer();
    
    // Clear the buffer content
    public abstract void ClearBuffer();
    
    // Get the buffer
    public abstract Buffer GetBuffer();
    
    // Close the buffer
    public abstract void Close();
    
    #endregion
}