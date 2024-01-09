using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ConsoleInteractive.Helper; 

public class RuneBuilder {
    List<Rune> _runes = new();

    public int Count => _runes.Count;
    
    public void Append(Rune[] runes) {
        foreach (var rune in runes) {
            _runes.Add(rune);
        }
    }

    public void Append(IEnumerable<Rune> runes) {
        _runes.InsertRange(_runes.Count, runes);
    }
    
    public void Append(Rune rune) {
        _runes.Add(rune);
    }
    
    public void Append(int index, Rune rune) {
        _runes.Insert(index, rune);
    }

    public void Replace(Range range, string str) {
        List<Rune> _runebldr = new();
        
        // Determine the text elements in the string to be replaced.
        // A text element is defined here:
        // https://learn.microsoft.com/en-us/dotnet/api/system.globalization.stringinfo.gettextelementenumerator?view=net-7.0
        // as a singular visible rune.
        foreach (var rune in str.EnumerateRunes()) {
            _runebldr.Add(rune);
        }
        
        Replace(range, _runebldr.ToArray());
    }

    public void Replace(Range range, IEnumerable<Rune> runes) {
        // Determine the starting and ending offsets.
        var start = range.Start.Value;
        var end = range.End.Value;
        
        _runes.RemoveRange(start, end - start);
        _runes.InsertRange(start, runes);
    }
}