namespace ConsoleInteractive {
    internal interface IBuffer {
        public void Insert(char c);
        public void RedrawInput(int leftCursorPosition);
        public void MoveCursorForward();
        public void MoveCursorBackward();
        public void RemoveForward();
        public void RemoveBackward();
        public string FlushBuffer();
        public void ClearVisibleUserInput();
        public void MoveToStartBufferPosition();
        public void MoveToEndBufferPosition();
    }
}