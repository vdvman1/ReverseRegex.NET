using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex.Extensions
{
    public static class IntExtensions
    {
        public static string CodePointAsString(this int c) => char.ConvertFromUtf32(c);

        public static bool IsOctalDigit(this int c) => '0' <= c && c <= '7';

        public static bool IsAsciiDigit(this int c) => '0' <= c && c <= '9';
    }
}
