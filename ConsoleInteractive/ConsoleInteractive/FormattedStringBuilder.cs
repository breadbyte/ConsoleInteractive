using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleInteractive.Extension;
using PInvoke;

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
            var result = GetFormattingTypeFromFormattingLetter(match.Groups[1].Value[1]);
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

    [Flags]
    public enum Formatting {
        None            =      0, // None formatting should be treated the same as Reset.
        Obfuscated      = 1 << 0,
        Bold            = 1 << 1,
        Strikethrough   = 1 << 2,
        Underline       = 1 << 3,
        Italic          = 1 << 4
    }

    public struct StringData {
        public StringData(string text, bool appendNewLine = false, Color? backgroundColor = null, Color? foregroundColor = null, Formatting formatting = FormattedStringBuilder.Formatting.None) {
            this.Text = text;
            this.BackgroundColor = backgroundColor;
            this.ForegroundColor = foregroundColor;
            this.Formatting = formatting;
            this.AppendNewLine = appendNewLine;
        }
        
        public string Text { get; }
        public bool AppendNewLine { get; }
        public Color? BackgroundColor { get; }
        public Color? ForegroundColor { get; }
        public Formatting Formatting { get; } = FormattedStringBuilder.Formatting.None;

        public string Build() {
            StringBuilder internalStringBuilder = new StringBuilder();

            // Step 1: Process formatting if available
            // Append the formatting first before the color,
            // because the None formatting type sends a reset code
            // to prevent the previous formatting and color from getting carried over.
            if (Formatting.HasFlag(Formatting.None))
                internalStringBuilder.Append($"\u001B[0m");
            if (Formatting.HasFlag(Formatting.Obfuscated)) // Square character for obfuscation.
                internalStringBuilder.Append('\u2588', Text.Length);
            if (Formatting.HasFlag(Formatting.Bold))
                internalStringBuilder.Append($"\u001B[1m");
            if (Formatting.HasFlag(Formatting.Strikethrough))
                internalStringBuilder.Append($"\u001B[9m");
            if (Formatting.HasFlag(Formatting.Underline))
                internalStringBuilder.Append($"\u001B[4m");
            if (Formatting.HasFlag(Formatting.Italic))
                internalStringBuilder.Append($"\u001B[3m");

            // Step 2: Process color if available
            if (ForegroundColor != null) {
                var color = ForegroundColor.Value;
                internalStringBuilder.Append($"\u001B[38;2;{color.R};{color.G};{color.B}m");
            }

            if (BackgroundColor != null) {
                var color = BackgroundColor.Value;
                internalStringBuilder.Append($"\u001B[48;2;{color.R};{color.G};{color.B}m");
            }

            // Step 3: Build the string
            internalStringBuilder.Append(AppendNewLine ? Text + Environment.NewLine : Text);

            // Ensure that a formatting reset happens at the end of the string.
            internalStringBuilder.Append("\u001B[0m");

            return internalStringBuilder.ToString();
        }
    };

    public struct Color {
        public Color(byte r = 255, byte g = 255, byte b = 255) {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public byte R;
        public byte G;
        public byte B;

        public static Color Black		=> new Color(12,12,12);
        public static Color White		=> new Color(242,242,242);
        public static Color Gray		=> new Color(204,204,204);
        public static Color DarkRed		=> new Color(197,15,31);
        public static Color DarkGreen	=> new Color(19,161,14);
        public static Color DarkYellow	=> new Color(193,156,0);
        public static Color DarkBlue	=> new Color(0,55,218);
        public static Color DarkMagenta	=> new Color(136,23,152);
        public static Color DarkCyan	=> new Color(58,150,221);
        public static Color Red			=> new Color(231,72,86);
        public static Color Green		=> new Color(22,198,12);
        public static Color Yellow		=> new Color(249,241,165);
        public static Color Blue		=> new Color(59,120,255);
        public static Color Magenta		=> new Color(180,0,158);
        public static Color Cyan		=> new Color(97,214,214);
        public static Color DarkGray	=> new Color(118,118,118);
    }

    public struct FormattingResult {
        public FormattingResult(bool isValidFormatting = false, bool isTextFormatting = false, bool isColorFormatting = false, Color? textColor = null, Formatting formatting = FormattedStringBuilder.Formatting.None) {
            IsTextFormatting = isTextFormatting;
            IsColorFormatting = isColorFormatting;
            TextColor = textColor;
            Formatting = formatting;
        }

        public bool IsValidFormatting = false;
        public bool IsColorFormatting = false;
        public bool IsTextFormatting = false;
        public Color? TextColor = null;
        public Formatting Formatting = Formatting.None;

        public FormattingResult ColorFormatting(Color textColor) {
            IsValidFormatting = true;
            IsColorFormatting = true;
            TextColor = textColor;
            return this;
        }

        public FormattingResult TextFormatting(Formatting formatting) {
            IsValidFormatting = true;
            IsTextFormatting = true;
            Formatting = formatting;
            return this;
        }
    }

    public FormattingResult GetFormattingTypeFromFormattingLetter(char letter) {
        switch (letter) {
            case '0':
                return new FormattingResult().ColorFormatting(Color.Black);
            case '1':
                return new FormattingResult().ColorFormatting(Color.DarkBlue);
            case '2':
                return new FormattingResult().ColorFormatting(Color.DarkGreen);
            case '3':
                return new FormattingResult().ColorFormatting(Color.DarkCyan);
            case '4':
                return new FormattingResult().ColorFormatting(Color.DarkRed);
            case '5':
                return new FormattingResult().ColorFormatting(Color.DarkMagenta);
            case '6':
                return new FormattingResult().ColorFormatting(Color.DarkYellow);
            case '7':
                return new FormattingResult().ColorFormatting(Color.Gray);
            case '8':
                return new FormattingResult().ColorFormatting(Color.DarkGray);
            case '9':
                return new FormattingResult().ColorFormatting(Color.Blue);
            case 'a':
                return new FormattingResult().ColorFormatting(Color.Green);
            case 'b':
                return new FormattingResult().ColorFormatting(Color.Cyan);
            case 'c':
                return new FormattingResult().ColorFormatting(Color.Red);
            case 'd':
                return new FormattingResult().ColorFormatting(Color.Magenta);
            case 'e':
                return new FormattingResult().ColorFormatting(Color.Yellow);
            case 'f':
                return new FormattingResult().ColorFormatting(Color.White);
            case 'l':
                return new FormattingResult().TextFormatting(Formatting.Bold);
            case 'm':
                return new FormattingResult().TextFormatting(Formatting.Strikethrough);
            case 'n':
                return new FormattingResult().TextFormatting(Formatting.Underline);
            case 'o':
                return new FormattingResult().TextFormatting(Formatting.Italic);
            case 'r':
                return new FormattingResult().TextFormatting(Formatting.None);
            // TODO: Remove custom formatting
            case 'w':
            // Custom: Background Red;
            case 'x':
            // Custom: Background Yellow;
            case 'y':
            // Custom: Background Green;
            case 'z':
            // Custom: Background Gray;

            default:
                Trace.Assert(false, $"Formatting for character {letter} is not implemented.");
                return new();
        }
    }

    // Color values taken from https://en.wikipedia.org/wiki/ANSI_escape_code#3-bit_and_4-bit using the Windows 10 Console color set
    public static Dictionary<ConsoleColor, ((byte r, byte g, byte b) rgb, string code)> DefaultColors = new() {
        {ConsoleColor.Black,        ((12,12,12), "\u001B[30m")},    // §0, Black
        {ConsoleColor.White,        ((242,242,242), "\u001B[97m")}, // §f, White (Bright White)
        {ConsoleColor.Gray,         ((204,204,204), "\u001B[90m")}, // §7, Gray  (White)
        {ConsoleColor.DarkRed,      ((197,15,31), "\u001B[31m")},   // §4, Dark Red
        {ConsoleColor.DarkGreen,    ((19,161,14), "\u001B[32m")},   // §2, Dark Green
        {ConsoleColor.DarkYellow,   ((193,156,0), "\u001B[33m")},   // §6, Gold
        {ConsoleColor.DarkBlue,     ((0,55,218), "\u001B[34m")},    // §1, Dark Blue
        {ConsoleColor.DarkMagenta,  ((136,23,152), "\u001B[35m")},  // §5, Dark Purple
        {ConsoleColor.DarkCyan,     ((58,150,221), "\u001B[36m")},  // §3, Dark Aqua
        {ConsoleColor.Red,          ((231,72,86), "\u001B[91m")},   // §c, Red
        {ConsoleColor.Green,        ((22,198,12), "\u001B[92m")},   // §a, Green
        {ConsoleColor.Yellow,       ((249,241,165), "\u001B[93m")}, // §e, Yellow
        {ConsoleColor.Blue,         ((59,120,255), "\u001B[94m")},  // §9, Blue
        {ConsoleColor.Magenta,      ((180,0,158), "\u001B[95m")},   // §d, Light Purple
        {ConsoleColor.Cyan,         ((97,214,214), "\u001B[96m")},  // §b, Aqua
        {ConsoleColor.DarkGray,     ((118,118,118), "\u001B[37m")}  // §8, Dark Gray (Bright Black)
    };
    
    public static Dictionary<Formatting, string> FormattingCodes = new() {
        { Formatting.Obfuscated , "\u2588"}, // Replace the affected character(s) with this code.
        { Formatting.Bold , "\u001B[1m"},           // §l
        { Formatting.Italic , "\u001B[3m"},         // §o
        { Formatting.Strikethrough, "\u001B[9m"},   // §m
        { Formatting.Underline, "\u001B[4m"},       // §n
        { Formatting.None, "\u001B[0m"}             // §r
    };

    public static ConsoleColor GetClosestDefaultColor(byte r, byte g, byte b) {
        var colors = (r, g, b);
        
        ConsoleColor tempColor = ConsoleColor.White;
        double tempDistance = double.MaxValue;

        foreach (var defaultColor in DefaultColors) {
            double rDistance = colors.r - defaultColor.Value.rgb.r;
            double gDistance = colors.g - defaultColor.Value.rgb.g;
            double bDistance = colors.b - defaultColor.Value.rgb.b;

            double totalDistance =
                Math.Sqrt((rDistance * rDistance) + (gDistance * gDistance) + (bDistance * bDistance));

            if (totalDistance < tempDistance) {
                tempColor = defaultColor.Key;
                tempDistance = totalDistance;
            }
        }

        return tempColor;
    }
}