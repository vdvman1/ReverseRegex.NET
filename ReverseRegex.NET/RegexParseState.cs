using ReverseRegex.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        public bool MoveNextIf(ISet<int> chars, bool include)
        {
            if(TryPeekNext(out int c) && chars.Contains(c) == include)
            {
                MoveNext();
                return true;
            }

            return false;
        }

        public bool MoveNextIf(int ch, bool include)
        {
            if (TryPeekNext(out int c) && (ch == c) == include)
            {
                MoveNext();
                return true;
            }

            return false;
        }

        public int ReadOctalEscape(int minLength, int maxLength)
        {
            if(!Char.IsOctalDigit())
            {
                if(minLength > 0)
                {
                    throw new RegexParseException("Expected octal digit", Index, Regex);
                }
                return 0;
            }

            int value = Char - '0';
            int i = 1;
            for (; i < maxLength && TryPeekNext(out int c) && c.IsOctalDigit(); i++)
            {
                MoveNext();
                value = value << 3 | (c - '0');
            }

            if(i < minLength)
            {
                throw new RegexParseException("Expected octal digit", Index, Regex);
            }

            ValidateEscape(value);

            return value;
        }

        public int ReadHexEscape(int minLength, int maxLength)
        {
            if(!Char.IsHexDigit())
            {
                if (minLength > 0)
                {
                    throw new RegexParseException("Expected hex digit", Index, Regex);
                }
                return 0;
            }

            int value = Char - '0';
            int i = 1;
            for (; i < maxLength && TryPeekNext(out int c) && c.IsHexDigit(); i++)
            {
                MoveNext();
                value = value << 4 | (c - '0');
            }

            if (i < minLength)
            {
                throw new RegexParseException("Expected hex digit", Index, Regex);
            }

            ValidateEscape(value);

            return value;
        }

        private void ValidateEscape(int value)
        {
            if (value > 0x10ffff)
            {
                throw new RegexParseException($"Value {value} is not valid unicode", Index, Regex);
            }

            // TODO: Investigate whether we need an option to allow surrogate escapes
            if (0xd800 <= value && value <= 0xdfff)
            {
                throw new RegexParseException($"Surrogate escapes are not allowed", Index, Regex);
            }
        }

        public int ReadAsciiNumber(int minLength, int maxLength)
        {
            if (!Char.IsAsciiDigit())
            {
                if (minLength > 0)
                {
                    throw new RegexParseException("Expected decimal digit", Index, Regex);
                }
                return 0;
            }

            int value = Char - '0';
            int i = 1;
            for (; i < maxLength && TryPeekNext(out int c) && c.IsAsciiDigit(); i++)
            {
                MoveNext();
                value = value * 10 + (c - '0');
            }

            if (i < minLength)
            {
                throw new RegexParseException("Expected decimal digit", Index, Regex);
            }

            return value;
        }

        public ISnapshot Snapshot() => new SnapshotImpl(this);

        public void Require(int c)
        {
            if (!MoveNext() || Char != c)
            {
                throw new RegexParseException($"Expected character {c.CodePointAsPrintingString()}", Index, Regex);
            }
        }

        public void RequireRange(int start, int end)
        {
            if(!MoveNext())
            {
                throw new RegexParseException($"Expected character in the range {start.CodePointAsPrintingString()} - {end.CodePointAsPrintingString()}", Index, Regex);
            }
            if (Char < start || Char > end)
            {
                throw new RegexParseException($"Character not in the required range {start.CodePointAsPrintingString()} - {end.CodePointAsPrintingString()}", Index, Regex);
            }
        }

        public void Require(IEnumerable<int> match)
        {
            foreach (var c in match)
            {
                Require(c);
            }
        }

        public IDisposable BeginMatch(int start, int end) => BeginMatch(new[] { start }, new[] { end });
        public IDisposable BeginMatch(IEnumerable<int> start, int end) => BeginMatch(start, new[] { end });
        public IDisposable BeginMatch(int start, IEnumerable<int> end) => BeginMatch(new[] { start }, end);

        public IDisposable BeginMatch(IEnumerable<int> start, IEnumerable<int> end)
        {
            Require(start);
            // TODO: Improve error message of what was expected, e.g. parsing hex surrounded by {} currently says only '}' expected if the hex stopped early
            return new EndRequirement(end, this);
        }

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

        private class EndRequirement : IDisposable
        {
            private readonly IEnumerable<int> Ending;
            private readonly RegexParseState State;
            private bool disposed = false;

            public EndRequirement(IEnumerable<int> ending, RegexParseState state)
            {
                Ending = ending;
                State = state;
            }

            public void Dispose()
            {
                if(!disposed && Marshal.GetExceptionPointers() == IntPtr.Zero)
                {
                    disposed = true;
                    State.Require(Ending);
                }
            }
        }
    }
}
