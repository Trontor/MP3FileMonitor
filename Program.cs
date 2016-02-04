using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MP3_File_Auto_Tagger
{
    class Program
    {
        static void Main(string[] args)
        {
            FileSystemWatcher monitor = new FileSystemWatcher(@"D:\music", "*.mp3");
            monitor.EnableRaisingEvents = true;
            monitor.Created += monitor_CreatedOrChanged;
            monitor.Renamed += monitor_CreatedOrChanged;
            Console.ReadKey();
        }

        static void monitor_CreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            TagLib.File file = TagLib.File.Create(e.FullPath);
            if (e.Name.Contains("-"))
            {
                string[] split = e.Name.Replace(".mp3", "").Split('-');
                string Title = split[1].Trim();
                string Artist = split[0].Trim();
                file.Tag.Performers = null;
                file.Tag.Performers = new string[1] { Artist };
                file.Tag.Title = Title;
                file.Save();
            }
            Console.WriteLine(e.FullPath);
            file.Dispose();
        }
    }
}
