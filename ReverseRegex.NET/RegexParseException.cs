using ReverseRegex.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ReverseRegex
{
    public class RegexParseException : Exception
    {
        public readonly string ExpectedMessage;
        public readonly int Index;
        public readonly string Regex;
        public RegexParseException(string msg, int index, IEnumerable<int> regex) : base($"{msg} at normalised index {index}")
        {
            ExpectedMessage = msg;
            Index = index;
            Regex = regex.CodePointsToString();
        }

        public void Print()
        {
            int[] textElements = StringInfo.ParseCombiningCharacters(Regex);
            int spaces = 0;
            while(spaces + 1 < textElements.Length && Index > textElements[spaces] && Index > textElements[spaces + 1])
            {
                spaces++;
            }

            Console.Error.WriteLine($@"{ExpectedMessage}
{Regex}
{new string(' ', spaces)}^");
        }
    }
}
