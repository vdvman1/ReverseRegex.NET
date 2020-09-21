using ReverseRegex.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex
{
    public class RegexParseState
    {
        private readonly int[] Regex;
        private int Index = -1;

        public RegexParseState(int[] regex)
        {
            Regex = regex;
        }

        public int Char => Regex[Index];

        public bool MoveNext()
        {
            Index++;
            if(Index < Regex.Length)
            {
                return true;
            }

            Index = Regex.Length - 1;
            return false;
        }
    }
}
