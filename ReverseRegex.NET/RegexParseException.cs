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
            int spaces;
            if(ConsoleHelper.SupportsCombining)
            {
                int[] textElements = StringInfo.ParseCombiningCharacters(Regex.CodePointsToString());
                spaces = 0;
                while (spaces + 1 < textElements.Length && Index > textElements[spaces] && Index > textElements[spaces + 1])
                {
                    spaces++;
                }
                if(spaces + 1 < textElements.Length && textElements[spaces + 1] == Index)
                {
                    spaces++;
                }
            }
            else
            {
                spaces = Index;
            }
            Console.Error.WriteLine($@"{ExpectedMessage}
{Regex.CodePointsToString()}
{new string(' ', spaces)}^");
        }
    }
}
