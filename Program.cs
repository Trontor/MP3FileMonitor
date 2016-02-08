using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MP3_File_Auto_Tagger
{
    internal class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private static readonly bool Filter = false;

        private static void Main(string[] args)
        {
            var path = @"D:\Music - Copy";
            if (Filter)
                path = Path.Combine(path, "filter");
            //var monitor = new FileSystemWatcher(path, "*.mp3") { EnableRaisingEvents = true };
            //monitor.Created += monitor_CreatedOrChanged;
            //monitor.Renamed += monitor_CreatedOrChanged;
            var handle = GetConsoleWindow();
            //  ShowWindow(handle, SW_HIDE);

            var files = Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly);
            foreach (string filePath in files)
            {
                if (!filePath.Contains(".ini"))
                {
                    var file = new Mp3File();
                    string finalName = file.FixFileName(filePath);
                    string movePath = Path.Combine(path, finalName + ".mp3");
                    if (filePath != movePath && !File.Exists(movePath))
                        File.Move(filePath, movePath);
                    Console.WriteLine(finalName);
                }
            }
            Console.ReadKey();
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void monitor_CreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
            }
            catch
            {
                // ignored
            }
        }
    }
}