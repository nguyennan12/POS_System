using System.Globalization;
using System.Text;

namespace POS.Infrastructure.Reports;

internal static class ReceiptUnicode
{
    private const string Tones = "\0\u0300\u0301\u0309\u0303\u0323";
    private static readonly Dictionary<char, (char Base, char Shape, char Tone)> Decompositions = new();
    private static readonly Dictionary<(char Base, char Shape, char Tone), char> Compositions = new();
    private static readonly bool HasRuntimeNormalization = "a\u0301".Normalize(NormalizationForm.FormC) == "á";

    static ReceiptUnicode()
    {
        // Canonical Vietnamese vowel/tone combinations, independent of ICU and OS cultures.
        (char Base, char Shape, string Letters)[] rows =
        [
            ('a', '\0', "aàáảãạ"), ('a', '\u0306', "ăằắẳẵặ"), ('a', '\u0302', "âầấẩẫậ"),
            ('e', '\0', "eèéẻẽẹ"), ('e', '\u0302', "êềếểễệ"), ('i', '\0', "iìíỉĩị"),
            ('o', '\0', "oòóỏõọ"), ('o', '\u0302', "ôồốổỗộ"), ('o', '\u031b', "ơờớởỡợ"),
            ('u', '\0', "uùúủũụ"), ('u', '\u031b', "ưừứửữự"), ('y', '\0', "yỳýỷỹỵ"),
            ('A', '\0', "AÀÁẢÃẠ"), ('A', '\u0306', "ĂẰẮẲẴẶ"), ('A', '\u0302', "ÂẦẤẨẪẬ"),
            ('E', '\0', "EÈÉẺẼẸ"), ('E', '\u0302', "ÊỀẾỂỄỆ"), ('I', '\0', "IÌÍỈĨỊ"),
            ('O', '\0', "OÒÓỎÕỌ"), ('O', '\u0302', "ÔỒỐỔỖỘ"), ('O', '\u031b', "ƠỜỚỞỠỢ"),
            ('U', '\0', "UÙÚỦŨỤ"), ('U', '\u031b', "ƯỪỨỬỮỰ"), ('Y', '\0', "YỲÝỶỸỴ")
        ];
        foreach (var row in rows)
            for (var i = 0; i < Tones.Length; i++)
            {
                var key = (row.Base, row.Shape, Tones[i]);
                Compositions[key] = row.Letters[i];
                Decompositions[row.Letters[i]] = key;
            }
    }

    public static string Normalize(string text)
    {
        if (HasRuntimeNormalization) return text.Normalize(NormalizationForm.FormC);
        // .NET invariant mode disables Unicode normalization. Preserve Vietnamese NFC
        // explicitly; leave other scripts intact for the embedded font's text shaping.
        var output = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(text);
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            if (!Decompositions.TryGetValue(element[0], out var key)) { output.Append(element); continue; }
            var valid = true;
            foreach (var c in element.AsSpan(1))
            {
                if (c is '\u0302' or '\u0306' or '\u031b' && key.Shape == '\0') key.Shape = c;
                else if (Tones.IndexOf(c) > 0 && key.Tone == '\0') key.Tone = c;
                else { valid = false; break; }
            }
            if (valid && Compositions.TryGetValue(key, out var composed)) output.Append(composed);
            else output.Append(element);
        }
        return output.ToString();
    }

    public static string Windows1258(string text)
    {
        var output = new StringBuilder();
        foreach (var c in Normalize(text))
        {
            if (Decompositions.TryGetValue(c, out var key))
            {
                output.Append(Compositions[(key.Base, key.Shape, '\0')]);
                if (key.Tone != '\0') output.Append(key.Tone);
            }
            else output.Append(c);
        }
        return output.ToString();
    }
}
