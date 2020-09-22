using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex
{
    public interface IRegexNode
    {
        public string GenerateSample(Random rng);
    }
}
