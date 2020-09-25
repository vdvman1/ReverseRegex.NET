using ReverseRegex.Extensions;
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
            Console.InputEncoding = Console.OutputEncoding = Encoding.Unicode;
            ConsoleHelper.Initialise();
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

            Regex regex;
            try
            {
                regex = Regex.Build(regexStr, new RegexOptions
                {
                    CaseSensitive = caseSensitive
                });
            }
            catch (RegexParseException e)
            {
                e.Print();
                return;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return;
            }
            var rng = new Random();
            ConsoleKeyInfo response;
            do
            {
                Console.WriteLine($"Sample: {regex.GenerateSample(rng)}");
                Console.Write("Generate another? (y/n) ");
                response = Console.ReadKey();
                Console.WriteLine();
            } while (response.KeyChar == 'y');
        }
    }
}
