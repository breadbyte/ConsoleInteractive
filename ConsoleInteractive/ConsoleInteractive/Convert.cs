using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ConsoleInteractive.Extensions;
using static System.Convert;

namespace ConsoleInteractive; 

public static class Convert {
    public static List<StringData> FromMarkup(string input) {
        List<StringData> output = new();
        
        var split = input.SplitNewLines();

        foreach (var newlineSplit in split) {
            var matches = InternalContext.FormatRegex.Matches(newlineSplit);

            // No markup can be parsed from the text, treat it as a regular string.
            if (matches.Count == 0) {
                output.Add(new StringData(newlineSplit));
                continue;
            }

            // Check if the string starts with an identifier character.
            // If not, then add the text before the identifier character normally.
            var idx = newlineSplit.IndexOf('§');
            if (idx > 0) {
                output.Add(new StringData(newlineSplit[..idx]));
            }

            Color? currentColor = null;
            FormattingType currentFormattingType = FormattingType.None;
            // match.Groups[0] = The entire matched part
            // match.Groups[1] = The identifier
            // match.Groups[2] = The string after the identifier
            for (var i = 0; i < matches.Count; i++) {
                var match = matches[i];
                if (match.Groups[1].Value == "§r") {
                    currentColor = null;
                    currentFormattingType = FormattingType.None;
                }

                if (match.Groups[1].Value == "§k") {
                    // Square character in place of §k.
                    output.Add(new StringData(new string('\u2588', match.Groups[2].Length), false, null, currentColor,
                        currentFormattingType));
                }

                // Parse the current line formatting.
                // Get the character after the identifier character.
                var result = FormattingResult.GetFormattingTypeFromFormattingLetter(match.Groups[1].Value[1]);
                if (result.IsValidFormatting) {
                    if (result.IsColorFormatting)
                        currentColor = result.TextColor!.Value;
                    if (result.IsTextFormatting) {
                        if (result.FormattingType == FormattingType.None)
                            currentFormattingType = FormattingType.None;
                        else
                            currentFormattingType |= result.FormattingType;
                    }
                }

                // Don't need to add the text if the identifier does not have text associated with it.
                if (match.Groups[2].Length == 0)
                    continue;

                // If we're at the end of the string, append a newline.
                if (i == matches.Count - 1) {
                    output.Add(new StringData(match.Groups[2].Value, true, null, currentColor, currentFormattingType));
                }
                else {
                    output.Add(new StringData(match.Groups[2].Value, false, null, currentColor, currentFormattingType));
                }
            }
        }

        return output;
    }

    public static List<StringData> FromTerminalCode(string input) {
        List<StringData> output = new();
        var split = input.SplitNewLines();

        foreach (var newlineSplit in split) {
            var matches = InternalContext.ColorCodeRegex.Matches(newlineSplit);

            // No markup can be parsed from the text, treat it as a regular string.
            if (matches.Count == 0) {
                output.Add(new StringData(newlineSplit));
                continue;
            }

            // Check if the string starts with the identifier character, \u001B (esc) in this case.
            // If not, then add the text before the identifier character normally.
            var idx = newlineSplit.IndexOf('\u001b');
            if (idx > 0) {
                output.Add(new StringData(newlineSplit[..idx]));
            }

            Color? currentColor = null;
            Color? currentbgColor = null;
            FormattingType currentFormattingType = FormattingType.None;


            int cnt = 0;
            // match.Groups[0] = The entire matched part
            // match.Groups[1] = The code identifier (for formatting or color type)
            // match.Groups[2] = The string after the identifier
            foreach (Match match in matches) {
                cnt++;
                var colorValue = match.Groups[2].Value;

                // If the color value is longer than 1, i.e. it doesn't match [0m or [3m
                // we're probably working with something like [38;2;12;12;12m
                if (colorValue.Length > 1) {
                    // 38 is the code for Foreground Color
                    if (colorValue.StartsWith("38")) {
                        var splitted = colorValue.Split(';');
                        currentColor = new(ToByte(splitted[2]), ToByte(splitted[3]),
                            ToByte(splitted[4]));
                    }

                    // 48 is the code for the Background Color.
                    else if (colorValue.StartsWith("48")) {
                        var splitted = colorValue.Split(';');
                        currentbgColor = new(ToByte(splitted[2]), ToByte(splitted[3]),
                            ToByte(splitted[4]));
                    }
                }
                else {
                    // If the color value is a single character however,
                    // it is most likely a formatting code.
                    switch (ToInt32(colorValue)) {
                        case 0:
                            currentFormattingType = FormattingType.None;
                            break;
                        case 1:
                            currentFormattingType |= FormattingType.Bold;
                            break;
                        case 3:
                            currentFormattingType |= FormattingType.Italic;
                            break;
                        case 4:
                            currentFormattingType |= FormattingType.Underline;
                            break;
                        case 9:
                            currentFormattingType |= FormattingType.Strikethrough;
                            break;
                        default:
                            Debug.WriteLine($"Unknown formatting code {colorValue}. Skipping processing.");
                            break;
                    }
                }

                // Don't need to add the text if the identifier does not have text associated with it.
                if (match.Groups[3].Value.Length == 0)
                    continue;

                if (cnt == matches.Count - 1) {
                    output.Add(new StringData(match.Groups[3].Value, true, currentbgColor, currentColor, currentFormattingType));
                }
                else {
                    output.Add(new StringData(match.Groups[3].Value, false, currentbgColor, currentColor, currentFormattingType));
                }
            }
        }
        
        return output;
    }
}