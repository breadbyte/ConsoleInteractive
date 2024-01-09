using System;
using System.Globalization;

namespace ConsoleInteractive.Interface.Abstract; 

public abstract partial class InputBuffer {
    
    // TODO: Locking
    
    // 
    // PREAMBLE
    //
    // This class represents Buffer operations using the Rune type.
    // This is a fallback class, and should not be used unless absolutely necessary.
    //
    // The majority of users will be using the char operations (ASCII), and CJK users will be using the Rune operations,
    // as a safer way to handle and manipulate CJK characters and strings.
    //
    // In order to prevent inserting a character in the middle of a rune, we use the TextElementEnumerator.
    // The TextElementEnumerator enumerates over the "visible" characters in a string.
    // This is done to prevent the user from inserting characters in the middle of a rune.
    // For example, if the user types "👨‍👩‍👧‍👦" (4 runes), the user should not be able to insert a character between the family.
    // The user should only be able to insert a character at the start or the end of the string.
    // 
    
    public virtual bool AppendSafe(char c) {
        
        // Fail fast if the char is a non-ASCII character, like a control character
        if (!char.IsAsciiLetterOrDigit(c)) return false;
        
        var teEnum = StringInfo.GetTextElementEnumerator(PeekBuffer());
        
        // Move to the end of the buffer
        while (teEnum.MoveNext()) {} // This space intentionally left blank
        
        // Insert the character
        UserInputBuffer.Insert(teEnum.ElementIndex, c);
        
        return true;
    }

    public virtual bool AppendSafe(string str) {
        
        // Fail fast if the string contains non-ASCII characters, like control characters
        foreach (char c in str) {
            if (!char.IsAsciiLetterOrDigit(c)) return false;
        }
        
        var teEnum = StringInfo.GetTextElementEnumerator(PeekBuffer());
        
        // Move to the end of the buffer
        while (teEnum.MoveNext()) {} // This space intentionally left blank
        
        // Insert the string
        UserInputBuffer.Insert(teEnum.ElementIndex, str);
        
        return true;
    }

    public virtual bool InsertSafe(int index, char c) {
        
        // Fail fast if the char is a non-ASCII character, like a control character
        if (!char.IsAsciiLetterOrDigit(c)) return false;
        
        var teEnum = StringInfo.GetTextElementEnumerator(PeekBuffer());
        
        // Move to the specified index
        while (teEnum.MoveNext() && teEnum.ElementIndex < index) {} // This space intentionally left blank
        
        // Insert the character
        UserInputBuffer.Insert(teEnum.ElementIndex, c);
        
        return true;
    }

    public virtual bool InsertSafe(int index, string str) {

        // Fail fast if the string contains non-ASCII characters, like control characters
        foreach (char c in str) {
            if (!char.IsAsciiLetterOrDigit(c)) return false;
        }

        var teEnum = StringInfo.GetTextElementEnumerator(PeekBuffer());

        // Move to the specified index
        while (teEnum.MoveNext() && teEnum.ElementIndex < index) {} // This space intentionally left blank

        // Insert the string
        UserInputBuffer.Insert(teEnum.ElementIndex, str);

        return true;
    }

    public virtual bool ReplaceSafe(Range index, string str) {
        
        // Fail fast if the string contains non-ASCII characters, like control characters
        foreach (char c in str) {
            if (!char.IsAsciiLetterOrDigit(c)) return false;
        }
        
        var teEnum = StringInfo.GetTextElementEnumerator(PeekBuffer());
        
        // Move to the beginning of the range
        while (teEnum.MoveNext() && teEnum.ElementIndex < index.Start.Value) {} // This space intentionally left blank
        var startIndex = teEnum.ElementIndex;
        
        // Move to the end of the range
        while (teEnum.MoveNext() && teEnum.ElementIndex < index.End.Value) {} // This space intentionally left blank
        var endIndex = teEnum.ElementIndex;
        
        // We get the StartIndex and the EndIndex of the range, and we remove the range from the buffer.
        // This ensures that we run over the correct number of runes, and don't insert characters in the middle of a rune.
        
        // Replace the string
        UserInputBuffer.Remove(teEnum.ElementIndex, endIndex - startIndex);
        UserInputBuffer.Insert(teEnum.ElementIndex, str);
        
        return true;
    }
}