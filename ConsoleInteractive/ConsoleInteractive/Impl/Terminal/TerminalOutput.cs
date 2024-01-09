using ConsoleInteractive.Interface.Abstract;

namespace ConsoleInteractive.Interface; 

public class TerminalOutput : OutputWriter, IWindow {
    
    protected InputBuffer InputBuffer { get; }
    protected OutputWriter OutputWriter { get; }
    protected Cursor BufferCursor { get; }
    protected InputPrefix InputPrefix { get; }
    
    int PrintLength { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrefixVisible { get; set; }
    public bool IsOutputVisible { get; set; }
    protected TerminalOutput(InputBuffer inputBuffer, Cursor bufferCursor, OutputWriter outputWriter, InputPrefix inputPrefix) {
        InputBuffer = inputBuffer;
        BufferCursor = bufferCursor;
        OutputWriter = outputWriter;
        InputPrefix = inputPrefix;
    }
    
    public override void WriteLine(string text) {
        throw new System.NotImplementedException();
    }

    public override void WriteLineFormatted(StringData[] data) {
        throw new System.NotImplementedException();
    }

    public override void Write(string text) {
        throw new System.NotImplementedException();
    }

    public override void WriteFormatted(StringData[] data) {
        throw new System.NotImplementedException();
    }
    
    public void Clear() {
        throw new System.NotImplementedException();
    }

    public void ClearLine() {
        throw new System.NotImplementedException();
    }

    public void SetCursorPos(int x, int y) {
        throw new System.NotImplementedException();
    }

    public void SetCursorPos(Cursor cursor) {
        throw new System.NotImplementedException();
    }

    public void SetCursorVisible(bool visible) {
        throw new System.NotImplementedException();
    }
}