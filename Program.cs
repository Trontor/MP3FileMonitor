using System;
using System.IO;
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
        private static bool WindowHidden = false;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        public static ContextMenu menu;
        public static MenuItem mnuExit;
        public static NotifyIcon notificationIcon;
        private static string path = @"D:\Music";
        private static readonly bool Filter = false;

        private static void Main(string[] args)
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
            WindowHidden = true;
            var notifyThread = new Thread(
                delegate ()
                {
                    menu = new ContextMenu();
                    mnuExit = new MenuItem("Exit");
                    menu.MenuItems.Add(0, mnuExit);

                    notificationIcon = new NotifyIcon
                    {
                        Icon = Resources.mp34,
                        ContextMenu = menu,
                        Text = "Main"
                    };
                    notificationIcon.Click += NotificationIcon_Click;
                    mnuExit.Click += mnuExit_Click;

                    notificationIcon.Visible = true;
                    Application.Run();
                }
                );
            notifyThread.Start();
            if (Filter)
                path = Path.Combine(path, "filter");
            var monitor = new FileSystemWatcher(path, "*.mp3") { EnableRaisingEvents = true };
            monitor.Created += monitor_CreatedOrChanged;
            monitor.Renamed += monitor_CreatedOrChanged; 
            var files = Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly);
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

        private static void mnuExit_Click(object sender, EventArgs e)
        {
            notificationIcon.Dispose();
            Application.Exit();
        }

        private static void FixFile(string FilePath)
        {
            var file = new Mp3File();
            string finalName = file.FixFileName(FilePath);
            string movePath = Path.Combine(path, finalName + ".mp3");
            if (FilePath != movePath && !File.Exists(movePath))
                File.Move(FilePath, movePath);
            Console.WriteLine(finalName);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void monitor_CreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                FixFile(e.FullPath);
            }
            catch
            {
                // ignored
            }
        }
    }
}