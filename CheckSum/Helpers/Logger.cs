using System;
using System.IO;

namespace CheckSum.Helpers
{
    public static class Logger
    {
       public static bool IsRedirected { get; set; }


        static Logger()
        {
            try
            {
                IsRedirected = Console.CursorVisible && false;
            }
            catch
            {
                IsRedirected = true;
            }
        }

        public static void Error(string format, params object[] args)
        {
            Console.ForegroundColor=ConsoleColor.Red;
            Console.Error.WriteLine(format,args);
            Console.ResetColor();
        }
        public static void Warning(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine(format, args);
            Console.ResetColor();
        }

        public static void Info(string format, params object[] args)
        {
            Console.Error.WriteLine(format, args);
        }

        public static void Output(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
        
        internal static void ResultSuccess(string prefix, string format, params object[] args)
        {
            Console.Write(prefix);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Out.WriteLine(format, args);
            Console.ResetColor();
        }

        internal static void ResultFailure(string prefix, string format, params object[] args)
        {
            Console.Write(prefix);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Out.WriteLine(format, args);
            Console.ResetColor();
        }
        internal static void ResultWarning(string prefix, string format, params object[] args)
        {
            Console.Write(prefix);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Out.WriteLine(format, args);
            Console.ResetColor();
        }

        internal static void Progress(string progressMessage, ref int? lineIndex)
        {
            if (!IsRedirected)
            {
                int currentLineIndex = Console.CursorTop;
                int currentColIndex = Console.CursorLeft;
                bool flag = lineIndex.HasValue;
                if (flag)
                    Console.CursorTop = lineIndex.Value;
                else
                    lineIndex = Console.CursorTop;

                Console.CursorLeft = 0;

                Console.Write((progressMessage + new string(' ', Console.BufferWidth)).Substring(0,
                    Console.BufferWidth));

                Console.CursorTop = currentLineIndex;
                Console.CursorLeft = currentColIndex;
                if (!flag)
                    Console.WriteLine();
            }
            else
            {
                Console.WriteLine(progressMessage);
            }

        }

        public static TextWriter ErrorTextWriter { get { return Console.Error; } }
    }
}