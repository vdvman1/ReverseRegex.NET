using ReverseRegex.Extensions;
using ReverseRegex.RegexNodes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex
{
    public class Regex
    {
        private readonly IRegexNode RootNode;
        private Regex(IRegexNode node) => RootNode = node;

        public string GenerateSample(Random rng)
        {
            // TODO: Proper support for unicode casing, inclusing multiple characters potentially converting to a single character
            var builder = new StringBuilder();
            foreach ((int c, bool caseSensitive) in RootNode.GenerateSample(rng))
            {
                var str = c.CodePointAsString();
                if (caseSensitive)
                {
                    builder.Append(str);
                }
                else
                {
                    builder.Append(rng.NextBool() ? str.ToUpper() : str.ToLower());
                }
            }

            return builder.ToString();
        }

        #region Parsing the regex
        public static Regex Build(string regexStr, RegexOptions options)
            => new Regex(ParseRoot(new RegexParseState(regexStr.Normalize().ToCodePoints(), options)));

        private static IRegexNode ParseRoot(RegexParseState state)
        {
            // TODO: Alternation
            IRegexNode node = ParseSequence(state, new HashSet<int> { '|' });
            if (state.HasNext)
            {
                throw new NotImplementedException("Alternation has not been implemented");
            }
            return node;
        }

        private static IRegexNode ParseSequence(RegexParseState state, ISet<int> ends)
        {
            var nodes = new List<IRegexNode>();
            var str = new List<int>();
            void nextNode()
            {
                if (str.Count != 0)
                {
                    nodes.Add(new StringNode(str, state));
                    str.Clear();
                }
            }

            while (state.MoveNextIf(ends, false))
            {
                switch (state.Char)
                {
                    case '\\':
                        nextNode();
                        nodes.Add(ParseEscape(state, inCharacterClass: false));
                        break;
                    case '^':
                        throw new NotImplementedException("Start of string/line assertion has not been implemented");
                    case '$':
                        throw new NotImplementedException("End of string/line assertion has not been implemented");
                    case '.':
                        throw new NotImplementedException("Dot matching has not been implemented");
                    case '[':
                        throw new NotImplementedException("Character classes have not been implemented");
                    case '(':
                        throw new NotImplementedException("Groups/control verbs have not been implemented");
                    case '*':
                    case '+':
                    case '?':
                    case '{':
                        throw new NotImplementedException("Quantifiers have not been implemented");
                    default:
                        str.Add(state.Char);
                        break;
                }
            }

            var lastNode = new StringNode(str, state);
            if (nodes.Count == 0)
            {
                return lastNode;
            }

            nodes.Add(lastNode);
            return new SequenceNode(nodes);
        }

        private static IRegexNode ParseEscape(RegexParseState state, bool inCharacterClass)
        {
            if (!state.MoveNext())
            {
                throw new Exception("Expected a character that is being escaped");
            }

            if (!state.Char.IsLetterOrDigit())
            {
                return new StringNode(state.Char, state);
            }

            switch (state.Char)
            {
                case 'Q':
                    return ParseLiteral(state);
                case 'a':
                    return new StringNode('\a', state);
                case 'c':
                    {
                        state.RequireRange(' ', '~');
                        // Convert lowercase to upper case, and invert bit 6. See \cx in the pcre docs: http://www.rexegg.com/pcre-doc/_latestpcre2/pcre2pattern.html#SEC5
                        var ctrlC = state.Char.CodePointAsString().ToUpper().ToCodePoints()[0] ^ (1 << 6);
                        return new StringNode(ctrlC, state);
                    }
                case 'e': // "escape" character
                    return new StringNode(0x1B, state);
                case 'f':
                    return new StringNode('\f', state);
                case 'n':
                    return new StringNode('\n', state);
                case 'r':
                    return new StringNode('\r', state);
                case 't':
                    return new StringNode('\t', state);
                case '0':
                    return new StringNode(state.ReadOctalEscape(0, 3), state);
                case 'o':
                    {
                        int value;
                        using (state.BeginMatch('{', '}'))
                        {
                            if (!state.MoveNext())
                            {
                                throw new Exception(@"Empty \o{} escapes are invalid");
                            }
                            value = state.ReadOctalEscape(1, int.MaxValue);
                        }

                        return new StringNode(value, state);
                    }
                case 'x':
                    {
                        if (!state.TryPeekNext(out int c))
                        {
                            return new StringNode(0, state);
                        }

                        if (c == '{')
                        {
                            int value;
                            using (state.BeginMatch('{', '}'))
                            {
                                if (!state.MoveNext())
                                {
                                    throw new Exception(@"Empty \x{} escapes are invalid");
                                }
                                value = state.ReadHexEscape(1, int.MaxValue);
                            }

                            return new StringNode(value, state);
                        }
                        else
                        {
                            state.MoveNext(); // Guaranteed to succeed thanks to the TryPeekNext earlier
                            return new StringNode(state.ReadHexEscape(0, 2), state);
                        }
                    }
                case 'N':
                    {
                        if (state.TryPeekNext(out int c) && c == '{')
                        {
                            int value;
                            using (state.BeginMatch("{U+".ToCodePoints(), '}'))
                            {
                                if (!state.MoveNext() || !state.Char.IsHexDigit())
                                {
                                    throw new InvalidOperationException(@"Escape code \N{U+...} must contain 1 or more hex digits");
                                }

                                value = state.ReadHexEscape(1, int.MaxValue);
                            }

                            return new StringNode(value, state);
                        }
                        else
                        {
                            throw new NotImplementedException("Character classes have not been implemented");
                        }
                    }
                case 'b':
                    if (inCharacterClass)
                    {
                        return new StringNode('\b', state);
                    }
                    else
                    {
                        throw new NotImplementedException("Assertions have not been implemented");
                    }
                case 'g':
                    throw new NotImplementedException("Back/forward references have not been implemented");
                case 'd':
                case 'D':
                case 'h':
                case 'H':
                case 's':
                case 'S':
                case 'v':
                case 'V':
                case 'w':
                case 'W':
                case 'C':
                    throw new NotImplementedException("Character classes have not been implemented");
                case 'R':
                    if (inCharacterClass)
                    {
                        throw new NotImplementedException("Character classes have not been implemented");
                    }
                    break;
                case 'p':
                case 'P':
                    throw new NotImplementedException("Unicode properties have not been implemented");
                case 'X':
                    throw new NotImplementedException("Unicode extended grapheme clusters have not been implemented");
                case 'B':
                case 'A':
                case 'Z':
                case 'z':
                case 'G':
                    if (inCharacterClass)
                    {
                        break;
                    }
                    else
                    {
                        throw new NotImplementedException("Assertions have not been implemented");
                    }
                default:
                    if (state.Char.IsAsciiDigit())
                    {
                        if (inCharacterClass)
                        {
                            if (state.Char == '8' || state.Char == '9')
                            {
                                return new StringNode(state.Char, state);
                            }

                            return new StringNode(state.ReadOctalEscape(1, 3), state);
                        }
                        else
                        {
                            var snapshot = state.Snapshot();

                            int firstDigit = state.Char - '0';
                            int value = state.ReadAsciiNumber(1, int.MaxValue);

                            if (value < 10 || firstDigit > 7 || value < state.CapturesCount /* case of trying to reference capture 0 is implicitly handled by the specific handling of \0xx above */)
                            {
                                // Backreference
                                throw new NotImplementedException("Backreferences are not implemented");
                            }
                            else
                            {
                                // Octal
                                snapshot.Restore();

                                return new StringNode(state.ReadOctalEscape(1, 3), state);
                            }
                        }
                    }
                    break;
            }

            throw new Exception($"Invalid escape character {state.Char.CodePointAsString()}");
        }

        private static IRegexNode ParseLiteral(RegexParseState state)
        {
            var chars = new List<int>();
            while (state.MoveNext())
            {
                if (state.Char == '\\' && state.MoveNextIf('E', true))
                {
                    break;
                }

                chars.Add(state.Char);
            }

            return new StringNode(chars, state);
        }
        #endregion
    }
}
