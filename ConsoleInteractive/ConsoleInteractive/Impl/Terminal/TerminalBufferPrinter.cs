using ConsoleInteractive.Interface.Abstract;

namespace ConsoleInteractive.Impl.Terminal; 

public class TerminalBufferPrinter : InputBufferPrinter {
    public TerminalBufferPrinter(InputBuffer inputBuffer, Cursor bufferCursor, OutputWriter outputWriter, InputPrefix inputPrefix) : base(inputBuffer, bufferCursor, outputWriter, inputPrefix) { }

    public override void PrintBuffer() {
        lock (InputBuffer.UserInputBufferLock) {
            lock (BufferCursor.CursorLock) {
                lock (OutputWriter.WriterLock) {
                    
                    // If the buffer cursor is at the end of the buffer, we can just print the buffer
                    // as is.
                    if (BufferCursor.CursorPosition == InputBuffer.UserInputBufferLength) {
                        OutputWriter.WriteLine(InputBuffer.PeekBuffer()[BufferCursor.CursorPosition..PrintLength]);
                        return;
                    }
                    
                    // If the buffer cursor is at the start of the buffer, we can just print the buffer
                    // as is.
                    if (BufferCursor.CursorPosition == 0) {
                        OutputWriter.WriteLine(InputBuffer.PeekBuffer()[..PrintLength]);
                        return;
                    }
                    
                    // If the buffer cursor is not at the start or end of the buffer, we need to
                    // print the buffer from the buffer cursor position to the end of the buffer.
                    OutputWriter.WriteLine(InputBuffer.PeekBuffer()[BufferCursor.CursorPosition..]);
                }
            }
        }
    }
}