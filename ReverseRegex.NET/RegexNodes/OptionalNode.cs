using ReverseRegex.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ReverseRegex.RegexNodes
{
    internal class OptionalNode : QuantifierNode, IRegexNode
    {
        private readonly IRegexNode Node;

        public OptionalNode(IRegexNode node) => Node = node;

        public bool AllowsRepetition => false;

        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng)
        {
            if(rng.NextBool())
            {
                foreach (var c in Node.GenerateSample(rng))
                {
                    yield return c;
                }
            }
        }
    }
}
