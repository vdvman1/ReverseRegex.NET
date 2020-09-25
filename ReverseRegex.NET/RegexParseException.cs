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
        public readonly IEnumerable<int> Regex;
        public RegexParseException(string msg, int index, IEnumerable<int> regex) : base($"{msg} at normalised index {index}")
        {
            ExpectedMessage = msg;
            Index = index;
            Regex = regex;
        }

        public void Print()
        {
            Console.Error.WriteLine($@"{ExpectedMessage}
{Regex.CodePointsToString()}
{new string(' ', Index)}^");
        }
    }
}
