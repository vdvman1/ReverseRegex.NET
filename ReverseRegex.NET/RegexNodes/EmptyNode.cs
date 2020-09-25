using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex.RegexNodes
{
    internal class EmptyNode : IRegexNode
    {
        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng)
        {
            yield break;
        }
    }
}
