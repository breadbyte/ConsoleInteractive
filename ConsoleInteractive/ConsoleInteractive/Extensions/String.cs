using System.Collections.Generic;

namespace ConsoleInteractive.Extensions; 

public static class String {

    /// <summary>
    /// Split a string into their individual strings, splitted by new lines.
    /// </summary>
    public static List<string> SplitNewLines(this string str) {
        // NB: Windows uses \r\n newlines, and this is reflected in Environment.NewLine.
        // We use Environment.NewLine in ConsoleInteractive, but other applications
        // may opt to use \n for all platforms instead of Environment.NewLine.
        // We need to adjust for this, so we need to split the string multiple times with both newline types.
        // \r is not accounted for, as \r is a rare newline type and is generally not encountered anymore.
        const string WindowsNewLine = "\\r\\n";
        const string LinuxNewLine = "\\n";

        // Make sure to split windows newlines first, as they are \r\n
        
        var rnSplit = str.Split(WindowsNewLine);
        List<string> nSplit = new();

        foreach (var unsplitted in rnSplit) {
            foreach (var splitted in unsplitted.Split(LinuxNewLine)) {
                nSplit.Add(splitted);
            }
        }

        // Remove empty strings (i.e. newlines at the start and end of the string get counted as an empty string)
        // A newline at the end of the string is not needed anyways as a single string counts as a line.
        // TODO: Do we really need to?
        // nSplit.RemoveAll(x => x.Length == 0);

        return nSplit;
    }
}