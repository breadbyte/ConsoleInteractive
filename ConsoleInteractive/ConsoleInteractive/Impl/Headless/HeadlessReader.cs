using ConsoleInteractive.Interface;
using ConsoleInteractive.Interface.Abstract;

namespace ConsoleInteractive.Impl.Headless; 

public class HeadlessReader : InputReader {
    public override string RequestImmediateInput(bool password = false) {
        throw new System.NotImplementedException();
    }

    public override void BeginReaderThread() {
        throw new System.NotImplementedException();
    }

    public override void StopReaderThread() {
        throw new System.NotImplementedException();
    }

    public override void KeyListener(object? cancellationToken) {
        throw new System.NotImplementedException();
    }

    internal override void CompleteMessage() {
        throw new System.NotImplementedException();
    }
}