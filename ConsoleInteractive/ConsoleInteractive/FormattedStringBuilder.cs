using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleInteractive.Extensions;

namespace ConsoleInteractive; 

public class FormattedStringBuilder {
    public List<StringData> strings = new();

    /// <summary>
    /// Appends markup to the FormattedStringBuilder.
    /// Markup consists of § codes.
    /// </summary>
    public FormattedStringBuilder AppendMarkup(string text) {
        strings = (List<StringData>)strings.Concat(Convert.FromMarkup(text));
        return this;
    }

    /// <summary>
    /// Appends terminal codes to the FormattedStringBuilder.
    /// Used for extensive formatting and coloring in the console.
    /// </summary>
    public FormattedStringBuilder AppendTerminalCode(string text) {
        strings = (List<StringData>)strings.Concat(Convert.FromTerminalCode(text));
        return this;
    }
    
    /// <summary>
    /// Appends both markup and terminal codes to the FormattedStringBuilder.
    /// Used for sanitizing plain string inputs.
    /// </summary>
    public FormattedStringBuilder Append(string text) {
        
        // To allow for both Markup and Color Codes in the same string,
        // make sure to AppendMarkup first (this processes the § markup into StringData),
        // then flatten the entire string to turn everything into color codes.
        // Then, expand everything back into a list of StringData.
        AppendMarkup(text);
        var flat = Flatten();
        
        // Discard the old list of StringData and replace it with the new one.
        strings = new();
        AppendTerminalCode(flat);
        return this;
    }

    /// <summary>
    /// Appends a complete StringData to the FormattedStringBuilder.
    /// </summary>
    public FormattedStringBuilder Append(StringData stringData) {
        strings.Add(stringData);
        return this;
    }

    /// <summary>
    /// Appends a string to the FormattedStringBuilder.
    /// </summary>
    public FormattedStringBuilder Append(string text, Color? foregroundColor = null, Color? backgroundColor = null, FormattingType formattingType = FormattingType.None) {
        SanitizedAppend(text);
        return this;
    }
    
    /// <summary>
    /// Appends a string to the FormattedStringBuilder, adding a NewLine after the string.
    /// </summary>
    public FormattedStringBuilder AppendLine(string text, Color? foregroundColor = null, Color? backgroundColor = null, FormattingType formattingType = FormattingType.None) {
        return Append(text + Environment.NewLine, foregroundColor, backgroundColor, formattingType);
    }

    private FormattedStringBuilder SanitizedAppend(string input) {
        // Split the string into it's individual lines, if we have newlines on the string.
        var newlineSplit = input.SplitNewLines();

        foreach (var split in newlineSplit) {
            strings.Add(new StringData(split, true));
        }

        return this;
    }
    
    // Flatten the StringBuilder into a single string.
    // Only used for transferring data between StringBuilders.
    // Do not print the output to the console, as it can have catastrophic results!
    public string Flatten() {
        StringBuilder internalStringBuilder = new StringBuilder();

        foreach (StringData strData in strings) {
            internalStringBuilder.Append(strData.Build());
        }

        var retval = internalStringBuilder.ToString();
        internalStringBuilder.Clear();
        
        return retval;
    }
    
    /// <summary>
    /// Expand the data into individual lines to print.
    /// Used for printing to the console. 
    /// </summary>
    /// <returns>A List of Lists of StringData. Each individual list is a line. and each StringData is a word in a line.</returns>
    //
    // The first List<> is defining lists of lines,
    // the Lists inside being a list of words in a line.
    public List<List<StringData>> Expand() {

        // Splits the .Append() and .AppendNewLine() in the list of `StringData`s.
            
        List<List<StringData>> expanded = new();
        List<StringData> currentList = new();
            
        for (int i = 0; i < strings.Count; i++) {
            // While the current string is not an append new line,
            // add it to the current list.
            if (strings[i].AppendNewLine == false) {
                AppendToListWithSplit(strings[i].Text, strings[i]);
            }
            else {
                // We have reached the end of the current string, i.e.
                // we can now append a new line.
                AppendToListWithSplit(strings[i].Text, strings[i]);
                expanded.Add(currentList);
                    
                // Make sure to reset the current list.
                currentList = new();
            }
        }
            
        // If there is data in the current list, i.e. the string does not end on an AppendLine,
        // make sure to add the string in as well.
        if (currentList.Count > 0) {
            if (currentList.Last().AppendNewLine)
                expanded.Add(currentList);
            else {
                // If the last item is an Append, make sure to append our own newline at the end.
                // This is the finalization stage, so no new strings will be appended to the string anyways
                // and we can safely finalize the last string into an AppendNewLine.
                
                var lastItem = currentList.Last();
                currentList.RemoveAt(currentList.Count - 1);
                currentList.Add(new StringData(lastItem.Text, true, lastItem.BackgroundColor, lastItem.ForegroundColor, lastItem.FormattingType));
                expanded.Add(currentList);
            }
        }

        return expanded;

        void AppendToListWithSplit(string rawStr, StringData dataToCopy) {
            foreach (var str in rawStr.SplitNewLines()) {
                currentList.Add(new StringData(str, false, dataToCopy.BackgroundColor, dataToCopy.ForegroundColor, dataToCopy.FormattingType));
            }
        }
    }
}