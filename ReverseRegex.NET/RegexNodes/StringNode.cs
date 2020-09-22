using ReverseRegex.Extensions;
using ReverseRegex.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseRegex.NET.RegexNodes
{
    public class StringNode : IRegexNode
    {
        public readonly IReadOnlyList<int> Chars;
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

        public string GenerateSample(Random rng)
        {
            if(CaseSensitive)
            {
                return Chars.CodePointsToString();
            }

            var builder = new StringBuilder(Chars.Count);
            foreach (int c in Chars)
            {
                var str = c.CodePointAsString();
                builder.Append(rng.NextBool() ? str.ToUpper() : str.ToLower());
            }
            return builder.ToString();
        }
    }
}
