using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex.NET.Extensions
{
    public static class IntExtensions
    {
        public static string CodePointAsString(this int c) => char.ConvertFromUtf32(c);
    }
}
