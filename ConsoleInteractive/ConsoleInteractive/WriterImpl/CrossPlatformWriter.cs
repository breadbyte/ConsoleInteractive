using System;
using System.Collections.Generic;
using System.Text;
using static ConsoleInteractive.WriterImpl.BufferHelper;

namespace ConsoleInteractive.WriterImpl; 

public class CrossPlatformWriter : WriterBase {
    protected override void InternalWrite(StringData data) {
        WriteToConsole(data.Build(false), DetermineLineCount(data.Text.Length));
    }

    protected override void InternalWriteStringDataChain(List<StringData> data) {
        int chainLength = 0;
        StringBuilder completeString = new();

        foreach (var strData in data) {
            chainLength += strData.Text.Length;
            completeString.Append(strData.Build(false));
        }
            
        WriteToConsole(completeString.ToString(), DetermineLineCount(chainLength));
    }

    protected override void InternalWriteUnsafe(string data) {
        WriteToConsole(data, DetermineLineCount(data.Length));

    }
    
    
    private static void WriteToConsole(string text, int linesAdded) {
        // The final chain in the write chain, this outputs the string to the console.
        // Limitation: Does not accept newlines. Will break if you pass in a string with newlines into it.
        // Newlines have to be pre-processed beforehand.
        //
        // This segment has to be detached from WriteInternal, as VT-coded text requires additional color codes to be added to the text.
        // This has the unfortunate side effect of polluting the linesAdded variable, as the color codes are also counted in,
        // despite being invisible, as they only affect the terminal graphics.
        // Therefore, to be able to separate colored text from non-colored text, we need to create a separate function
        // in which only the text positioning code remains.
        
        lock (InternalContext.WriteLock) {
            // Stash the buffer state.
            // When the buffer is active, this will store it's current state.
            __StashBufferState();
                
            Console.WriteLine(text);
                
            // This will calculate the cursor's next position.
            __CalculateNextCursorTopPosition(linesAdded);
                
            // Restore the buffer state.
            // When the buffer is active, this will rebuild the user input buffer on the screen.
            __RestoreBufferState();
        }
    }
}