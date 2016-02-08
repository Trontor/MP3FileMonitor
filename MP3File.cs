using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using File = TagLib.File;

namespace MP3_File_Auto_Tagger
{
    internal class Mp3File
    {
        public string FixFileName(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            FilePath = path;
            FileName = Path.GetFileName(path).Replace(".mp3", "");
            FindAndReplace();
            CompleteFeaturingBrackets();
            ExtractArtists();
            AttachFeaturedArtists();
            WriteId3Tags();
            return FileName;
        }
        private List<string> _artists = new List<string>();

        public bool ReverseSplitHeifen = false;
        public string FilePath { get; set; }
        public string FileNameNoAttachedArtists = "";
        public string FileName { get; set; }
        public string BaseArtist { get; set; }

        public string Start
        {
            get { return PartOfFileName(FileName, true); }
        }

        public string End
        {
            get { return PartOfFileName(FileName, false); }
        }

        public int SplitHeifenKey { get; set; }

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
            if (FileName.Contains("-"))
            {
                using (var file = File.Create(FilePath))
                {
                    var split = SplitBy(FileNameNoAttachedArtists.Replace(".mp3", ""), '-', ReverseSplitHeifen ? 0 : SplitHeifenKey).ToArray();
                    string title = split[1].Trim();
                    file.Tag.Performers = null;
                    file.Tag.Performers = _artists.ToArray<string>();
                    file.Tag.Title = title;
                    file.Save();
                    file.Dispose();
                }
            }
        }

        private void AttachFeaturedArtists()
        {
            FileNameNoAttachedArtists = FileName;
            if (_artists.Count > 1)
            {
                ReverseSplitHeifen = true;
                FileName += " (ft. ";
            }
            _artists.Reverse();
            _artists = _artists.OrderBy(x => x != BaseArtist).ToList();
            for (var i = 0; i < _artists.Count; i++)
            {
                _artists[i] = _artists[i].Trim();
            }
            string result = FileName;
            var orderedList = _artists.OrderBy(x => x).ToList();
            foreach (string str in orderedList)
            {
                if (str != BaseArtist)
                    result = result + str +
                             ((orderedList.IndexOf(str) == _artists.Count - 1) || _artists.Count == 2 ? "" : ", ");
            }
            FileName = result;
            if (_artists.Count > 1)
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
            int instanceCount = s.Count(f => f == '-');
            if (instanceCount > 1 && SplitHeifenKey == 0)
            {
                var notReplaced = true;
                while (notReplaced)
                {
                    notReplaced = false;
                    Console.WriteLine("----------------------------");
                    Console.WriteLine("The file:{0} has more than one heifen:", FileName);
                    Console.WriteLine("Which heifen do you want to use?");
                    Console.WriteLine("Write an integer value corresponding with its occurence count (1 - {0}).",
                        instanceCount);
                    Console.WriteLine("----------------------------");
                    Console.WriteLine("I want to split heifen number: ");
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
                {"OFFICIAL", ""},
                {"FEATURING", "FT"},
                {" featuring ", "ft.  "},
                {" (Featuring ", " (ft. "},
                {" FEAT ", " (FEAT "},
                {" Feat ", " (FEAT "},
                {"(FEAT", "(feat"},
                {"(feat", "(ft"},
                {"FEAT ", "ft"},
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