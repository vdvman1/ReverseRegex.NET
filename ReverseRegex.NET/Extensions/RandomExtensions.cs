using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex.Extensions
{
    public static class RandomExtensions
    {
        public static bool NextBool(this Random rng) => rng.NextDouble() >= 0.5;
    }
}
