using System.Diagnostics;

namespace ConsoleInteractive; 

public struct FormattingResult {
    public FormattingResult(bool isValidFormatting = false, bool isTextFormatting = false, bool isColorFormatting = false, Color? textColor = null, FormattingType formattingType = FormattingType.None) {
        IsTextFormatting = isTextFormatting;
        IsColorFormatting = isColorFormatting;
        TextColor = textColor;
        FormattingType = formattingType;
    }

    public bool IsValidFormatting = false;
    public bool IsColorFormatting = false;
    public bool IsTextFormatting = false;
    public Color? TextColor = null;
    public FormattingType FormattingType = FormattingType.None;

    public FormattingResult ColorFormatting(Color textColor) {
        IsValidFormatting = true;
        IsColorFormatting = true;
        TextColor = textColor;
        return this;
    }

    public FormattingResult TextFormatting(FormattingType formattingType) {
        IsValidFormatting = true;
        IsTextFormatting = true;
        FormattingType = formattingType;
        return this;
    }
    
    public static FormattingResult GetFormattingTypeFromFormattingLetter(char letter) {
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
                return new FormattingResult().TextFormatting(FormattingType.Bold);
            case 'm':
                return new FormattingResult().TextFormatting(FormattingType.Strikethrough);
            case 'n':
                return new FormattingResult().TextFormatting(FormattingType.Underline);
            case 'o':
                return new FormattingResult().TextFormatting(FormattingType.Italic);
            case 'r':
                return new FormattingResult().TextFormatting(FormattingType.None);
            default:
                Trace.Assert(false, $"Formatting for character {letter} is not implemented.");
                return new FormattingResult();
        }
    }
    
}