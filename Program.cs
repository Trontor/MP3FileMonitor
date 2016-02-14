using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using MP3_File_Auto_Tagger.Properties;

namespace MP3_File_Auto_Tagger
{
    public class item
    {
        [XmlAttribute]
        public string key;

        [XmlAttribute]
        public string value;
    }

    internal class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private static bool WindowHidden;
        public static ContextMenu menu;
        public static NotifyIcon notificationIcon;
        private static string path = @"D:\Music";
        private static readonly bool Filter = true;
        private static string[] files;

        private static int tableWidth = 77;
        private static ConsoleColor _lastClr;

        private static void Main(string[] args)
        {
            if (Filter)
                path = @"D:\Music - Copy";
            ShowWindow(GetConsoleWindow(), SW_HIDE);
            WindowHidden = true;
            var notifyThread = new Thread(
                delegate ()
                {
                    menu = new ContextMenu();

                    menu.MenuItems.Add(0, new MenuItem("List Artists", mnuArtists_Click));
                    menu.MenuItems.Add(1, new MenuItem("Exit", mnuExit_Click));

                    notificationIcon = new NotifyIcon
                    {
                        Icon = Resources.mp34,
                        ContextMenu = menu,
                        Text = "MP3 File Monitor -- Made for Rohyl, by Rohyl"
                    };
                    notificationIcon.DoubleClick += NotificationIcon_Click;

                    notificationIcon.Visible = true;
                    Application.Run();
                }
                );
            files = Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly);
            notifyThread.Start();
            if (Filter)
                path = Path.Combine(path, "filter");
            var monitor = new FileSystemWatcher(path, "*.mp3") { EnableRaisingEvents = true };
            monitor.Created += monitor_CreatedOrChanged;
            monitor.Renamed += monitor_CreatedOrChanged;
            files = Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly);
            foreach (string filePath in files)
            {
                if (!filePath.Contains(".ini"))
                {
                    FixFile(filePath);
                }
            }
            Console.ReadKey();
        }

        private static void NotificationIcon_Click(object sender, EventArgs e)
        {
            if (WindowHidden)
            {
                WindowHidden = false;
                ShowWindow(GetConsoleWindow(), SW_SHOW);
            }
            else
            {
                WindowHidden = true;
                ShowWindow(GetConsoleWindow(), SW_HIDE);
            }
        }


        public static void Space()
        {
            Console.WriteLine("");
        }

        public static void ColoredConsoleWrite(ConsoleColor color, string text, bool writeOnly = false)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            if (writeOnly)
                Console.Write(text);
            else
                Console.WriteLine(text);
            Console.ForegroundColor = originalColor;
        }

        private static T GetRandomEnum<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>().OrderBy(e => Guid.NewGuid()).First();
        }

        private static void mnuExit_Click(object sender, EventArgs e)
        {
            notificationIcon.Dispose();
            Application.Exit();
            Environment.Exit(0);
        }

        private static void FixFile(string filePath)
        {
            var file = new Mp3File();
            string finalName = file.FixFileName(filePath);
            string movePath = Path.Combine(path, finalName + ".mp3");
            if (filePath != movePath && !File.Exists(movePath))
                File.Move(filePath, movePath);
            ColoredConsoleWrite(ConsoleColor.Green, finalName);
        }

        private static void PrintLine()
        {
            Console.WriteLine(new string('-', tableWidth));
        }

        private static void PrintRow(params string[] columns)
        {
            int width = (tableWidth - columns.Length) / columns.Length;
            var row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }

            Console.WriteLine(row);
        }

        private static string AlignCentre(string text, int width)
        {
            text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

            if (string.IsNullOrEmpty(text))
            {
                return new string(' ', width);
            }
            return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
        }

        private static void mnuArtists_Click(object sender, EventArgs e)
        {
            ShowWindow(GetConsoleWindow(), 3);

            files = Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly);
            var list = new List<string>();
            var titles = new List<string>();
            foreach (string filePath in files)
            {
                if (filePath.Contains(".ini")) continue;
                using (var file = TagLib.File.Create(filePath))
                {
                    foreach (string artist in file.Tag.Performers.Where(artist => !list.Contains(artist)))
                    {
                        list.Add(artist.Trim());
                    }
                    if (titles.Contains(file.Tag.Title))
                    {
                        ColoredConsoleWrite(ConsoleColor.Yellow, "Duplicate Title:" + filePath);
                    }
                    else
                        titles.Add(file.Tag.Title);
                    file.Dispose();
                }
            }
            string longeststring = list.OrderByDescending(s => s.Length).First();
            tableWidth = longeststring.Length * 7 + 5;
            var strings = DivideStrings(7, list.ToArray());
            foreach (var strs in strings)
            {
                PrintRow(strs.ToArray());
            }
        }

        private static T[] CopyPart<T>(T[] array, int index, int length)
        {
            var newArray = new T[length];
            Array.Copy(array, index, newArray, 0, length);
            return newArray;
        }

        private static List<string[]> DivideStrings(int expectedStringsPerArray, string[] allStrings)
        {
            var arrays = new List<string[]>();

            int arrayCount = allStrings.Length / expectedStringsPerArray;

            int elemsRemaining = allStrings.Length;
            for (int arrsRemaining = arrayCount; arrsRemaining >= 1; arrsRemaining--)
            {
                int elementCount = elemsRemaining / arrsRemaining;

                var array = CopyPart(allStrings, elemsRemaining - elementCount, elementCount);
                arrays.Insert(0, array);

                elemsRemaining -= elementCount;
            }

            return arrays;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void monitor_CreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                files = Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly);
                FixFile(e.FullPath);
            }
            catch
            {
                // ignored
            }
        }
    }
}