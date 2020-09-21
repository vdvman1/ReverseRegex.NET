using ReverseRegex.Extensions;
using ReverseRegex.NET.Extensions;
using ReverseRegex.NET.RegexNodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ReverseRegex
{
    class Program
    {
        static void Main(string[] args)
        {
            string regexStr;
            if(args.Length > 0)
            {
                if(args[0].ToLower() == "--file")
                {
                    if(args.Length < 2)
                    {
                        Console.Error.WriteLine("Expected a file path");
                        return;
                    }

                    var path = string.Join(' ', args.Skip(1));
                    try
                    {
                        regexStr = File.ReadAllText(path);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                        return;
                    }
                }
                else
                {
                    regexStr = string.Join(' ', args);
                }
            }
            else
            {
                Console.Write("Regex> ");
                regexStr = Console.ReadLine();
            }

            int[] regex = regexStr.Normalize().ToCodePoints();
            var parsed = Parse(new RegexParseState(regex), new HashSet<int>());
            string response;
            do
            {
                Console.WriteLine($"Sample: {parsed.GenerateSample()}");
                Console.Write("Generate another? (y/n) ");
                response = Console.ReadLine().ToLower().Trim();
            } while (response == "y" || response == "yes");
        }

        private static IRegexNode Parse(RegexParseState state, ISet<int> ends)
        {
            var builder = new StringBuilder();
            while (state.MoveNext() && !ends.Contains(state.Char))
            {
                switch (state.Char)
                {
                    default:
                        builder.Append(state.Char.CodePointAsString());
                        break;
                }
            }
            return new StringNode(builder.ToString());
        }
    }
}
