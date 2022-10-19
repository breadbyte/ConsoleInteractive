using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleInteractive.WriterImpl; 

public class CrossPlatformWriter : WriterBase {

    public override void Write(StringData data) {
        throw new NotImplementedException();
    }

    public override void Write(FormattedStringBuilder data) {
        var expanded = data.Expand();

        foreach (var line in expanded) {
            WriteStringDataChain(line);
        }
    }

    public override void WriteStringDataChain(List<StringData> data) {
        int chainLength = 0;
        StringBuilder completeString = new();

        foreach (var strData in data) {
            chainLength += strData.Text.Length;
            completeString.Append(strData.Build());
        }
            
        __WriteInternal(completeString.ToString(), DetermineLineCount(chainLength));
    }
}