using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex.Extensions
{
    public static class StringExtensions
    {
        public static int[] ToCodePoints(this string str)
        {
            if(string.IsNullOrEmpty(str))
            {
                return new int[0];
            }

            var chars = new List<int>(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                chars.Add(char.ConvertToUtf32(str, i));
                if(char.IsHighSurrogate(str[i]))
                {
                    i++;
                }
            }
            return chars.ToArray();
        }
    }
}
