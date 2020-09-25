using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex
{
    internal interface IRegexNode
    {
        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng);
    }
}
