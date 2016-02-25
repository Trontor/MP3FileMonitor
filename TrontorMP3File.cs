using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HtmlAgilityPack;
using TagLib;
using File = System.IO.File;

namespace MP3_File_Auto_Tagger
{
    public class TrontorMP3File
    {
        public static void Space()
        {
            Console.WriteLine("");
        }

        public static void ColoredConsoleWrite(ConsoleColor color, string text, bool writeOnly = false)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            if (writeOnly)
                Console.Write(text);
            else
                Console.WriteLine(text);
            Console.ForegroundColor = originalColor;
        }

        private static Dictionary<string, string> _dictionary = new Dictionary<string, string>();

        private readonly List<string> _artists = new List<string>();

        public string FileNameNoAttachedArtists = "";

        public bool ReverseSplitHeifen;
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string BaseArtist { get; set; } 
        private string Start
        {
            get { return PartOfFileName(FileName, true); }
        }
        private string End
        {
            get { return PartOfFileName(FileName, false); }
        }

        public TrontorMP3File(string filePath )
        {
            FilePath = filePath;
        }

        public int SplitHeifenKey { get; set; }

        private static void SaveDictionary()
        {
            var xElem = new XElement(
                "items",
                _dictionary.Select(
                    x => new XElement("item", new XAttribute("key", x.Key), new XAttribute("value", x.Value)))
                );
            string xml = xElem.ToString();
            xElem.Save("MP3FileNameConfig.xml");
        }

        private static void LoadDictionary()
        {
            var xElem2 = new XElement("items");
            if (File.Exists("MP3FileNameConfig.xml"))
                xElem2 = XElement.Load("MP3FileNameConfig.xml");
            else
                xElem2.Save("MP3FileNameConfig.xml");
            _dictionary = xElem2.Descendants("item")
                .ToDictionary(x => (string)x.Attribute("key"), x => (string)x.Attribute("value"));
        }

        public string FixFileName()
        {
            LoadDictionary();
            if (FilePath == null) throw new ArgumentNullException("path");
            FileName = Path.GetFileName(FilePath).Replace(".mp3", "");
            if (!FileName.Contains("-"))
            {
                Console.WriteLine("File does not contain a heifen to seperate :(... skipping");
                return FileName;
            }
            FindAndReplace();
            CompleteFeaturingBrackets();
            ExtractArtists();
            AttachFeaturedArtists();
            WriteId3Tags();

            if (SplitHeifenKey > 0 && !_dictionary.ContainsKey(FileName))
                _dictionary.Add(FileName, SplitHeifenKey.ToString());

            SaveDictionary();
            return FileName;
        }

        private static IEnumerable<string> SplitBy(string inputString, char c, int splitIndex)
        {
            var test = new List<string>();
            switch (splitIndex)
            {
                case 0:
                    test.AddRange(inputString.Split(c));
                    break;
                case 1:
                    string str1 = inputString.Split(c)[0];
                    string str2 = inputString.Split(c)[1];
                    string str3 = inputString.Split(c)[2];
                    string end = str2 + c + str3;
                    test.Add(str1);
                    test.Add(end);
                    break;
                case 2:
                    string sts1 = inputString.Split(c)[0];
                    string sts2 = inputString.Split(c)[1];
                    string sts3 = inputString.Split(c)[2];
                    string start = sts1 + c + sts2;
                    test.Add(start);
                    test.Add(sts3);
                    break;
            }
            return test.ToArray();
        }

        private void WriteId3Tags()
        {
            var modified = false;
            if (FileName.Contains("-"))
            {
                using (var file = TagLib.File.Create(FilePath))
                {
                    _artists.Reverse();
                    var split =
                        SplitBy(FileNameNoAttachedArtists.Replace(".mp3", ""), '-',
                            ReverseSplitHeifen ? 0 : SplitHeifenKey).ToArray();
                    string title = split[1].Trim();

                    if (!file.Tag.Performers.ToArray().SequenceEqual(_artists.ToArray()))
                    {
                        file.Tag.Performers = _artists.ToArray<string>();
                        modified = true;
                    }
                    if (file.Tag.Title != title)
                    {
                        file.Tag.Title = title;
                        modified = true;
                    }
                    if (modified)
                        file.Save();
                    file.Dispose();
                }
            }
        }

