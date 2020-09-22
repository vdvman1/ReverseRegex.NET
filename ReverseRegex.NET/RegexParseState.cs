using ReverseRegex.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex
{
    public class RegexParseState
    {
        private readonly int[] Regex;
        private int Index = -1;
        public bool CaseSensitive = true;

        public RegexParseState(int[] regex)
        {
            Regex = regex;
        }

        public int Char => Regex[Index];

        public int CapturesCount => 1;

        public bool TryPeekNext(out int c)
        {
            if(Index + 1 < Regex.Length)
            {
                c = Regex[Index + 1];
                return true;
            }

            c = default;
            return false;
        }

        public bool MoveNext()
        {
            Index++;
            if(Index < Regex.Length)
            {
                return true;
            }

            Index = Regex.Length - 1;
            return false;
        }

        public int ReadOctal(int maxLength)
        {
            int value = Char - '0';
            for (int i = 1; i < maxLength && TryPeekNext(out int c) && c.IsOctalDigit(); i++)
            {
                MoveNext();
                value = value << 3 | (c - '0');
            }

            ValidateEscape(value);

            return value;
        }

        private void ValidateEscape(int value)
        {
            if (value > 0x10ffff)
            {
                throw new Exception($"Value {value} is not valid unicode");
            }

            // TODO: Investigate whether we need an option to allow surrogate escapes
            if (0xd800 <= value && value <= 0xdfff)
            {
                throw new Exception("Surrogate escapes are not allowed");
            }
        }

        public ISnapshot Snapshot() => new SnapshotImpl(this);

        private class SnapshotImpl : ISnapshot
        {
            private readonly RegexParseState State;
            private readonly int Index;
            private readonly bool CaseSensitive;

            public SnapshotImpl(RegexParseState state)
            {
                State = state;
                Index = state.Index;
                CaseSensitive = state.CaseSensitive;
            }

            public void Restore()
            {
                State.Index = Index;
                State.CaseSensitive = CaseSensitive;
            }
        }

        public interface ISnapshot
        {
            public void Restore();
        }
    }
}
