using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using HtmlAgilityPack;
using MP3_File_Auto_Tagger.Properties;
using TagLib;
using File = TagLib.File;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using Timer = System.Windows.Forms.Timer;

namespace MP3_File_Auto_Tagger
{
    public class item
    {
        [XmlAttribute]
        public string key;

        [XmlAttribute]
        public string value;
    }

    /// <summary>
    ///     Wrapper class so that we can return an IWin32Window given a hwnd
    /// </summary>
    public class WindowWrapper : IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }
    }

    internal class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private static bool doneDrawing;
        private static bool WindowHidden;
        public static ContextMenu menu;
        public static NotifyIcon notificationIcon;
        private static string path = @"D:\Music";
        private static bool Filter;
        private static string[] files;

        private static int tableWidth = 77;
        private static ConsoleColor _lastClr;

        /// <summary>
        ///     Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private static void DrawImage(string filePath)
        {
            var baseImg = Image.FromFile(filePath);
            Image Picture = ResizeImage(baseImg, baseImg.Width / 1, baseImg.Height / 1);
            Console.SetBufferSize(Picture.Width * 0x90, Picture.Height * 0x90);
            var Dimension = new FrameDimension(Picture.FrameDimensionsList[0x0]);
            int FrameCount = Picture.GetFrameCount(Dimension);
            int Left = Console.WindowLeft, Top = Console.WindowTop;
            char[] Chars = { '#', '#', '@', '%', '=', '+', '*', ':', '-', '.', ' ' };
            Picture.SelectActiveFrame(Dimension, 0x0);
            for (var i = 0x0; i < Picture.Height; i++)
            {
                for (var x = 0x0; x < Picture.Width; x++)
                {
                    var Color = ((Bitmap)Picture).GetPixel(x, i);
                    int Gray = (Color.R + Color.G + Color.B) / 0x3;
                    int Index = Gray * (Chars.Length - 0x1) / 0xFF;
                    Console.Write(Chars[Index]);
                }
                Console.Write('\n');
            }
            //  Console.SetCursorPosition(Left, Top); 
            doneDrawing = true;
        }
        public static void Do(
    Action action,
    TimeSpan retryInterval,
    int retryCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, retryCount);
        }
        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            int retryCount = 3)
        {
            var exceptions = new List<Exception>();

            for (var retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    if (retry > 0)
                        Thread.Sleep(retryInterval);
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }

        private static bool GoogleImageSearch(string query)
        {
            var lastLoc = Point.Empty;
            string entitized = HtmlEntity.Entitize(query);
            string url = string.Format("https://www.google.com.au/search?q={0}&tbm=isch",
                entitized + " song cover artwork");
            using (var client = new WebClient()) // WebClient class inherits IDisposable
            {
                client.Headers.Add("user-agent",
                    "Mozilla/5.0 (MeeGo; NokiaN9) AppleWebKit/534.13 (KHTML, like Gecko) NokiaBrowser/8.5.0 Mobile Safari/534.13");
                string htmlCode = client.DownloadString(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlCode);
                foreach (var eleImg in doc.DocumentNode.SelectSingleNode("//*[@id=\"images\"]").ChildNodes)
                {
                    string thumbNailUrl = eleImg.FirstChild.Attributes["src"].Value;
                    string tempPath = Path.GetTempFileName();

                    client.DownloadFile(thumbNailUrl, tempPath);

                    var rightmost = Screen.AllScreens[0];
                    foreach (
                        var screen in
                            Screen.AllScreens.Where(screen => screen.WorkingArea.Right > rightmost.WorkingArea.Right)
                        )
                    {
                        rightmost = screen;
                    }


                    string landingPg = HtmlEntity.DeEntitize(eleImg.Attributes["href"].Value);
                    var innerDoc = new HtmlDocument();
                    client.Headers.Add("user-agent",
                        "Mozilla/5.0 (MeeGo; NokiaN9) AppleWebKit/534.13 (KHTML, like Gecko) NokiaBrowser/8.5.0 Mobile Safari/534.13");
                    string inrHTml = client.DownloadString(landingPg);
                    innerDoc.LoadHtml(inrHTml);

                    foreach (
                        var element in
                            innerDoc.DocumentNode.Descendants()
                                .Where(
                                    element =>
                                        element.InnerText.Contains("full size") &&
                                        element.Attributes.Contains("href")))
                    {
                        using (var imgClient = new WebClient()) // WebClient class inherits IDisposable
                        {
                            tempPath = Path.GetTempFileName();

                            try
                            {
                                Do(() => imgClient.DownloadFile(element.Attributes["href"].Value, tempPath),
                                    TimeSpan.FromSeconds(1));
                            }
                            catch (AggregateException)
                            {
                                return false;
                            }

                        }
                    }
                    var noArtwork = false;
                    var applyArtwork = false;
                    var f = new Form
                    {
                        FormBorderStyle = FormBorderStyle.Fixed3D,
                        Size = new Size(500, 500),
                        Text = query,
                        UseWaitCursor = false
                    };
                    //f.Size = Image.FromFile(tempPath).Size;
                    f.BackgroundImage = ResizeImage(Image.FromFile(tempPath), f.Width, f.Height);
                    f.FormClosing += (l, m) => { lastLoc = f.Location; };
                    f.KeyDown += (k, fs) =>
                    {
                        if (fs.KeyCode == Keys.Enter)
                        {
                            applyArtwork = true;
                        }
                        else if (fs.KeyCode == Keys.Escape)
                            noArtwork = true;
                        f.Close();
                    };
                    f.Load += (o, e) =>
                    {
                        if (lastLoc == null)
                        {
                            f.Left = rightmost.WorkingArea.Right - f.Width;
                            f.Top = rightmost.WorkingArea.Bottom - f.Height;
                        }
                        else
                            f.Location = lastLoc;
                        f.TopMost = true;
                        f.Focus();
                        var t = new Timer { Interval = 5000 };
                        t.Tick += (k, z) =>
                        {
                            applyArtwork = true;
                            f.Close();
                        };
                        t.Enabled = true;
                    };
                    f.ShowDialog();
                    f.Cursor = Cursors.Default;
                    if (noArtwork)
                        return false;
                    if (!applyArtwork) continue;
                    AddArtwork(Path.Combine(path, query + ".mp3"), tempPath);
                    return true;

                }
                return false;
            }
        }

        private static void F_FormClosing(object sender, FormClosingEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static void AddArtwork(string filePath, string imgPath)
        {
            using (var file = File.Create(filePath))
            {
                IPicture artwork = new Picture(imgPath);
                file.Tag.Pictures = new IPicture[1] { artwork };
                file.Save();
            }
        }

        private static void F_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private static void AnalyseAllFiles()
        {
            if (Filter)
                path = Path.Combine(path, "filter");
            files = Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly);
            int fileCount = files.Count();
            foreach (string filePath in files)
            {
                if (!filePath.Contains(".ini"))
                {
                    using (var file = File.Create(filePath))
                    {
                        if (!file.Tag.Pictures.Any())
                        {
                            if (GoogleImageSearch(Path.GetFileNameWithoutExtension(filePath)))
                                ColoredConsoleWrite(ConsoleColor.Red,
                                    "Artwork for File ( " + Array.IndexOf(files, filePath) + "/" + fileCount + " )");
                        }
                        FixFile(filePath);
                    }
                }
            }
        }

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
            var monitor = new FileSystemWatcher(path, "*.mp3") { EnableRaisingEvents = true };
            monitor.Created += monitor_CreatedOrChanged;
            monitor.Renamed += monitor_CreatedOrChanged;
            AnalyseAllFiles();
            while (true)
            {
                switch (Console.ReadLine())
                {
                    case "filter":
                        Filter = true;
                        path = @"D:\Music - Copy";
                        ColoredConsoleWrite(ConsoleColor.Cyan, "Filter has been enabled.");
                        break;
                    case "regular":
                        Filter = false;
                        path = @"D:\Music";
                        ColoredConsoleWrite(ConsoleColor.Cyan, "Filter has been disabled.");
                        break;
                    case "analyse":
                        AnalyseAllFiles();
                        break;
                }
            }
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
            if (filePath != movePath && !System.IO.File.Exists(movePath))
                System.IO.File.Move(filePath, movePath);
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
                using (var file = File.Create(filePath))
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