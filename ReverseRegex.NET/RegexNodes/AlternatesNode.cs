using ReverseRegex.RegexNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseRegex.NET.RegexNodes
{
    internal class AlternatesNode : IRegexNode
    {
        private readonly IReadOnlyList<IRegexNode> Nodes;
        public AlternatesNode(IEnumerable<IRegexNode> nodes)
            => Nodes = nodes.ToList().AsReadOnly();

        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng)
            => Nodes[rng.Next(Nodes.Count)].GenerateSample(rng);
    }
}
