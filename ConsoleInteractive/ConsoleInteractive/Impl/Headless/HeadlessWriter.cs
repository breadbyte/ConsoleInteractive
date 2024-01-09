using ConsoleInteractive.Interface.Abstract;

namespace ConsoleInteractive.Impl.Headless; 

public class HeadlessWriter : OutputWriter {
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
}