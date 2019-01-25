using ProjectDesign.com.wikia.lyrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MusicApiCollection.Data;
using MusicApiCollection;
using Lastfm;
using System.Net.Http;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Mono.Web;

namespace ProjectDesign
{
    /// <summary>
    /// Interaction logic for LyricsWindow.xaml
    /// </summary>
    public partial class LyricsWindow : Window
    {
        public SongChoice Song { get; set; }
        public int artistArticleNo = 1;
        public int albumArticleNo = 1;

        public LyricsWindow(SongChoice CurrentSong)
        {
            InitializeComponent();
            this.Song = CurrentSong;
            this.DataContext = this;

            DisplayChanges();
        }

        private void DisplayChanges()
        {
            InfoBox.Text = GetInfo(Song.Artist, artistArticleNo, "artist");
            AlbumInfo.Text = GetInfo(Song.Album, albumArticleNo, "album");
            try
            {
                LyricsResult response = GetLyrics(Song);
                string lyrics = response.lyrics;
                Encoding iso8859 = Encoding.GetEncoding("ISO-8859-1");
                lyrics = "\"" + Encoding.UTF8.GetString(iso8859.GetBytes(lyrics)) + "\"";
                lyrics = lyrics.Replace("[...]", "...");
                LyricsBox.Text = lyrics;

                LyricsSide.Visibility = Visibility.Visible;
                if (InfoBox.Text != "") { ArtistSide.Visibility = Visibility.Visible; }
                if (AlbumInfo.Text!= "") { AlbumSide.Visibility = Visibility.Visible; }
                //AlbumArt.Visibility = Visibility.Visible;
            }
            catch
            {

            }
        }

        static string GetReleaseAsString(HttpClient client, int releaseID)
        {
            string url = "releases/" + releaseID.ToString();
            HttpResponseMessage response = client.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }

            Console.WriteLine("The GET request for releases failed.");
            return null;
        }

        private LyricsResult GetLyrics(SongChoice CurrentSong)
        {
            LyricWiki wikiAttempt = new LyricWiki();
            LyricsResult response = new LyricsResult();

            if (wikiAttempt.checkSongExists(CurrentSong.Artist, CurrentSong.Name))
            {
                response = wikiAttempt.getSong(CurrentSong.Artist, CurrentSong.Name);
            }
            return response;
        }

        private string GetInfo(string searchTerm, int articleNo, string searchMode)
        {
            Encoding iso8859 = Encoding.GetEncoding("ISO-8859-1");
            var webClient = new WebClient();
            var sourceCode = webClient.DownloadString(@"http://en.wikipedia.org/w/api.php?format=xml&action=opensearch&profile=engine_autoselect&limit=50&search=" + searchTerm);
            //var sourceCode = webClient.DownloadString("http://en.wikipedia.org/w/api.php?format=xml&action=query&prop=extracts&titles=+"+ userChoice + "&redirects=1");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(sourceCode);
            //doc.Save("temp.xml");
            //Wikimedia wm = new Wikimedia("temp.xml");
            //WikimediaPage wmp = new WikimediaPage();
            var allResults = doc.GetElementsByTagName("Text");
            XmlNode fnode;

            if (searchMode == "album")
            {
                fnode = doc.GetElementsByTagName("Text")[articleNo - 1];
                foreach (XmlNode result in allResults)
                {
                    if (result.InnerText.Contains("album"))
                    {
                        fnode = result;
                        break;
                    }
                }
            }
            else
            {
                fnode = doc.GetElementsByTagName("Text")[articleNo];
                foreach (XmlNode result in allResults)
                {
                    if (result.InnerText.Contains("band"))
                    {
                        fnode = result;
                        break;
                    }
                }
            }

            // var fnode = doc.GetElementsByTagName("Text")[articleNo];

            try
            {
                string firstResult = Encoding.UTF8.GetString(iso8859.GetBytes(fnode.InnerText));

                string sourceCode2 = webClient.DownloadString("http://en.wikipedia.org/w/api.php?format=xml&action=query&exintro&explaintext&prop=extracts&titles=+" + firstResult + "&redirects=1");
                XmlDocument doc2 = new XmlDocument();
                return GetParagraph(doc2, sourceCode2);
            }

            catch
            {
                return "";
            }
        }

        private string GetParagraph(XmlDocument doc, string sourceCode)
        {
            Encoding iso8859 = Encoding.GetEncoding("ISO-8859-1");
            doc.LoadXml(sourceCode);
            var fnode = doc.GetElementsByTagName("extract")[0];
            string ss = fnode.InnerText;
            //string ss = doc.InnerText;
            Regex regex = new Regex("\\<[^\\>]*\\>");
            string.Format("Before:{0}", ss);
            ss = regex.Replace(ss, string.Empty);
            string result = String.Format(ss);
            return Encoding.UTF8.GetString(iso8859.GetBytes(result));
        }

        private void LyricsLink_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LyricsResult response = GetLyrics(Song);
            Process.Start(response.url);
        }

        private void NextWikiButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            artistArticleNo++;
            GetInfo(Song.Artist, artistArticleNo, "artist");
            DisplayChanges();
        }

        private void NextWikiButtonAlbum_MouseUp(object sender, MouseButtonEventArgs e)
        {
            albumArticleNo++;
            GetInfo(Song.Artist, albumArticleNo, "album");
            DisplayChanges();
        }
    }
}
