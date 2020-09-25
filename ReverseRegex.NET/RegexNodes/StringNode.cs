using ReverseRegex.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseRegex.RegexNodes
{
    internal class StringNode : IRegexNode
    {
        private readonly IReadOnlyList<int> Chars;
        private readonly bool CaseSensitive;

        public StringNode(IEnumerable<int> value, RegexParseState state)
        {
            Chars = value.ToList().AsReadOnly();
            CaseSensitive = state.CaseSensitive;
        }

        public StringNode(int value, RegexParseState state)
        {
            Chars = new List<int> { value };
            CaseSensitive = state.CaseSensitive;
        }

        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng)
        {
            foreach (var c in Chars)
            {
                yield return (c, CaseSensitive);
            }
        }
    }
}
