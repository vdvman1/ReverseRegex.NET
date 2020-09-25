using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseRegex.RegexNodes
{
    internal class SequenceNode : IRegexNode
    {
        private readonly List<IRegexNode> Nodes;

        private SequenceNode(IEnumerable<IRegexNode> nodes) => Nodes = nodes.ToList();

        public static IRegexNode From(IList<IRegexNode> nodes) => nodes.Count switch
        {
            0 => new EmptyNode(),
            1 => nodes[0],
            _ => new SequenceNode(nodes)
        };

        public IEnumerable<(int c, bool caseSensitive)> GenerateSample(Random rng) => Nodes.SelectMany(n => n.GenerateSample(rng));

        public bool AllowsRepetition => false;
    }
}
