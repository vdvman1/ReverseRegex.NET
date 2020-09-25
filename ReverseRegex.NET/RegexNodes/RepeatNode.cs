using ReverseRegex.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ReverseRegex.RegexNodes
{
    internal class RepeatNode : QuantifierNode, IRegexNode
    {
        private readonly IRegexNode Node;
        private readonly int Min;
        private readonly int Max;

        public RepeatNode(IRegexNode node, int min, int max)
        {
            Node = node;
            Min = min;
            Max = max;
        }

        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng)
        {
            int i = 0;
            for(; i < Min; i++)
            {
                foreach (var c in Node.GenerateSample(rng))
                {
                    yield return c;
                }
            }
            for (; i < Max && rng.NextBool(); i++)
            {
                foreach (var c in Node.GenerateSample(rng))
                {
                    yield return c;
                }
            }
        }

        public bool AllowsRepetition => false;
    }
}
