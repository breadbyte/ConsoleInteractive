namespace ConsoleInteractive.Interface.Abstract; 

public abstract class OutputWriter {
    
    //
    // PREAMBLE
    //
    // This class provides a generic output writer
    // that can be used by any class that needs it.
    //
    // This class does not include any methods for window-based operations.
    // For that, see the IWindow class.
    //
    
    internal readonly object WriterLock = new();

    public abstract void WriteLine(string text);
    public abstract void WriteLineFormatted(StringData[] data);

    public abstract void Write(string text);
    public abstract void WriteFormatted(StringData[] data);
}