        private void AttachFeaturedArtists()
        {
            var featuredArtists = new List<string>(_artists.Count);

            _artists.ForEach(item => { featuredArtists.Add(item); });
            featuredArtists.Remove(BaseArtist);
            FileNameNoAttachedArtists = FileName;
            if (featuredArtists.Count > 0)
            {
                ReverseSplitHeifen = true;
                FileName += " (ft. ";
            }
            featuredArtists.Reverse();
            featuredArtists = featuredArtists.OrderBy(x => x != BaseArtist).ToList();
            for (var i = 0; i < featuredArtists.Count; i++)
            {
                featuredArtists[i] = featuredArtists[i].Trim();
            }
            string result = FileName;
            var orderedList = featuredArtists.OrderBy(x => x).ToList();
            foreach (string str in orderedList)
            {
                if (str != BaseArtist)
                    result = result + str +
                             ((orderedList.IndexOf(str) == featuredArtists.Count - 1) || featuredArtists.Count == 1
                                 ? ""
                                 : ", ");
            }
            FileName = result;
            if (featuredArtists.Count > 0)
                FileName = FileName.Trim() + ")"; 
        }

        private static int IndexOfNth(string str, char c, int n)
        {
            int s = -1;
            for (var i = 0; i < n; i++)
            {
                s = str.IndexOf(c, s + 1);

                if (s == -1) break;
            }
            if (s < -1)
                return s;

            while (str[s - 1] == ' ')
            {
                s -= 1;
            }
            return s == -1 ? IndexOfNth(str, c, n - 1) : s;
        }

        private string PartOfFileName(string s, bool first)
        {
            int savedSplitHeifen; 
            if (_dictionary.ContainsKey(FileName) && int.TryParse(_dictionary[FileName], out savedSplitHeifen))
                SplitHeifenKey = savedSplitHeifen;
            int instanceCount = s.Count(f => f == '-');
            if (instanceCount > 1 && SplitHeifenKey == 0)
            {
                var notReplaced = true;
                while (notReplaced)
                {
                    notReplaced = false;
                    ColoredConsoleWrite(ConsoleColor.Cyan, "----------------------------");
                    ColoredConsoleWrite(ConsoleColor.Red, string.Format("The file:{0} has more than one heifen:", FileName));
                    Console.WriteLine("I'm not too sure which heifen you want to use.");
                    Console.WriteLine("Write an integer value corresponding with its occurence count (1 - {0}).",
                        instanceCount);
                    ColoredConsoleWrite(ConsoleColor.Cyan, "----------------------------");
                    Space();
                    ColoredConsoleWrite(ConsoleColor.White, "I want to split heifen number: ", true);

                    int key;
                    if (!int.TryParse(Console.ReadLine(), out key)) continue;
                    SplitHeifenKey = key;
                    if (SplitHeifenKey > instanceCount)
                    {
                        Console.WriteLine("Sorry! That number exceeds the amount of heifens in the filename :^(");
                        notReplaced = true;
                    }
                }
            }
            var filePaths = SplitBy(s.Trim(), '-', SplitHeifenKey);
            string startOfFile = filePaths.ToArray()[Convert.ToInt32(!first)].Trim();
            return startOfFile;
        }

