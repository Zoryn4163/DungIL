using System;
using System.IO;
using System.Text;

namespace DungILModLoader
{
    public static class Log
    {
        public static string LogPath { get; private set; }
        public static StreamWriter LogStream { get; private set; }

        public static void Initialize(string logFilePath)
        {
            if (File.Exists(logFilePath))
                File.Delete(logFilePath);


            LogPath = logFilePath;
            //LogStream = new StreamWriter(LogPath, false, Encoding.UTF8);
            LogStream = File.CreateText(logFilePath);
            LogStream.AutoFlush = true;
        }

        public static void Out(object o)
        {
            LogStream.WriteLine($"[{DateTime.Now}] {o.ToString()}");
        }
    }
}
