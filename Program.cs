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
using System.Web;
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
    public class Item
    {
        [XmlAttribute]
        public string Key;

        [XmlAttribute]
        public string Value;
    }

    /// <summary>
    ///     Wrapper class so that we can return an IWin32Window given a hwnd
    /// </summary>

    internal class Program
    {
        private const int SwHide = 0;
        private const int SwShow = 5;

        private static bool _windowHidden;
        public static ContextMenu Menu;
        public static NotifyIcon NotificationIcon;
        private static string _path = @"D:\Music";
        private static bool _filter;
        private static string[] _files;

        private static int _tableWidth = 77;

        private static int _processedArtwork;

        /// <summary>
        /// Resize the image to the specified width and height.
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

        [STAThread]
        private static void Main(string[] args)
        {
            if (_filter)
                _path = @"D:\Music - Copy";
            ShowWindow(GetConsoleWindow(), SwHide);
            _windowHidden = true;
            var notifyThread = new Thread(
                delegate ()
                {
                    Menu = new ContextMenu();

                    Menu.MenuItems.Add(0, new MenuItem("List Artists", mnuArtists_Click));
                    Menu.MenuItems.Add(1, new MenuItem("Exit", mnuExit_Click));

                    NotificationIcon = new NotifyIcon
                    {
                        Icon = Resources.mp34,
                        ContextMenu = Menu,
                        Text = "MP3 File Monitor -- Made for Rohyl, by Rohyl"
                    };
                    NotificationIcon.DoubleClick += NotificationIcon_Click;

                    NotificationIcon.Visible = true;
                    Application.Run();
                }
                );
            _files = Directory.GetFiles(_path, "*.mp3", SearchOption.TopDirectoryOnly);
            notifyThread.Start();
            var monitor = new FileSystemWatcher(_path, "*.mp3") { EnableRaisingEvents = true };
            monitor.Created += monitor_CreatedOrChanged;
            monitor.Renamed += monitor_CreatedOrChanged;
            AnalyseAllFiles();
            ScanAriaCharts();
            while (true)
            {
                switch (Console.ReadLine())
                {
                    case "filter":
                        _filter = true;
                        _path = @"D:\Music - Copy";
                        ColoredConsoleWrite(ConsoleColor.Cyan, "Filter has been enabled.");
                        break;
                    case "regular":
                        _filter = false;
                        _path = @"D:\Music";
                        ColoredConsoleWrite(ConsoleColor.Cyan, "Filter has been disabled.");
                        break;
                    case "analyse":
                        AnalyseAllFiles();
                        break;
                    case "dump artwork":
                        DumpArtwork();
                        break;
                }
            }
        }

        private static bool GoogleImageSearch(string query, bool showdialog = false)
        {
            string entitized = HttpUtility.UrlEncode(query);
            string url = string.Format("https://www.google.com.au/search?q={0}&tbm=isch",
                entitized + " song cover artwork");
            using (var client = new WebClient()) // WebClient class inherits IDisposable
            {
                client.Headers.Add("user-agent",
                    "Mozilla/5.0 (MeeGo; NokiaN9) AppleWebKit/534.13 (KHTML, like Gecko) NokiaBrowser/8.5.0 Mobile Safari/534.13");
                string htmlCode = client.DownloadString(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlCode);
                for (var index = 0;
                    index < doc.DocumentNode.SelectSingleNode("//*[@id=\"images\"]").ChildNodes.Count;
                    index++)
                {
                    if (index < 0)
                        index = 0;
                    var eleImg = doc.DocumentNode.SelectSingleNode("//*[@id=\"images\"]").ChildNodes[index];
                    string thumbNailUrl = eleImg.FirstChild.Attributes["src"].Value;
                    string tempPath = Path.GetTempFileName();

                    client.DownloadFile(thumbNailUrl, tempPath);

                    var rightmost = Screen.AllScreens[0];
                    foreach (
                        var screen in
                            Screen.AllScreens.Where(screen => screen.WorkingArea.Right > rightmost.WorkingArea.Right))
                    {
                        rightmost = screen;
                    }


                    string landingPg = HtmlEntity.DeEntitize(eleImg.Attributes["href"].Value);
                    var innerDoc = new HtmlDocument();
                    client.Headers.Add("user-agent", "Mozilla/5.0 (MeeGo; NokiaN9) AppleWebKit/534.13 (KHTML, like Gecko) NokiaBrowser/8.5.0 Mobile Safari/534.13");
                    string inrHTml = client.DownloadString(landingPg);
                    innerDoc.LoadHtml(inrHTml);

                    foreach (
                        var element in
                            innerDoc.DocumentNode.Descendants()
                                .Where(
                                    element =>
                                        element.InnerText.Contains("full size") && element.Attributes.Contains("href")))
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
                    if (Settings.Default.LastSize.Height == 0 || Settings.Default.LastSize.Width == 0)
                    {
                        Settings.Default.LastSize = new Size(500, 500);
                    }


                    if (showdialog)
                    {
                        var f = new Form
                        {
                            FormBorderStyle = FormBorderStyle.SizableToolWindow,
                            Size = Settings.Default.LastSize,
                            UseWaitCursor = false
                        };
                        f.Resize += (i, u) =>
                        {
                            var control = (Control)i;
                            control.Width = control.Height;
                        };
                        //f.Size = Image.FromFile(tempPath).Size;
                        f.BackgroundImage = ResizeImage(Image.FromFile(tempPath), f.Width, f.Height);
                        f.FormClosing += (l, m) =>
                        {
                            Settings.Default.LastSize = f.Size;
                            Settings.Default.LastLocation = f.Location;
                        };
                        f.KeyDown += (k, fs) =>
                        {
                            if (fs.KeyCode == Keys.Enter)
                            {
                                applyArtwork = true;
                            }
                            else if (fs.KeyCode == Keys.Escape)
                            {
                                noArtwork = true;
                            }
                            else if (fs.KeyCode == Keys.Left)
                            {
                                index -= 2;
                            }
                            f.Close();
                        };

                        f.Text = query;
                        f.Load += (o, e) =>
                        {
                            if (Settings.Default.LastLocation.X == 0)
                            {
                                f.Left = rightmost.WorkingArea.Right - f.Width;
                                f.Top = rightmost.WorkingArea.Bottom - f.Height;
                            }
                            else
                                f.Location = Settings.Default.LastLocation;
                            f.TopMost = true;
                            var t = new Timer { Interval = 10000 };
                            t.Tick += (k, z) =>
                            {
                                applyArtwork = true;
                                f.Close();
                            };
                            t.Enabled = true;
                        };
                        f.TopMost = true;
                        f.ShowDialog();
                    }
                    else
                        applyArtwork = true;
                    Settings.Default.Save();
                    if (noArtwork)
                        return false;
                    if (!applyArtwork) continue;
                    AddArtwork(Path.Combine(_path, query + ".mp3"), tempPath);
                    return true;
                }
                return false;
            }
        }

        private static string RetreiveLyrics(string filePath)
        {
            string lyrics = "";
            TrontorMP3File file = new TrontorMP3File(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileTitle = fileName.Split('-')[0].ToLower();
            string fileArtist = fileName.Split('-')[1].ToLower();
            string entitized = HttpUtility.UrlEncode(fileArtist + " - " + fileTitle);
            string url = string.Format("https://www.google.com.au/search?&q={0}+site%3Aazlyrics.com", entitized);
            using (var client = new WebClient()) // WebClient class inherits IDisposable
            {
                client.Headers.Add("user-agent", "Mozilla/5.0 (MeeGo; NokiaN9) AppleWebKit/534.13 (KHTML, like Gecko) NokiaBrowser/8.5.0 Mobile Safari/534.13");
                string htmlCode = client.DownloadString(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlCode);
                var resultNodes =
                    doc.DocumentNode.SelectSingleNode("//*[@id=\"universal\"]")
                        .ChildNodes;

                var htmlNodes = (IList<HtmlNode>)resultNodes ?? resultNodes.ToList();
                for (var index = 0; index < htmlNodes.Count(); index++)
                {
                    var innerNode = htmlNodes[index].ChildNodes[0];
                    string searchTitle = innerNode.ChildNodes[0].InnerText.ToLower();
                    string azLyricsUrl = innerNode.ChildNodes[0].Attributes["href"].Value.Replace("/url?q=", "");
                    if (searchTitle.Contains(fileArtist) && searchTitle.Contains(fileTitle))
                    {
                        Thread.Sleep(1000);
                        using (var webclient = new WebClient()) // WebClient class inherits IDisposable
                        {
                            string lyricsHtml = webclient.DownloadString(azLyricsUrl.Split('&')[0]);
                            var lyricsDoc = new HtmlDocument();
                            lyricsDoc.LoadHtml(lyricsHtml);
                            var lyricNodes = lyricsDoc.DocumentNode.SelectSingleNode("/html/body/div[3]/div/div[2]/div[6]");
                            lyrics = lyricNodes.InnerText.Replace(
                                    @"\r\n<!-- Usage of azlyrics.com content by any third-party lyrics provider is prohibited by our licensing agreement. Sorry about that. -->\r\n",
                                    "");
                        }
                    }
                }
            }
            return lyrics;
        }

        public static bool AddArtwork(string filePath, string imgPath)
        {
            using (var file = File.Create(filePath))
            {
                IPicture artwork = new Picture(imgPath);
                Image currentImage;
                Image newImage;
                long currentSize = 0, newSize = 0;
                if (file.Tag.Pictures.Any())
                {
                    IPicture currentArtwork = file.Tag.Pictures[0];
                    using (var ms = new MemoryStream(currentArtwork.Data.Data))
                    {
                        currentImage = Image.FromStream(ms);
                        currentSize = ms.Length;
                    }
                    using (var ms = new MemoryStream(artwork.Data.Data))
                    {
                        newImage = Image.FromStream(ms);
                        newSize = ms.Length;
                    }
                }
                file.Tag.Pictures = new IPicture[1] { artwork };
                file.Save();
                return (currentSize == newSize);
            }
        }
        
        private static bool completed = false;
        private static WebBrowser ariaBrowser = new WebBrowser();
        private static void ScanAriaCharts()
        {
            string siteUrl = "http://www.ariacharts.com.au/Charts/Singles-Chart";
            ariaBrowser.DocumentCompleted += Program.AriaBrowserDocumentCompleted;
            ariaBrowser.Navigate(siteUrl);
            while (!completed)
            {
                Application.DoEvents();
                Thread.Sleep(100);
            }
        }

        private static void AriaBrowserDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(ariaBrowser.DocumentText);
            string site_XPath = "//*[@id=\"dvChartItems\"]";
            var node = doc.DocumentNode.SelectSingleNode(site_XPath);
            for (var index = 0; index < node.ChildNodes.Count; index++)
            {
                var item_Row = doc.DocumentNode.SelectSingleNode(site_XPath).ChildNodes[index];
                foreach (var nodes in item_Row.ChildNodes)
                {
                    if (nodes.Attributes["class"].Value.Contains("title-artist"))
                    {
                        Console.WriteLine(nodes.ChildNodes[0].InnerText);
                    }

                }
            }
        }

        private static void AnalyseAllFiles()
        {
            Dictionary<string, string> changedFiles = new Dictionary<string, string>();
            ColoredConsoleWrite(ConsoleColor.Yellow, "Processing song names...");
            _filter = false;
            if (_filter)
                _path = Path.Combine(@"D:\Music - Copy", "filter");
            _files = Directory.GetFiles(_path, "*.mp3", SearchOption.TopDirectoryOnly);

            foreach (string filePath in _files)
            {
                if (!filePath.Contains(".ini"))
                    using (var file = File.Create(filePath))
                    {
                        if (!file.Tag.Pictures.Any())
                        {
                            new Thread(() =>
                            {
                                try
                                {
                                    Do(() => ProcessArtwork(filePath), TimeSpan.FromSeconds(2));
                                }
                                catch
                                {
                                    // ignored
                                }
                            }).Start();
                            Thread.Sleep(1000);
                        }
                    }
                //string lyrics = RetreiveLyrics(filePath);
                //if (lyrics != "")
                //{
                //    Console.WriteLine(filePath);
                //    Console.Write(lyrics);
                //    Console.ReadKey();
                //}
                string fixedFileName = FixFile(filePath);
                if (fixedFileName != Path.GetFileNameWithoutExtension(filePath))
                {
                    changedFiles.Add(Path.GetFileNameWithoutExtension(filePath), fixedFileName);
                }
                int fileIndex = Array.IndexOf(_files, filePath) + 1;
                int fileAmount = _files.Count();
                float percentage = ((float)fileIndex / (float)fileAmount) * 100;
                ColoredConsoleWrite(ConsoleColor.Green, string.Format("\r{0}%       ", percentage), true);
            }
            Space();
            ColoredConsoleWrite(ConsoleColor.Yellow, "Finished Processing Song Names.");
            if (changedFiles.Any())
            {
                ColoredConsoleWrite(ConsoleColor.Magenta, "The following names have been changed:");
                foreach (var s in changedFiles)
                {
                    ColoredConsoleWrite(ConsoleColor.DarkGreen, "Old: " + Path.GetFileNameWithoutExtension(s.Key) + ", ", true);

                    ColoredConsoleWrite(ConsoleColor.Green, "New: " + s.Value);
                }
            }
            else
            {
                ColoredConsoleWrite(ConsoleColor.Green, "No names changes have been made.");
            }
        }

        private static bool ProcessArtwork(string filename, bool showdiag = false)
        {
            if (filename.Contains("-"))
            {
                int fileCount = _files.Count();
                return GoogleImageSearch(Path.GetFileNameWithoutExtension(filename), showdiag);
            }
            return false;
        }

        private static void DumpArtwork()
        {
            var spath = @"D:\Artwork Dump";

            _files = Directory.GetFiles(_path, "*.mp3", SearchOption.TopDirectoryOnly);
            Directory.CreateDirectory(spath);
            foreach (string filePath in _files)
            {
                using (var file = File.Create(filePath))
                {
                    if (!file.Tag.Pictures.Any()) continue;
                    using (var ms = new MemoryStream(file.Tag.Pictures[0].Data.Data))
                    {
                        if (filePath.EndsWith(".ini.mp3")) continue;
                        string savePath = Path.Combine(spath, Path.GetFileNameWithoutExtension(filePath) + ".jpg");
                        var img = Image.FromStream(ms);
                        img.Save(savePath);
                    }
                }
            }
        }

        private static void NotificationIcon_Click(object sender, EventArgs e)
        {
            if (_windowHidden)
            {
                _windowHidden = false;
                ShowWindow(GetConsoleWindow(), SwShow);
            }
            else
            {
                _windowHidden = true;
                ShowWindow(GetConsoleWindow(), SwHide);
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

        private static void mnuExit_Click(object sender, EventArgs e)
        {
            NotificationIcon.Dispose();
            Application.Exit();
            Environment.Exit(0);
        }

        private static string FixFile(string filePath)
        {
            if (filePath.Contains("summer"))
            {
            }
            var file = new TrontorMP3File(filePath);
            string finalName = file.FixFileName();
            string movePath = Path.Combine(_path, finalName + ".mp3");
            if (filePath != movePath && !System.IO.File.Exists(movePath))
                System.IO.File.Move(filePath, movePath);
            return finalName;
        }

        private static void PrintRow(params string[] columns)
        {
            int width = (_tableWidth - columns.Length) / columns.Length;
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
            return string.IsNullOrEmpty(text)
                ? new string(' ', width)
                : text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
        }

        private static void mnuArtists_Click(object sender, EventArgs e)
        {
            ShowWindow(GetConsoleWindow(), 3);

            _files = Directory.GetFiles(_path, "*.mp3", SearchOption.TopDirectoryOnly);
            var list = new List<string>();
            var titles = new List<string>();
            foreach (string filePath in _files)
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
            _tableWidth = longeststring.Length * 7 + 5;
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
                _files = Directory.GetFiles(_path, "*.mp3", SearchOption.TopDirectoryOnly);
                string fixedFile = FixFile(e.FullPath);
                string oldFile = Path.GetFileNameWithoutExtension(e.FullPath);
                if (oldFile != fixedFile)
                {
                    ColoredConsoleWrite(ConsoleColor.Magenta, "The following files has been detected and modified:");
                    ColoredConsoleWrite(ConsoleColor.DarkGreen, "Old: " + oldFile + ", ", true);
                    ColoredConsoleWrite(ConsoleColor.Green, "New: " + fixedFile);
                    Space();
                }
                if (ProcessArtwork(e.FullPath, true))
                {
                    ColoredConsoleWrite(ConsoleColor.Magenta, "The following file artwork has been detected and modified:");
                    ColoredConsoleWrite(ConsoleColor.Green, "File name: " + fixedFile);
                    Space();
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}