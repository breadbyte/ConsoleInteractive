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

    public FormattedStringBuilder AppendMarkup(string text) {
        var matches = InternalContext.FormatRegex.Matches(text);

        // No markup can be parsed from the text, treat it as a regular string.
        if (matches.Count == 0) {
            return Append(text);
        }

        // Check if the string starts with an identifier character.
        // If not, then add the text before the identifier character normally.
        var idx = text.IndexOf('§');
        if (idx > 0) {
            strings.Add(new StringData(text[..idx]));
        }

        Color? currentColor = null;
        Formatting currentFormatting = Formatting.None;
        // match.Groups[0] = The entire matched part
        // match.Groups[1] = The identifier
        // match.Groups[2] = The string after the identifier
        foreach (Match match in matches) {
            
            if (match.Groups[1].Value == "§r") {
                currentColor = null;
                currentFormatting = Formatting.None;
            }

            if (match.Groups[1].Value == "§k") {
                // Square character in place of §k.
                strings.Add(new StringData(new string('\u2588', match.Groups[2].Length), false, null, currentColor, currentFormatting));
            }
            
            // Parse the current line formatting.
            // Get the character after the identifier character.
            var result = FormattingResult.GetFormattingTypeFromFormattingLetter(match.Groups[1].Value[1]);
            if (result.IsValidFormatting) {
                if (result.IsColorFormatting)
                    currentColor = result.TextColor!.Value;
                if (result.IsTextFormatting) {
                    if (result.Formatting == Formatting.None)
                        currentFormatting = Formatting.None;
                    else
                        currentFormatting |= result.Formatting;
                }
            }
            
            // Don't need to add the text if the identifier does not have text associated with it.
            if (match.Groups[2].Length == 0)
                continue;

            strings.Add(new StringData(match.Groups[2].Value, false, null, currentColor, currentFormatting));
        }

        return this;
    }

    public FormattedStringBuilder AppendTerminalCodeMarkup(string text) {
        var matches = InternalContext.ColorCodeRegex.Matches(text);
        
        // No markup can be parsed from the text, treat it as a regular string.
        if (matches.Count == 0) {
            return Append(text);
        }
        
        // Check if the string starts with the identifier character, \u001B (esc) in this case.
        // If not, then add the text before the identifier character normally.
        var idx = text.IndexOf('\u001b');
        if (idx > 0) {
            strings.Add(new StringData(text[..idx]));
        }
        
        Color? currentColor = null;
        Color? currentbgColor = null;
        Formatting currentFormatting = Formatting.None;

        // match.Groups[0] = The entire matched part
        // match.Groups[1] = The code identifier (for formatting or color type)
        // match.Groups[2] = The string after the identifier
        foreach (Match match in matches) {
            var colorValue = match.Groups[2].Value;

            // If the color value is longer than 1, i.e. it doesn't match [0m or [3m
            // we're probably working with something like [38;2;12;12;12m
            if (colorValue.Length > 1) {
                // 38 is the code for Foreground Color
                if (colorValue.StartsWith("38")) {
                    var splitted = colorValue.Split(';');
                    currentColor = new(Convert.ToByte(splitted[2]), Convert.ToByte(splitted[3]), Convert.ToByte(splitted[4]));
                }

                // 48 is the code for the Background Color.
                else if (colorValue.StartsWith("48")) {
                    var splitted = colorValue.Split(';');
                    currentbgColor = new(Convert.ToByte(splitted[2]), Convert.ToByte(splitted[3]), Convert.ToByte(splitted[4]));
                }
            }
            else {
                // If the color value is a single character however,
                // it is most likely a formatting code.
                switch (Convert.ToInt32(colorValue)) {
                    case 0:
                        currentFormatting = Formatting.None;
                        break;
                    case 1:
                        currentFormatting |= Formatting.Bold;
                        break;
                    case 3:
                        currentFormatting |= Formatting.Italic;
                        break;
                    case 4:
                        currentFormatting |= Formatting.Underline;
                        break;
                    case 9:
                        currentFormatting |= Formatting.Strikethrough;
                        break;
                    default:
                        Debug.WriteLine($"Unknown formatting code {colorValue}. Skipping processing.");
                        break;
                }
            }

            // Don't need to add the text if the identifier does not have text associated with it.
            if (match.Groups[3].Value.Length == 0)
                continue;
            
            strings.Add(new StringData(match.Groups[3].Value, false, currentbgColor, currentColor, currentFormatting));
        }

        return this;
    }

    public FormattedStringBuilder MarkupAndColorCodeMarkup() {
        // TODO:
        // To allow both Markup and Color Code Markup,
        // make sure to AppendMarkup first (this processes the § color codes into StringData),
        // then call AppendTerminalCodeMarkup with Build().
        // This will allow the code to process the § color codes first,
        // and the VT color codes lastly.
        throw new NotImplementedException();
    }

    public FormattedStringBuilder AppendLineMarkup(string text) {
        return AppendMarkup(text + Environment.NewLine);
    }

    public FormattedStringBuilder Append(string text, Color? foregroundColor = null, Color? backgroundColor = null, Formatting formatting = Formatting.None) {
        strings.Add(new StringData(text, false, backgroundColor, foregroundColor, formatting));
        return this;
    }
    
    public FormattedStringBuilder AppendLine(string text, Color? foregroundColor = null, Color? backgroundColor = null, Formatting formatting = Formatting.None) {
        strings.Add(new StringData(text, true, backgroundColor, foregroundColor, formatting));
        return this;
    }
    public FormattedStringBuilder AppendLine() {
        strings.Add(new StringData(Environment.NewLine, true));
        return this;
    }

    public string Flatten() {
        StringBuilder internalStringBuilder = new StringBuilder();

        foreach (StringData strData in strings) {
            internalStringBuilder.Append(strData.Build());
        }

        var retval = internalStringBuilder.ToString();
        internalStringBuilder.Clear();
        
        return retval;
    }

    // Expand the data into individual lines to print.
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
                // This is the finalization stage, so no new strings will be appended to the string
                // and we can safely finalize the last string into an AppendNewLine.
                
                var lastItem = currentList.Last();
                currentList.RemoveAt(currentList.Count - 1);
                currentList.Add(new StringData(lastItem.Text, true, lastItem.BackgroundColor, lastItem.ForegroundColor, lastItem.Formatting));
                expanded.Add(currentList);
            }
        }

        return expanded;

        void AppendToListWithSplit(string rawStr, StringData dataToCopy) {
            foreach (var str in rawStr.SplitNewLines()) {
                currentList.Add(new StringData(str, false, dataToCopy.BackgroundColor, dataToCopy.ForegroundColor, dataToCopy.Formatting));
            }
        }
    }
}