using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ConsoleInteractive.Interface.Abstract; 

public abstract partial class InputBuffer {
    
    //
    // PREAMBLE
    //
    // This class represents Buffer operations using the char type.
    // 
    // The char operations are preferred over the Rune operations,
    // due to the fact that less checks and operations are done on the character.
    //
    // Rune is slightly more expensive than char, but it is more flexible.
    //
    
    /// <summary>
    /// Append a single character to the buffer.
    /// </summary>
    /// <param name="c">The character to append.</param>
    /// <returns>True if the character was appended, false otherwise.</returns>
    public virtual bool Append(char c) {

        // Move to Rune operations if the character is not ASCII
        if (!AsciiMode)
            return AppendSafe(c);
        
        // Fail fast if the char is a non-ASCII character, like a control character
        if (!char.IsAsciiLetterOrDigit(c)) return false;
        
        UserInputBuffer.Append(c);
        return true;
    }

    /// <summary>
    /// Append a string to the buffer.
    /// </summary>
    /// <param name="str">The string to append.</param>
    /// <returns>True if the string was appended, false otherwise.</returns>
    public virtual bool Append(string str) {
        
        // Move to Rune operations if the character is not ASCII
        if (!AsciiMode)
            return AppendSafe(str);
        
        // Fail fast if the string contains non-ASCII characters, like control characters
        foreach (char c in str) {
            if (!char.IsAsciiLetterOrDigit(c)) return false;
        }
        
        UserInputBuffer.Append(str);
        return true;
    }

    /// <summary>
    /// Insert a single character at the specified index.
    /// </summary>
    /// <param name="index">The index to insert the character at.</param>
    /// <param name="c">The character to insert.</param>
    /// <returns>True if the character was inserted, false otherwise.</returns>
    public virtual bool Insert(int index, char c) {
                
        // Move to Rune operations if the character is not ASCII
        if (!AsciiMode)
            return InsertSafe(index, c);
        
        // Fail fast if the string contains non-ASCII characters, like control characters
        if (!char.IsAsciiLetterOrDigit(c)) return false;

        UserInputBuffer.Insert(index, c);
        return true;
    }

    /// <summary>
    /// Insert a string at the specified index.
    /// </summary>
    /// <param name="index">The index to insert the string at.</param>
    /// <param name="str">The string to insert.</param>
    /// <returns>True if the string was inserted, false otherwise.</returns>
    public virtual bool Insert(int index, string str) {
                
        // Move to Rune operations if the character is not ASCII
        if (!AsciiMode)
            return InsertSafe(index, str);
        
        // Fail fast if the string contains non-ASCII characters, like control characters
        foreach (char c in str) {
            if (!char.IsAsciiLetterOrDigit(c)) return false;
        }
        
        UserInputBuffer.Insert(index, str);
        return true;
    }

    /// <summary>
    /// Replace the buffer content at a specified range.
    /// </summary>
    /// <param name="index">The range to replace.</param>
    /// <param name="str">The string to replace the range with.</param>
    /// <returns>True if the range was replaced, false otherwise.</returns>
    public virtual bool Replace(Range index, string str) {
                
        // Move to Rune operations if the character is not ASCII
        if (!AsciiMode)
            return ReplaceSafe(index, str);
        
        // Fail fast if the string contains non-ASCII characters, like control characters
        foreach (char c in str) {
            if (!char.IsAsciiLetterOrDigit(c)) return false;
        }
        
        UserInputBuffer.Remove(index.Start.Value, index.End.Value - index.Start.Value);
        UserInputBuffer.Insert(index.Start.Value, str);
        return true;
    }
}