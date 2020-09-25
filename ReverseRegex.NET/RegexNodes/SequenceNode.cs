using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseRegex.RegexNodes
{
    internal class SequenceNode : IRegexNode
    {
        private readonly List<IRegexNode> Nodes;

        public SequenceNode(IEnumerable<IRegexNode> nodes) => Nodes = nodes.ToList();

        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng) => Nodes.SelectMany(n => n.GenerateSample(rng));
    }
}
