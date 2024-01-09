using ConsoleInteractive.Interface.Abstract;

namespace ConsoleInteractive.Interface; 

public abstract class KeyboardShortcuts {
    public InputBuffer Buffer { get; }
    public Cursor Cursor { get; }
    public InputHistory History { get; }

    public KeyboardShortcuts(InputBuffer buffer, Cursor cursor, InputHistory history) {
        Buffer = buffer;
        Cursor = cursor;
        History = history;
    }

    // Del
    public void DeleteCharToRightOfCursor() {
        lock (Buffer.UserInputBufferLock) {
            lock (Cursor.CursorLock) {

                // If we're at the end of the buffer, do nothing.
                if (Cursor.CursorPosition >= Buffer.UserInputBufferLength)
                    return;

                Buffer.RemoveAt(Cursor.CursorPosition);
            }
        }
    }
    // Backspace
    public void DeleteCharToLeftOfCursor() {
        lock (Buffer.UserInputBufferLock) {
            lock (Cursor.CursorLock) {
                
                // If we're at the start of the buffer, do nothing.
                if (Cursor.CursorPosition <= 0)
                    return;
                
                Buffer.RemoveAt(Cursor.CursorPosition - 1);
                Cursor.DecrementCursorPos();
            }
        }
    }
    
    // Ctrl + Delete
    public void DeleteWordToRightOfCursor();
    // Ctrl + Backspace
    public void DeleteWordToLeftOfCursor();
    
    // Home
    public void MoveCursorToStart() {
        lock (Cursor.CursorLock)
            Cursor.CursorPosition = 0;
    }
    // End
    public void MoveCursorToEnd() {
        lock (Cursor.CursorLock)
            Cursor.CursorPosition = Buffer.UserInputBufferLength;
    }
    
    // Right
    public void MoveCursorRight() {
        lock (Buffer.UserInputBufferLock) {
            // If we're at the end of the buffer, do nothing.
            if (Cursor.CursorPosition == Buffer.UserInputBufferLength)
                return;

            Cursor.CursorPosition += GetLengthOfNextWord(inWords);
        }
    }

    // Left
    public void MoveCursorLeft() {
        lock (Buffer.UserInputBufferLock) {
            // If we're at the beginning of the buffer, do nothing.
            if (Cursor.CursorPosition == 0)
                return;

            Cursor.CursorPosition -= GetLengthOfPreviousWord(inWords);
        }
    }

    // Ctrl + Left   
    public void MoveCursorToPreviousWord();
    // Ctrl + Right
    public void MoveCursorToNextWord();
    
    // Up
    public void GoToPreviousHistory();
    // Down
    public void GoToNextHistory();
    
    // PgUp
    public void MoveViewportUp();
    // PgDn
    public void MoveViewportDown();
}