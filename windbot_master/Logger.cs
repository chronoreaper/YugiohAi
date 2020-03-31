using System;
using System.IO;

namespace WindBot
{
    public static class Logger
    {
        public static string Path = "log.txt";

        public static void DeleteFile()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }

        public static void WriteToFile(string str)
        {
            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(Path))
            {
                sw.WriteLine(str);
            }
        }
        public static void WriteLine(string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message);
        }
        public static void DebugWriteLine(string message)
        {
#if DEBUG
            Console.WriteLine("[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message);
#endif
        }
        public static void WriteErrorLine(string message)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Error.WriteLine("[" + DateTime.Now.ToString("yy-MM-dd HH:mm:ss") + "] " + message);
            Console.ResetColor();
        }
    }
}