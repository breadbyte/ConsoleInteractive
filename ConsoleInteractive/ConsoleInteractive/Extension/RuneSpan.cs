using System;
using System.Text;

namespace ConsoleInteractive.Extensions; 

public class RuneSpan {
    public static bool IsEmptyOrWhiteSpace(Span<Rune> runeSpan) {
        foreach (Rune rune in runeSpan) {
            if (!Rune.IsWhiteSpace(rune)) return false;
        }
        return true;
    }
}