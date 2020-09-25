using System;
using System.Collections.Generic;
using System.Text;

namespace ReverseRegex
{
    public static class ConsoleHelper
    {
        private static bool initialised = false;
        private static bool supportsCombining = false;

        public static bool SupportsCombining => initialised ? supportsCombining : throw new InvalidOperationException($"{nameof(ConsoleHelper)} has not been initialised");

        public static void Initialise()
        {
            if (initialised) return;

            Console.WriteLine("Gathering console properties");
            if (Console.IsOutputRedirected)
            {
                // Assume it is being output to a file
                supportsCombining = true;
            }
            else
            {
                var start = Console.CursorLeft;
                Console.Write("k̀");
                supportsCombining = Console.CursorLeft - start == 1;
                Console.WriteLine();
            }
            Console.WriteLine($"Supports combining characters: {supportsCombining}");

            initialised = true;
        }
    }
}
