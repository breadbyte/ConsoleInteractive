using System;
using System.Threading;

namespace ConsoleInteractive.WriterImpl; 

public static class BufferHelper {
    
    internal static volatile int __WriterInternalPositionState = 0;

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