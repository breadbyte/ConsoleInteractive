using System;
using System.Collections.Generic;
using System.Threading;

namespace ConsoleInteractive.WriterImpl;

/// <summary>
/// This abstract class is the base class for all writer implementations.
/// </summary>
public abstract class WriterBase {
    
    // Print a simple line to the console.
    // The only reason why this is unsafe is because
    // it skips all text sanitization checks.
    public void WriteUnsafe(string text) {
        __WriteUnsafe(text);
    }
    
    #region For other writers to implement.

    // Writes a StringData.
    public abstract void Write(StringData data);

    // Writes a list of StringDatas in a chain.
    // This allows us to use different colors and formatting for a single line but multiple StringData's.
    public abstract void WriteStringDataChain(List<StringData> data);
    
    
    // Writes a plain string onto the console.
    public virtual void Write(string data) {
        FormattedStringBuilder f = new FormattedStringBuilder();
        f.Append(data);
        Write(f);
    }
    
    // Writes a FormattedStringBuilder to the console.
    public virtual void Write(FormattedStringBuilder data) {
        var splits = data.Expand();

        foreach (var line in splits) {
            WriteStringDataChain(line);
        }
    }
    
    #endregion

    internal static volatile int __WriterInternalPositionState = 0;

    // Directly passes the string to the WriteInternal function.
    // Skips all user input checks.
    // ONLY USE IF you are absolutely certain that your string does not have any data
    // that the checks will clean up, i.e. newlines!
    internal static void __WriteUnsafe(string value) {
        __WriteInternal(value, DetermineLineCount(value.Length));
    }
    
    internal static void __WriteInternal(string text, int linesAdded) {
        // The final chain in the write chain, this outputs the string to the console.
        // Limitation: Does not accept newlines. Will break if you pass in a string with newlines into it.
        // Newlines have to be pre-processed beforehand.
        //
        // This segment has to be detached from WriteInternal, as VT-coded text requires additional color codes to be added to the text.
        // This has the unfortunate side effect of polluting the linesAdded variable, as the color codes are also counted in,
        // despite being invisible, as they only affect the terminal graphics.
        // Therefore, to be able to separate colored text from non-colored text, we need to create a separate function
        // in which only the text positioning code remains.
        
        lock (InternalContext.WriteLock) {
            // Stash the buffer state.
            // When the buffer is active, this will store it's current state.
            __StashBufferState();
                
            Console.WriteLine(text);
                
            // This will calculate the cursor's next position.
            __CalculateNextCursorTopPosition(linesAdded);
                
            // Restore the buffer state.
            // When the buffer is active, this will rebuild the user input buffer on the screen.
            __RestoreBufferState();
        }
    }
    
    internal static void __StashBufferState() {

        // Stashes the cursor state internally.
        // This is used in cases where the current cursor position is higher than 0,
        // because the user has inputted text on the input area.
        // We will require to redraw the contents of the user buffer after redrawing,
        // and so we need to get the previous position of the cursor to restore it.

        // If the buffer is initialized, then we should get the current cursor position
        // because we are potentially writing over user input.
        //
        // 0 otherwise, since we know that there is no user input,
        // so we can start at the beginning.
        if (InternalContext.BufferInitialized) {
            Interlocked.Exchange(ref __WriterInternalPositionState, InternalContext.CurrentCursorLeftPos);
            ConsoleBuffer.ClearVisibleUserInput();
        }
        else
            // Clears the entire line. Not optimal as it also clears blank spaces,
            // but ensures that the entire line is cleared.
            ConsoleBuffer.ClearCurrentLine();
    }
    internal static void __RestoreBufferState() {

        // Only redraw if we have a buffer initialized.
        if (InternalContext.BufferInitialized) {
            ConsoleBuffer.RedrawInput();

            // Need to redraw the prefix manually in cases that RedrawInput() doesn't
            ConsoleBuffer.DrawPrefix();
        }

        // InternalPositionState returns 0 by default.
        // This is only used in cases where the user input buffer is initialized.
        // Otherwise, it is 0.

        InternalContext.SetCursorPosition(__WriterInternalPositionState, InternalContext.CurrentCursorTopPos);
    }
    internal static void __CalculateNextCursorTopPosition(int linesAdded) {

        // Determine if we need to use the previous top position.
        // i.e. vertically constrained.
        if (InternalContext.CurrentCursorTopPos + linesAdded >= InternalContext.CursorTopPosLimit)
            Interlocked.Exchange(ref InternalContext.CurrentCursorTopPos, InternalContext.CursorTopPosLimit - 1);
        else
            Interlocked.Add(ref InternalContext.CurrentCursorTopPos, linesAdded);
    }
    internal static int DetermineLineCount(string value) {
        // TODO: does not detect escape codes.
        // Determine if the current length of the string exceeds the window width, and by how much.
        int linesAdded = 0;
        linesAdded += DetermineLineCount(value.Length);
        return linesAdded;
    }
    internal static int DetermineLineCount(int value) {
        return (Math.Max(0, value - 1) / InternalContext.CursorLeftPosLimit) + 1;
    }
}