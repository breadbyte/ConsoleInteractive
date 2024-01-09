using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ConsoleInteractive.Extensions;
using ConsoleInteractive.Interface.Abstract;

namespace ConsoleInteractive.Interface; 

public class InputHistory {
    
    //
    // PREAMBLE
    //
    // This class represents the history of the input buffer.
    // It is used to store the history of the input buffer.
    //
    // The history buffer works as follows:
    // Consider the following behavior:
    //
    // The user inputs the following: 1 2 3 4 5
    // The history now contains the following in a LIFO fashion:
    // 5 <- Last item pushed
    // 4
    // 3
    // 2
    // 1 <- First item pushed
    //
    // The user then presses the up arrow key to go back in history.
    // This increments the history index and returns the item at the index.
    // Up pressed          Up pressed            Up pressed
    // Index[1]            Index[2]              Index[3]
    // 5 <- Current item   5                     5
    // 4                   4 <- Current item     4
    // 3                   3                     3 <- Current item
    // 2                   2                     2
    // 1                   1                     1
    //
    // The user then presses the down arrow key to go forward in history.
    // This decrements the history index and returns the item at the index.
    // Down pressed        Down pressed          Down pressed
    // Index[3]            Index[2]              Index[1]
    // 5                   5                     5 <- Current item
    // 4                   4 <- Current item     4
    // 3 <- Current item   3                     3
    // 2                   2                     2
    // 1                   1                     1
    // Index[0] will present the current buffer content.
    //
    // Popping an item in the middle of the history will move it to the back of the history.
    // Given the following history with Index[3] as the current item:
    // 5
    // 4
    // 3 <- Current item
    // 2
    // 1
    //
    // If the user accepts the current item, the history will be as follows:
    // 3
    // 5
    // 4
    // 2
    // 1
    
    
    
    // The history
    LinkedList<string> History { get; }
    
    // The contents of the buffer before the history was called
    string BufferCurrent { get; set; }
    
    // The history limit and index
    int HistoryLimit { get; set; } = 32;
    
    
    private readonly object Lock = new();
    InputBuffer InputBuffer { get; set; }
    Cursor Cursor { get; set; }
    
    public event EventHandler<string>? OnHistoryChanged;
    
    InputHistory(InputBuffer inputBuffer, Cursor cursor) {
        InputBuffer = inputBuffer;
        Cursor = cursor;
        History = new();
    }
    
    // Add the buffer content to the history
    public void AddToHistory(string message) {
        // If the message is empty, don't add it to the history
        if (message.Length == 0 || string.IsNullOrWhiteSpace(message)) return;
        
        // Lock the history to prevent multiple threads from accessing it at the same time
        lock (Lock) {
            
            // If the message is the same as the last message, don't add it to the history
            if (!message.SequenceEqual(History.Last!.ValueRef)) {
                History.AddLast(message);
                
                // Ensure that the history limit is not exceeded.
                var removeCount = History.Count - HistoryLimit;
                for (int i = 0; i < removeCount; i++)
                    History.RemoveFirst();
            }
        }
    }

    // Up arrow key
    public void MoveHistoryUp() {
        // Lock the history to prevent multiple threads from accessing it at the same time
        lock (Lock) {
            
            // If the history is empty, return (no history)
            if (History.Count == 0) return;
            
            // If the history index is 0, set our current buffer to the current buffer content
            if (Cursor.CursorPosition == 0) {
                BufferCurrent = InputBuffer.FlushBuffer();
            }
            
            IncrementIndexHistory();
            InputBuffer.SetBufferContent(History.ElementAt(Cursor.CursorPosition));
        }
    }

    // Down arrow key
    public void MoveHistoryDown() {
        lock (Lock) {
            // If the history is empty, return (no history)
            if (History.Count == 0) return;
            
            DecrementIndexHistory();
            // If we've reached the end of the history, set the buffer content to the current buffer
            if (Cursor.CursorPosition == 0) {
                InputBuffer.SetBufferContent(BufferCurrent);
            }
            // Otherwise, set the buffer content to the history item
            else {
                InputBuffer.SetBufferContent(History.ElementAt(Cursor.CursorPosition));
            }
        }
    }

    public void ClearHistory() {
        lock (Lock) {
            History.Clear();
            Cursor.CursorPosition = 0;
        }
    }
    public void ClearCurrent() {
        lock (Lock) {
            BufferCurrent = "";
        }
    }
    
    public void IncrementIndexHistory() {
        lock (Lock) {
            if (Cursor.CursorPosition == HistoryLimit) return;
            Cursor.IncrementCursorPos();
        }
    }
    
    public void DecrementIndexHistory() {
        lock (Lock) {
            if (Cursor.CursorPosition == 0) return;
            Cursor.DecrementCursorPos();
        }
    }
}