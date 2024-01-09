namespace ConsoleInteractive.Interface.Abstract; 

public class Cursor {
    
    //
    // PREAMBLE
    //
    // This class provides a generic position tracker 
    // that can be used by any class that needs it.
    
    internal readonly object CursorLock = new();     // The cursor lock
    internal int CursorPosition { get; set; }        // The cursor position
    
    #region Cursor movement
    
    // Right arrow key
    public void IncrementCursorPos() {
        lock (CursorLock)
            CursorPosition++;
    }
    
    // Left arrow key
    public void DecrementCursorPos() {
        lock (CursorLock)
            CursorPosition--;
    } 
    
    #endregion
}