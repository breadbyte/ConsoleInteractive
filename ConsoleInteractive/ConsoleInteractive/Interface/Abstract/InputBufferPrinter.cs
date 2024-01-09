namespace ConsoleInteractive.Interface.Abstract; 

public abstract class InputBufferPrinter {
    protected InputBuffer InputBuffer { get; set; } // The input buffer attached to this printer
    protected OutputWriter OutputWriter { get; set; } // The writer attached to this printer
    
    // BufferCursor contains the cursor position where we should start printing the buffer.
    // This is useful for when we want to print the buffer in a different location than the
    // current cursor position.

    protected Cursor BufferCursor { get; set; }    // The buffer cursor attached to this printer

    // PrintLength contains the length of the printed buffer. This is useful for when we want
    // to print only a specific length of the buffer.
    InputPrefix InputPrefix { get; set; }           // The input prefix attached to this printer
    
    public abstract void PrintBuffer();

    public void Clear() {
        lock (InternalContext.WriteLock) {
            if (BufferCursor.CursorPosition < OutputWriter.BufferWidth) {
                Console.CursorLeft = startPos;
                Console.Write(new string(' ', Math.Max(0,
                    PrefixTotalLength + Math.Min(UserInputBuffer.Length - BufferOutputAnchor, UserInputBufferMaxLength) - startPos)));
            }
            Console.CursorLeft = 0;
        }
    }
}