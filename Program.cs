using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using File = TagLib.File;

namespace MP3_File_Auto_Tagger
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string path = @"D:\Music";
            FileSystemWatcher monitor = new FileSystemWatcher(path, "*.mp3") { EnableRaisingEvents = true };
            monitor.Created += monitor_CreatedOrChanged;
            monitor.Renamed += monitor_CreatedOrChanged; IntPtr handle = GetConsoleWindow();
            //  ShowWindow(handle, SW_HIDE);

            var files = Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly);

            foreach (string _fileName in files)
            {
                if (!_fileName.Contains(".ini"))
                {
                    string newFileName = _fileName;
                    string[] filePaths = _fileName.Replace(".mp3", "").Trim().Split(new string[] { "-" }, StringSplitOptions.None);
                    string startOfFile = filePaths[0];
                    string endOfFile = filePaths[1];
                    string fileNameWithoutExtension = _fileName.Replace(".mp3", "");
                     
                    if (startOfFile.Contains("(ft.") && startOfFile[startOfFile.Length - 1] != ')') 
                        newFileName = startOfFile.Insert(startOfFile.Length, ")") + " - " + endOfFile; 

                    if (endOfFile.Contains("(ft.") && endOfFile[endOfFile.Length - 1] != ')')
                        newFileName = fileNameWithoutExtension.Insert(fileNameWithoutExtension.Length, ")");

                    Console.WriteLine(newFileName);
                }
            }
            Console.ReadKey();
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        private static void monitor_CreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                File file = TagLib.File.Create(e.FullPath);
                if (e.Name.Contains("-"))
                {
                    string[] split = e.Name.Replace(".mp3", "").Split('-');
                    string title = split[1].Trim();
                    string artist = split[0].Trim();
                    file.Tag.Performers = null;
                    file.Tag.Performers = new string[1] { artist };
                    file.Tag.Title = title;
                    file.Save();
                }
                Console.WriteLine(e.FullPath);
                file.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}
