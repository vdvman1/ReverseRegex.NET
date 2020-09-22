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
            string response;
            do
            {
                Console.WriteLine($"Sample: {parsed.GenerateSample(rng)}");
                Console.Write("Generate another? (y/n) ");
                response = Console.ReadLine().ToLower().Trim();
            } while (response == "y" || response == "yes");
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

            while (state.MoveNext() && !ends.Contains(state.Char))
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

            if (!char.IsLetterOrDigit(state.Char.CodePointAsString(), 0))
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
                        if (!state.MoveNext() || state.Char < ' ' || state.Char > '~')
                        {
                            throw new Exception("Expected printable ascii character");
                        }
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
                    return new StringNode(state.ReadOctal(3), state);
                case 'o':
                    {
                        if (!state.MoveNext() || state.Char != '{')
                        {
                            throw new Exception(@"Octal escapes using \o must be followed by a '{' character");
                        }

                        if(!state.MoveNext())
                        {
                            throw new Exception(@"Octal escapes using \o{} must be contain octal digits");
                        }
                        int value = state.ReadOctal(int.MaxValue);

                        if(!state.MoveNext() || state.Char != '}')
                        {
                            throw new Exception(@"Octal escapes using \o{} must be contain only octal digits and end with a '}' character");
                        }

                        return new StringNode(value, state);
                    }
                default:
                    if(state.Char.IsAsciiDigit())
                    {
                        if (inCharacterClass)
                        {
                            if(state.Char > '7')
                            {
                                return new StringNode(state.Char, state);
                            }

                            return new StringNode(state.ReadOctal(3), state);
                        }
                        else
                        {
                            var snapshot = state.Snapshot();

                            int firstDigit = state.Char - '0';
                            var value = firstDigit;
                            while (state.TryPeekNext(out int c) && c.IsAsciiDigit())
                            {
                                state.MoveNext();
                                value = value * 10 + (c - '0');
                            }

                            if (value < 10 || firstDigit > 7 || value < state.CapturesCount /* case of trying to reference capture 0 is implicitly handled by the specific handling of \0xx above */)
                            {
                                // Backreference
                                throw new NotImplementedException("Backreferences are not implemented");
                            }
                            else
                            {
                                // Octal
                                snapshot.Restore();

                                return new StringNode(state.ReadOctal(3), state);
                            }
                        }
                    }
                    throw new Exception($"Invalid escape character {state.Char.CodePointAsString()}");
            }
        }

        private static IRegexNode ParseLiteral(RegexParseState state)
        {
            var chars = new List<int>();
            while(state.MoveNext())
            {
                if(state.Char == '\\' && state.TryPeekNext(out int nextC) && nextC == 'E')
                {
                    state.MoveNext();
                    break;
                }

                chars.Add(state.Char);
            }

            return new StringNode(chars, state);
        }
    }
}