        private void FindAndReplace()
        {
            var replaceList = new Dictionary<string, string>
            {
                {"[", "("},
                {"]", ")"},
                {"OFFICIAL", ""},
                {"Official", ""},
                {"Video)", "|"},
                {"video)", "|"},
                {"FEATURING", "FT"},
                {" featuring ", "ft.  "},
                {" (Featuring ", " (ft. "},
                {",)", ")"},
                {" FEAT ", " (FEAT "},
                {" Feat ", " (FEAT "},
                {" feat. ", " (FEAT "},
                {"(Feat.", "(ft."},
                {"(FEAT", "(feat"},
                {"(feat", "(ft"},
                {"FEAT ", "ft"},
                {"( )", ""},
                {"()", ""},
                {"(|", ""},
                {"( |", ""},
                {"(  )", ""},
                {"FT ", "ft. "},
                {"Ft ", "ft. "},
                {"ft ", "ft. "},
                {" FT. ", " (ft. "},
                {" FT", " (ft"},
                {"(FT ", "(ft. "},
                {" (ft ", " (ft."},
                {" (Ft ", "(ft. "}
            };
            while (true)
            {
                var reiterate = false;
                foreach (var vari in replaceList)
                {
                    if (FileName.ToLower().Contains(vari.Key.ToLower()))
                        reiterate = true;
                }
                if (reiterate)
                    foreach (
                        var replaceItem in
                            replaceList.Where(replaceItem => FileName.ToLower().Contains(replaceItem.Key.ToLower())))
                    {
                        FileName = FileName.Replace(replaceItem.Key.ToLower(), replaceItem.Value);
                        FileName = FileName.Replace(replaceItem.Key, replaceItem.Value);
                    }
                if (reiterate) continue;
                break;
            }
        }

        private void CompleteFeaturingBrackets()
        {
            if (Start.Contains("(ft."))
            {
                int insertIndex;
                int openParenthesesCount = Start.Split('(').Length - 1;
                if (openParenthesesCount > 1)
                    insertIndex = IndexOfNth(Start, '(', 1);
                else insertIndex = Start.Length;
                if (Start[insertIndex - 1] != ')')
                    FileName = Start.Insert(insertIndex, ")") + " - " + End;
            }
            if (End.Contains("(ft."))
            {
                int insertIndex;
                int openParenthesesCount = End.Split('(').Length - 1;
                if (openParenthesesCount > 1)
                    insertIndex = IndexOfNth(End, '(', 2);
                else insertIndex = End.Length;
                if (End[insertIndex - 1] != ')')
                    FileName = Start + " - " + End.Insert(insertIndex, ")");
            }
        }

        private void ExtractArtists()
        {
            string newStart = null;
            string newEnd = null;

            string baseArtist;
            if (Start.Contains("(ft."))
            {
                int ftIndex = Start.IndexOf("(ft.", StringComparison.Ordinal);
                string featuringStart = Start.Substring(ftIndex);
                string featuringFinal = featuringStart.Remove(featuringStart.IndexOf(")", StringComparison.Ordinal));
                baseArtist = Start.Remove(ftIndex).Trim();
                var artists = new List<string>();
                if (featuringFinal.Contains(","))
                {
                    artists = featuringFinal.Replace("(ft.", "").Trim()
                        .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }
                else
                    artists.Add(featuringFinal.Replace("(ft.", "").Trim());
                _artists.AddRange(artists);
                newStart = Start.Replace(featuringStart, "").Trim();
            }
            else
                baseArtist = Start;

            _artists.Add(baseArtist);
            if (End.Contains("(ft."))
            {
                int ftIndex = End.IndexOf("(ft.", StringComparison.Ordinal);
                string featuringEnd = End.Substring(ftIndex);
                string featuringFinal = featuringEnd.Remove(featuringEnd.IndexOf(")", StringComparison.Ordinal));
                var artists = new List<string>();
                if (featuringFinal.Contains(","))
                {
                    artists = featuringFinal.Replace("(ft.", "").Trim()
                        .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }
                else
                    artists.Add(featuringFinal.Replace("(ft.", "").Trim());
                _artists.AddRange(artists);
                newEnd = End.Replace(featuringEnd, "").Trim();
            }

            _artists.Reverse();
            if (newStart == null)
                newStart = Start;
            if (newEnd == null)
                newEnd = End;
            BaseArtist = baseArtist;
            FileName = newStart + " - " + newEnd;
        }
    }
}