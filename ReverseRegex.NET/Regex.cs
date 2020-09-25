using ReverseRegex.Extensions;
using ReverseRegex.NET.RegexNodes;
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
            => new Regex(ParseAlternatives(new RegexParseState(regexStr.Normalize().ToCodePoints(), options)));

        private static IRegexNode ParseAlternatives(RegexParseState state)
        {
            var alternatives = new List<IRegexNode>();
            do
            {
                alternatives.Add(ParseSequence(state, new HashSet<int> { '|' }));
                if(state.HasNext)
                {
                    state.Require('|');
                }
                else
                {
                    break;
                }
            } while (true);

            return alternatives.Count switch
            {
                0 => new EmptyNode(),
                1 => alternatives[0],
                _ => new AlternatesNode(alternatives)
            };
        }

        private static IRegexNode ParseSequence(RegexParseState state, ISet<int> ends)
        {
            var nodes = new List<IRegexNode>();

            while (state.MoveNextIf(ends, false))
            {
                switch (state.Char)
                {
                    case '\\':
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
                        if(nodes.Count == 0)
                        {
                            throw state.Error("Cannot repeat nothing");
                        }

                        if (!nodes[^1].AllowsRepetition)
                        {
                            throw state.Error("Invalid repetition");
                        }

                        nodes[^1] = new RepeatNode(nodes[^1], 0, int.MaxValue);
                        break;
                    case '+':
                        if (nodes.Count == 0)
                        {
                            throw state.Error("Cannot repeat nothing");
                        }

                        if (!nodes[^1].AllowsRepetition)
                        {
                            if(nodes[^1] is QuantifierNode quantifierNode)
                            {
                                if(quantifierNode.HasModifier)
                                {
                                    throw state.Error("Cannot specify multiple quantifier modifiers at once");
                                }
                                quantifierNode.SetPossesive();
                            }

                            throw state.Error("Invalid repetition");
                        }

                        nodes[^1] = new RepeatNode(nodes[^1], 1, int.MaxValue);
                        break;
                    case '?':
                        if(nodes.Count == 0)
                        {
                            throw state.Error("Cannot make nothing optional");
                        }

                        if (!nodes[^1].AllowsRepetition)
                        {
                            if (nodes[^1] is QuantifierNode quantifierNode)
                            {
                                if (quantifierNode.HasModifier)
                                {
                                    throw state.Error("Cannot specify multiple quantifier modifiers at once");
                                }
                                quantifierNode.SetLazy();
                            }

                            throw state.Error("Invalid repetition");
                        }

                        nodes[^1] = new OptionalNode(nodes[^1]);
                        break;
                    case '{':
                        if (nodes.Count == 0)
                        {
                            throw state.Error("Cannot repeat nothing");
                        }

                        if (!nodes[^1].AllowsRepetition)
                        {
                            throw state.Error("Invalid repetition");
                        }

                        {
                            int min = 0;
                            int max = int.MaxValue;
                            using (state.BeginMatch(new int[0], '}'))
                            {
                                state.RequireNext();
                                min = state.ReadAsciiNumber(0, int.MaxValue);
                                if (state.MoveNextIf(',', true) && state.MoveNextIf('}', false))
                                {
                                    max = state.ReadAsciiNumber(1, int.MaxValue);
                                    if (max < min)
                                    {
                                        throw state.Error("Max cannot be less than min");
                                    }
                                }
                            }
                            nodes[^1] = new RepeatNode(nodes[^1], min, max);
                        }
                        break;
                    default:
                        nodes.Add(new CharNode(state.Char, state));
                        break;
                }
            }

            return SequenceNode.From(nodes);
        }

        private static IRegexNode ParseEscape(RegexParseState state, bool inCharacterClass)
        {
            state.RequireNext();

            if (!state.Char.IsLetterOrDigit())
            {
                return new CharNode(state.Char, state);
            }

            switch (state.Char)
            {
                case 'Q':
                    return ParseLiteral(state);
                case 'a':
                    return new CharNode('\a', state);
                case 'c':
                    {
                        state.RequireRange(' ', '~');
                        // Convert lowercase to upper case, and invert bit 6. See \cx in the pcre docs: http://www.rexegg.com/pcre-doc/_latestpcre2/pcre2pattern.html#SEC5
                        var ctrlC = state.Char.CodePointAsString().ToUpper().ToCodePoints()[0] ^ (1 << 6);
                        return new CharNode(ctrlC, state);
                    }
                case 'e': // "escape" character
                    return new CharNode(0x1B, state);
                case 'f':
                    return new CharNode('\f', state);
                case 'n':
                    return new CharNode('\n', state);
                case 'r':
                    return new CharNode('\r', state);
                case 't':
                    return new CharNode('\t', state);
                case '0':
                    return new CharNode(state.ReadOctalEscape(0, 3), state);
                case 'o':
                    {
                        int value;
                        using (state.BeginMatch('{', '}'))
                        {
                            state.RequireNext();
                            value = state.ReadOctalEscape(1, int.MaxValue);
                        }

                        return new CharNode(value, state);
                    }
                case 'x':
                    {
                        if (!state.TryPeekNext(out int c))
                        {
                            return new CharNode(0, state);
                        }

                        if (c == '{')
                        {
                            int value;
                            using (state.BeginMatch('{', '}'))
                            {
                                state.RequireNext();
                                value = state.ReadHexEscape(1, int.MaxValue);
                            }

                            return new CharNode(value, state);
                        }
                        else
                        {
                            state.MoveNext(); // Guaranteed to succeed thanks to the TryPeekNext earlier
                            return new CharNode(state.ReadHexEscape(0, 2), state);
                        }
                    }
                case 'N':
                    {
                        if (state.TryPeekNext(out int c) && c == '{')
                        {
                            int value;
                            using (state.BeginMatch("{U+".ToCodePoints(), '}'))
                            {
                                state.RequireNext();
                                if (!state.Char.IsHexDigit())
                                {
                                    throw state.Error("Expected 1 or more hex digits");
                                }

                                value = state.ReadHexEscape(1, int.MaxValue);
                            }

                            return new CharNode(value, state);
                        }
                        else
                        {
                            throw new NotImplementedException("Character classes have not been implemented");
                        }
                    }
                case 'b':
                    if (inCharacterClass)
                    {
                        return new CharNode('\b', state);
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
                                return new CharNode(state.Char, state);
                            }

                            return new CharNode(state.ReadOctalEscape(1, 3), state);
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

                                return new CharNode(state.ReadOctalEscape(1, 3), state);
                            }
                        }
                    }
                    break;
            }

            throw state.Error($"Invalid escape character {state.Char.CodePointAsString()}");
        }

        private static IRegexNode ParseLiteral(RegexParseState state)
        {
            var nodes = new List<IRegexNode>();
            while (state.MoveNext())
            {
                if (state.Char == '\\' && state.MoveNextIf('E', true))
                {
                    break;
                }

                nodes.Add(new CharNode(state.Char, state));
            }

            return SequenceNode.From(nodes);
        }
        #endregion
    }
}
