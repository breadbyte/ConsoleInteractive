namespace ConsoleInteractive.Extensions; 

public static class Formatting {
    public static string BuildFormattingVtCode(this FormattingType type) {
        var output = "";
        
        // NB: We don't parse Obfuscation formatting here
        // because we don't have access to the string to obfuscate.
        // It has to be the responsibility of the caller.

        // We're only concatenating short strings,
        // so a StringBuilder is inefficient here.
        if (type.HasFlag(FormattingType.Bold))
            output += "\u001B[1m";
        if (type.HasFlag(FormattingType.Strikethrough))
            output += "\u001B[9m";
        if (type.HasFlag(FormattingType.Underline))
            output += "\u001B[4m";
        if (type.HasFlag(FormattingType.Italic))
            output += "\u001B[3m";
        if (type.HasFlag(FormattingType.None))
            output += $"\u001B[0m";

        return output;
    }
}