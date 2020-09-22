using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReverseRegex.NET.RegexNodes
{
    public class SequenceNode : IRegexNode
    {
        private readonly List<IRegexNode> Nodes;

        public SequenceNode(IEnumerable<IRegexNode> nodes) => Nodes = nodes.ToList();

        public string GenerateSample(Random rng) => string.Join("", Nodes.Select(n => n.GenerateSample(rng)));
    }
}
