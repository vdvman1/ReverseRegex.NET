using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex.RegexNodes
{
    internal interface IRegexNode
    {
        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng);
    }
}
