using System;
using System.Text;
using System.Threading;

namespace ConsoleInteractive.Buffer {
    internal class VerticalBuffer : IBuffer {
        internal static StringBuilder UserInputBuffer = new();
        private static volatile int CurrentBufferPos = 0;
        
        // arbitrary value so we don't overload the console with input
        private const int VerticalBufferLimit = 3;
        private static volatile int CurrentVerticalBuffer = 1;
        private static volatile int VerticalBufferStart = 0;
        
        // horizontal limit
        private static volatile int ConsoleWriteLimit = InternalContext.CursorLeftPosLimit - 1;
        public void Insert(char c) {

            // TODO Insert when top pos limit reached
            // probably just let the console handle it automatically?
            if (InternalContext.CursorLeftPos == ConsoleWriteLimit) {
                
                // If we've reached the current vertical limit
                if (CurrentVerticalBuffer == VerticalBufferLimit) {
                    Interlocked.Increment(ref VerticalBufferStart);
                    UserInputBuffer.Append(c);
                    Interlocked.Increment(ref CurrentBufferPos);
                    
                    // Reset to the initial position of the input buffer.
                    Console.SetCursorPosition(0, CurrentVerticalBuffer - (InternalContext.CursorTopPos + 1));
                    RedrawInput(0);

                    // Return to our previous position.
                    Console.SetCursorPosition(InternalContext.CursorLeftPos, InternalContext.CursorTopPos);
                    return;
                }
                // If we haven't reached the current vertical limit yet
                // Increment the vbuffer
                else {
                    Console.Write(c);
                    InternalContext.IncrementTopPos();
                    InternalContext.SetLeftCursorPosition(0);
                    Interlocked.Increment(ref CurrentVerticalBuffer);
                }
            }
            else {
                Console.Write(c);
                InternalContext.IncrementLeftPos();
            }

            UserInputBuffer.Append(c);
            Interlocked.Increment(ref CurrentBufferPos);
        }

        public void RedrawInput(int leftCursorPosition){
            Console.Write(UserInputBuffer.ToString()[VerticalBufferStart..(VerticalBufferLimit * InternalContext.CursorLeftPosLimit + (VerticalBufferStart - 1))]);
         }

        public void MoveCursorForward() {
            throw new System.NotImplementedException();
        }

        public void MoveCursorBackward() {
            if (CurrentBufferPos == 0)
                return;
            
            Interlocked.Decrement(ref CurrentBufferPos);
            
            // If we're at the end of a console line
            if (InternalContext.CursorLeftPos == 0) {
                // If we're at the start of the buffer, start moving the buffer backwards
                if (CurrentVerticalBuffer == 1) {
                    Interlocked.Decrement(ref VerticalBufferStart);
                    RedrawInput(0);
                    InternalContext.SetCursorPosition(0, InternalContext.CursorTopPos);
                }

                // We're traversing through the vbuffer.
                else {
                    Interlocked.Decrement(ref CurrentVerticalBuffer);
                    InternalContext.DecrementTopPos();
                    InternalContext.SetLeftCursorPosition(InternalContext.CursorLeftPosLimit - 1);
                    return;
                }
            }
            
            InternalContext.DecrementLeftPos();
        }

        public void RemoveForward() {
            throw new System.NotImplementedException();
        }

        public void RemoveBackward() {
            throw new System.NotImplementedException();
        }

        public string FlushBuffer() {
            throw new System.NotImplementedException();
        }

        public void ClearVisibleUserInput() {
            throw new System.NotImplementedException();
        }

        public void MoveToStartBufferPosition() {
            throw new System.NotImplementedException();
        }

        public void MoveToEndBufferPosition() {
            throw new System.NotImplementedException();
        }
    }
}