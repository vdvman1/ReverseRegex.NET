using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ReverseRegex.RegexNodes
{
    internal abstract class QuantifierNode
    {
        public bool Possesive { get; private set; }
        public bool Lazy { get; private set; }
        public bool HasModifier => Possesive || Lazy;

        public void SetPossesive()
        {
            Debug.Assert(!HasModifier);
            Possesive = true;
        }

        public void SetLazy()
        {
            Debug.Assert(!HasModifier);
            Lazy = true;
        }
    }
}
