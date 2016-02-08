using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using File = TagLib.File;

namespace MP3_File_Auto_Tagger
{
    //public static class StringExtensions
    //{
    //    public static int Separate(this string str, char value, int nth = 1)
    //    {
    //        if (nth <= 0)
    //            throw new ArgumentException("Can not find the zeroth index of substring in string. Must start with 1");
    //        int offset = str.IndexOf(value.ToString(), StringComparison.Ordinal);
    //        for (int i = 1; i < nth; i++)
    //        {
    //            if (offset == -1) return -1;
    //            offset = str.IndexOf(value.ToString(), offset + 1, StringComparison.Ordinal);
    //        }
    //        return offset;
    //    }

    //    public static string StartOfFile(this string s)
    //    {
    //        var filePaths = s?.Trim().Split(new[] { "-" }, StringSplitOptions.None);
    //        string startOfFile = filePaths?[0].Trim();
    //        return startOfFile;
    //    }

    //    public static string EndOfFile(this string s)
    //    {
    //        //Returns: string[0] = Artist FEAT testIfy  string[1] = Title ft test
    //        var filePaths = s?.Trim().Split(new[] { "-" }, StringSplitOptions.None);
    //        string endOfFile = filePaths?[1].Trim();
    //        return endOfFile;
    //    }
    //}

    internal class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private static void Main(string[] args)
        {
            var path = @"D:\Music";
            var monitor = new FileSystemWatcher(path, "*.mp3") { EnableRaisingEvents = true };
            monitor.Created += monitor_CreatedOrChanged;
            monitor.Renamed += monitor_CreatedOrChanged;
            var handle = GetConsoleWindow();
            //  ShowWindow(handle, SW_HIDE);

            var files = Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly);
            foreach (string filePath in files)
            {
                if (!filePath.Contains(".ini"))
                {
                    Mp3File file = new Mp3File();
                    string finalName = file.FixFileName(filePath);
                    Console.WriteLine(finalName);
                }
            }
            Console.ReadKey();
        }

        ///// <summary>
        /////     Make sure you are passing a PATH not a FILENAME
        ///// </summary>
        ///// <param name="path"></param>
        ///// <returns></returns>
        ///// Path Format : D:\Music\Artist FEAT testIfy - Title ft test.mp3
        //private static string FixFilename(string path)
        //{
        //    var replaceList = new Dictionary<string, string>
        //    {
        //        {"OFFICIAL", ""},
        //        {"FEATURING", "FT"},
        //        {"FEAT", "ft"},
        //        {"FT ", "ft. "},
        //        {"Ft ", "ft. "},
        //        {" FEAT", " (FEAT"},
        //        {"(FEAT", "(feat"},
        //        {" FT. ", " (ft. "},
        //        {" FT", "(ft"},
        //        {"(FT ", "(ft. "}
        //    };

        //    //Returns Artist FEAT testIfy - Title ft test.mp3
        //    string fileName = Path.GetFileName(path);

        //    //Returns Artist FEAT testIfy - Title ft test 
        //    string fileNameWithoutExtension = fileName?.Replace(".mp3", "");
        //    string newFileName = fileNameWithoutExtension;
        //    if (newFileName == null) throw new ArgumentNullException(nameof(newFileName));


        //    foreach (var replaceItem in replaceList)
        //    {
        //        newFileName = newFileName.Replace(replaceItem.Key.ToLower(), replaceItem.Value);
        //        newFileName = newFileName.Replace(replaceItem.Key, replaceItem.Value);
        //    }

        //    if (newFileName.StartOfFile().Contains("(ft.") &&
        //        newFileName.StartOfFile()[newFileName.StartOfFile().Split('(').Count() > 1 ? newFileName.StartOfFile().Separate('(', 2) : newFileName.EndOfFile().Length - 1] != ')')
        //        newFileName = newFileName.StartOfFile().Insert(newFileName.StartOfFile().Separate('(', 2), ")") + " - " +
        //                      newFileName.EndOfFile();

        //    if (newFileName.EndOfFile().Contains("(ft.") &&
        //        newFileName.EndOfFile()[newFileName.EndOfFile().Split('(').Count() > 1 ? newFileName.EndOfFile().Separate('(', 2) : newFileName.EndOfFile().Length - 1] != ')')
        //        newFileName = newFileName.StartOfFile() + " - " +
        //                      newFileName.EndOfFile().Insert(newFileName.EndOfFile().Separate('(', 2), ")");

        //    string start = null;
        //    string end = null;
        //    string baseArtist;
        //    var artists = new List<string>();
        //    if (newFileName.StartOfFile().Contains("(ft."))
        //    {
        //        int ftIndex = newFileName.StartOfFile().IndexOf("(ft.", StringComparison.Ordinal);
        //        string subString = newFileName.StartOfFile().Substring(ftIndex);
        //        string subStringEnd = subString.Remove(subString.IndexOf(")", StringComparison.Ordinal));
        //        baseArtist = newFileName.StartOfFile().Remove(ftIndex).Trim();
        //        artists.AddRange(subStringEnd.Replace("(ft.", "").Split(new[] { "," }, StringSplitOptions.None).ToList());
        //        start = newFileName.StartOfFile().Replace(subString, "").Trim();
        //    }
        //    else
        //        baseArtist = newFileName.StartOfFile();

        //    artists.Add(baseArtist);
        //    if (newFileName.EndOfFile().Contains("(ft."))
        //    {
        //        int ftIndex = newFileName.EndOfFile().IndexOf("(ft", StringComparison.Ordinal);
        //        string subString = newFileName.EndOfFile().Substring(ftIndex);
        //        string subStringEnd = subString.Remove(subString.IndexOf(")", StringComparison.Ordinal));

        //        artists.AddRange(subStringEnd.Replace("(ft.", "").Split(new[] { "," }, StringSplitOptions.None).ToList());

        //        end = newFileName.EndOfFile().Replace(subString, "").Trim();
        //    }
        //    artists.Reverse();

        //    if (start == null)
        //        start = newFileName.StartOfFile();
        //    if (end == null)
        //        end = newFileName.EndOfFile();

        //    string finalFileName = start + " - " + end;

        //    if (artists.Count > 1)
        //        finalFileName += " (ft. ";
        //    artists.Reverse();
        //    artists = artists.OrderBy(x => x != baseArtist).ToList();
        //    for (var i = 0; i < artists.Count; i++)
        //    {
        //        artists[i] = artists[i].Trim();
        //    }
        //    finalFileName = artists.Where(str => str != baseArtist)
        //        .Aggregate(finalFileName,
        //            (current, str) =>
        //                current + str +
        //                ((artists.IndexOf(str) == artists.Count - 1) || artists.Count == 2 ? "" : ", "));
        //    if (artists.Count > 1)
        //        finalFileName = finalFileName.Trim() + ")";

        //    var file = File.Create(path);
        //    if (newFileName.Contains("-"))
        //    {
        //        var split = finalFileName.Replace(".mp3", "").Split('-');
        //        string title = split[1].Trim();
        //        string artist = split[0].Trim();
        //        file.Tag.Performers = null;
        //        file.Tag.Performers = artists.ToArray<string>();
        //        file.Tag.Title = title;
        //        file.Save();
        //    }
        //    file.Dispose();

        //    return finalFileName;
        //}

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