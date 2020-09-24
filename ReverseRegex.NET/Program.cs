using ReverseRegex.Extensions;
using ReverseRegex.NET.RegexNodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ReverseRegex
{
    class Program
    {
        static void Main(string[] args)
        {
            string? regexStr = null;
            bool caseSensitive = true;
            for (int i = 0; i < args.Length; i++)
            {
                if(args[i].StartsWith("--"))
                {
                    switch(args[i].Substring(2))
                    {
                        case "file":
                            {
                                i++;
                                if (i >= args.Length)
                                {
                                    Console.Error.WriteLine("Expected a file path");
                                    return;
                                }

                                var path = new StringBuilder();
                                for(; i < args.Length && !args[i].StartsWith("--"); i++)
                                {
                                    if(path.Length > 0)
                                    {
                                        path.Append(' ');
                                    }
                                    path.Append(args[i]);
                                }
                                i--;

                                try
                                {
                                    regexStr = File.ReadAllText(path.ToString());
                                }
                                catch (Exception e)
                                {
                                    Console.Error.WriteLine(e.Message);
                                    return;
                                }
                            }
                            break;
                        case "case-insensitive":
                            caseSensitive = false;
                            break;
                        case "regex":
                            {
                                var regexBuilder = new StringBuilder();
                                for (; i < args.Length; i++)
                                {
                                    if (regexBuilder.Length > 0)
                                    {
                                        regexBuilder.Append(' ');
                                    }
                                    regexBuilder.Append(args[i]);
                                }
                            }
                            break;
                    }
                }
            }
            if(regexStr is null)
            {
                Console.Write("Regex> ");
                regexStr = Console.ReadLine();
            }

            int[] regex = regexStr.Normalize().ToCodePoints();
            var parsed = Parse(
                new RegexParseState(regex)
                {
                    CaseSensitive = caseSensitive
                },
                new HashSet<int>()
            );
            var rng = new Random();
            ConsoleKeyInfo response;
            do
            {
                Console.WriteLine($"Sample: {parsed.GenerateSample(rng)}");
                Console.Write("Generate another? (y/n) ");
                response = Console.ReadKey();
                Console.WriteLine();
            } while (response.KeyChar == 'y');
        }

        private static IRegexNode Parse(RegexParseState state, ISet<int> ends)
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
                    default:
                        str.Add(state.Char);
                        break;
                }
            }

            var lastNode = new StringNode(str, state);
            if(nodes.Count == 0)
            {
                return lastNode;
            }

            nodes.Add(lastNode);
            return new SequenceNode(nodes);
        }

        private static IRegexNode ParseEscape(RegexParseState state, bool inCharacterClass)
        {
            if(!state.MoveNext())
            {
                throw new Exception("Expected a character that is being escaped");
            }

            if (!state.Char.IsLetterOrDigit())
            {
                return new StringNode(state.Char, state);
            }

            switch(state.Char)
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
                            if(!state.MoveNext())
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

                        if(c == '{')
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
                case 'b':
                    if(inCharacterClass)
                    {
                        return new StringNode('\b', state);
                    }
                    break;
                default:
                    if(state.Char.IsAsciiDigit())
                    {
                        if (inCharacterClass)
                        {
                            if(state.Char == '8' || state.Char == '9')
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
            while(state.MoveNext())
            {
                if(state.Char == '\\' && state.MoveNextIf('E', true))
                {
                    break;
                }

                chars.Add(state.Char);
            }

            return new StringNode(chars, state);
        }
    }
}
