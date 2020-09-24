using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ReverseRegex.Extensions
{
    public static class IntExtensions
    {
        public static string CodePointAsString(this int c) => char.ConvertFromUtf32(c);

        public static bool IsOctalDigit(this int c) => '0' <= c && c <= '7';

        public static bool IsAsciiDigit(this int c) => '0' <= c && c <= '9';

        public static bool IsHexDigit(this int c) =>
               ('0' <= c && c <= '9')
            || ('a' <= c && c <= 'f')
            || ('A' <= c && c <= 'F');

        public static bool IsLetterOrDigit(this int c) => char.IsLetterOrDigit(c.CodePointAsString(), 0);

        public static bool IsPrintableNotSpace(this int c)
        {
            var str = c.CodePointAsString();
            UnicodeCategory cat = char.GetUnicodeCategory(str, 0);
            switch(cat)
            {
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.CurrencySymbol:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.FinalQuotePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.MathSymbol:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.OtherNumber:
                case UnicodeCategory.OtherPunctuation:
                case UnicodeCategory.OtherSymbol:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.UppercaseLetter:
                    return true;
            }
            return false;
        }

        public static string CodePointAsPrintingString(this int c) => c.IsPrintableNotSpace() ? c.CodePointAsString() : $"0x{c:X}";
    }
}
