using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex.NET.RegexNodes
{
    public class StringNode : IRegexNode
    {
        public readonly string Value;

        public StringNode(string value) => Value = value;

        public string GenerateSample() => Value;
    }
}
