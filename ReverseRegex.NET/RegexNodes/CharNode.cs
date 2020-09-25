using ReverseRegex.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseRegex.RegexNodes
{
    internal class CharNode : IRegexNode
    {
        private readonly int Char;
        private readonly bool CaseSensitive;

        public CharNode(int value, RegexParseState state)
        {
            Char = value;
            CaseSensitive = state.CaseSensitive;
        }

        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng)
        {
            yield return (Char, CaseSensitive);
        }

        public bool AllowsRepetition => true;
    }
}
