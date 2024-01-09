namespace ConsoleInteractive.Impl.Terminal; 

public partial class TerminalBuffer {
    /// <summary>
    /// Get the length of the word before the current cursor, used for Ctrl+Backspace.
    /// </summary>
    private int GetLengthOfPreviousWord(bool inWords) {
        int startIndex = CursorPosition - 1;
        if (inWords) {
            int index = startIndex;
            while (index >= 0 && char.IsWhiteSpace(UserInputBuffer[index])) --index;
            if (index >= 0) {
                if (UserInputBuffer[index] == '\0') --index;
                if (char.IsLetterOrDigit(UserInputBuffer[index])) {
                    do { --index; }
                    while (index >= 0 && (UserInputBuffer[index] == '\0'
                                          || char.IsLetterOrDigit(UserInputBuffer[index])));
                } else if (char.IsSymbol(UserInputBuffer[index]) || char.IsPunctuation(UserInputBuffer[index])) {
                    do { --index; }
                    while (index >= 0 && (UserInputBuffer[index] == '\0'
                                          || char.IsSymbol(UserInputBuffer[index])
                                          || char.IsPunctuation(UserInputBuffer[index])));
                }
            }
            if (index < startIndex && UserInputBuffer[index + 1] == '\0') ++index;
            if (index != startIndex) return startIndex - index;
        }
        return (UserInputBuffer[startIndex] == '\0') ? 2 : 1;
    }
    
    /// <summary>
    /// Get the length of the word after the current cursor, used for Ctrl+delete.
    /// </summary>
    private int GetLengthOfNextWord(bool inWords) {
        int startIndex = CursorPosition;
        if (inWords) {
            int index = CursorPosition;
            if (char.IsLetterOrDigit(UserInputBuffer[index])) {
                do { ++index; }
                while (index < UserInputBuffer.Length && (UserInputBuffer[index] == '\0' || char.IsLetterOrDigit(UserInputBuffer[index])));
            } else if (char.IsSymbol(UserInputBuffer[index]) || char.IsPunctuation(UserInputBuffer[index])) {
                do { ++index; }
                while (index < UserInputBuffer.Length && (UserInputBuffer[index] == '\0'
                                                          || char.IsSymbol(UserInputBuffer[index])
                                                          || char.IsPunctuation(UserInputBuffer[index])));
            }
            while (index < UserInputBuffer.Length && char.IsWhiteSpace(UserInputBuffer[index])) ++index;
            if (index != startIndex) return index - startIndex;
        }
        return (startIndex + 1 < UserInputBuffer.Length && UserInputBuffer[startIndex + 1] == '\0') ? 2 : 1;
    }
}