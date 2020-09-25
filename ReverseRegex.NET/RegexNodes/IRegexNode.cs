using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex
{
    public interface IRegexNode
    {
        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng);
    }
}
