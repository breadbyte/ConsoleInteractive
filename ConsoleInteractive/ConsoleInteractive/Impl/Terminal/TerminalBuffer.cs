using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using ConsoleInteractive.Helper;
using ConsoleInteractive.Interface;
using ConsoleInteractive.Interface.Abstract;
using Wcwidth;

namespace ConsoleInteractive.Impl.Terminal; 

public partial class TerminalBuffer : InputBuffer {
    public InputHistory History { get; }
    public RuneBuilder UserInputBuffer { get; }
    
    public TerminalBuffer(InputHistory history) {
        History = history;
        UserInputBuffer = new();
    }
    

    public override bool Insert(int index, char c) {
        lock (InputBuffer.UserInputBufferLock) {
            lock (Cursor.Lock) {

                InputBuffer.Insert(index, c);
                Cursor.IncrementCursorPosition();
            }
        }
    }
    public override void Replace(Range range, string str) {
        lock (UserInputBufferLock) {
            // Ensure that the range is within the boundaries of the user buffer length.
            var userbufLength = UserInputBuffer.Length;
            if (range.GetOffsetAndLength(userbufLength).Length > userbufLength) return;

            // Determine the text elements in the string to be replaced,
            // and the UserInputBuffer.
            // A text element is defined here:
            // https://learn.microsoft.com/en-us/dotnet/api/system.globalization.stringinfo.gettextelementenumerator?view=net-7.0
            // as a singluar visible rune.
            int[] replacementStringTextElement = StringInfo.ParseCombiningCharacters(str);
            int[] userInputBufferTextElement = StringInfo.ParseCombiningCharacters(UserInputBuffer.ToString());

            // Determine the starting and ending offsets.
            var start = Math.Max(0, range.Start.Value);
            var end = Math.Min(UserInputBuffer.Length, range.End.Value);

            var runeStart = Array.IndexOf(replacementStringTextElement, start);
            var runeEnd = Array.IndexOf(replacementStringTextElement, end);

            Debug.Assert(runeStart >= 0);
            Debug.Assert(runeEnd >= 0);

            UserInputBuffer.Remove(start, end - start);
            UserInputBuffer.Insert(start, str);
        }
    }

    public override void Append(Rune r) {
        UserInputBuffer.Append(r);
    }

    public override void Append(Span<Rune> str) {
        UserInputBuffer.Append(str);
    }

    public override void Insert(int index, Rune r) {
        UserInputBuffer.Append(index, r);
    }

    public override void Insert(int index, Span<Rune> str) {
        foreach (var rune in str) {
            UserInputBuffer.Append(index, rune);
            index++;
        }
    }

    public override void Replace(Range index, Span<Rune> str) {
        throw new NotImplementedException();
    }

    public override void SetBufferContent(string content) {
        lock (UserInputBufferLock) {
            // Clear the current buffer, replace it with the new content,
            // and set the cursor position to the end of the new content.
            UserInputBuffer.Clear();
            UserInputBuffer.Append(content);

            CursorPosition = content.Length;
        }

        History.ClearCurrent();
    }

    public override void Init() {
        throw new NotImplementedException();
    }

    public override void RemoveAt(int index) {
        throw new NotImplementedException();
    }

    public override void Remove(int index, int length) {
        throw new NotImplementedException();
    }

    public override void Remove(Index index) {
        throw new NotImplementedException();
    }

    public override void SetBufferContent(Span<Rune> content) {
        throw new NotImplementedException();
    }

    public override string FlushBuffer() {
        string retval;
        lock (UserInputBufferLock) {
            retval = UserInputBuffer.ToString();

            CursorPosition = 0;
            UserInputBuffer.Clear();
        }

        RedrawInputArea();
        History.ClearCurrent();
        return retval;
    }

    public override string PeekBuffer() {
        throw new NotImplementedException();
    }

    public override void ClearBuffer() {
        throw new NotImplementedException();
    }
    
    public override Buffer GetBuffer() {
        throw new NotImplementedException();
    }

    public override void Close() {
        throw new NotImplementedException();
    }

    public void Insert(char c) {
        throw new NotImplementedException();
    }

    public void CursorDelete() {
        throw new NotImplementedException();
    }
}