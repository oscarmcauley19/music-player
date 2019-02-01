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

            // Begin search process with firstSearch as true
            DisplayChanges(true);
        }

        private void DisplayChanges(bool firstSearch)
        {
            // Get results and set textblocks to these values
            InfoBox.Text = GetInfo(Song.Artist, artistArticleNo, "artist", firstSearch, Song);
            AlbumInfo.Text = GetInfo(Song.Album, albumArticleNo, "album", firstSearch, Song);

            try
            {
                // Attempt lyrics retrieval
                LyricsResult response = GetLyrics(Song);
                string lyrics = response.lyrics;

                // Remove unwanted characters
                Encoding iso8859 = Encoding.GetEncoding("ISO-8859-1");
                lyrics = "\"" + Encoding.UTF8.GetString(iso8859.GetBytes(lyrics)) + "\"";

                // Change ending signpost to make more user friendly 
                lyrics = lyrics.Replace("[...]", "...");

                // Set text to display on page
                LyricsBox.Text = lyrics;
                LyricsSide.Visibility = Visibility.Visible;
            }
            catch
            {
                
            }

            // If at least one of the artist/album searches has yielded results
            if (!(InfoBox.Text == "" && AlbumInfo.Text == ""))
            {
                // Make both boxes visible
                ArtistSide.Visibility = Visibility.Visible;
                AlbumSide.Visibility = Visibility.Visible;

                // Set specific labels explaining failed searches
                if (InfoBox.Text == "")
                {
                    InfoBox.Text = "Sorry, no artist information could be found.";
                }
                if (AlbumInfo.Text == "")
                {
                    AlbumInfo.Text = "Sorry, no album information could be found.";
                }
            }
            else
            {
                // If neither album or artist info could be found
                // display larger message
                NoInfoText.Visibility = Visibility.Visible;
            }
            
        }

        //static string GetReleaseAsString(HttpClient client, int releaseID)
        //{

        //    string url = "releases/" + releaseID.ToString();
        //    HttpResponseMessage response = client.GetAsync(url).Result;

        //    if (response.IsSuccessStatusCode)
        //    {
        //        return response.Content.ReadAsStringAsync().Result;
        //    }

        //    Console.WriteLine("The GET request for releases failed.");
        //    return null;
        //}

        private LyricsResult GetLyrics(SongChoice CurrentSong)
        {
            // Instantiate new LyricWiki objects
            LyricWiki wikiAttempt = new LyricWiki();
            LyricsResult response = new LyricsResult();

            // If song exists in database, set response to the lyrics
            if (wikiAttempt.checkSongExists(CurrentSong.Artist, CurrentSong.Name))
            {
                response = wikiAttempt.getSong(CurrentSong.Artist, CurrentSong.Name);
            }
            return response;
        }

        private string GetInfo(string searchTerm, int articleNo, string searchMode, bool firstSearch, SongChoice chosenSong)
        {
            // Create new encoding to convert results to printable text
            Encoding iso8859 = Encoding.GetEncoding("ISO-8859-1");
            // Search Wikipedia for given term (max 50 results)
            var webClient = new WebClient();
            var unrefinedResult = webClient.DownloadString(@"http://en.wikipedia.org/w/api.php?format=xml&action=opensearch&profile=engine_autoselect&limit=50&search=" + searchTerm);
            // Parse XML result and select Titles and Descriptions
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(unrefinedResult);
            var allTitles = doc.GetElementsByTagName("Text");
            var allDescriptions = doc.GetElementsByTagName("Description");
            XmlNode desiredNode = null;

            if (searchMode == "album")
            {
                int count;
                // If this isn't first search on opening of window:
                if (!firstSearch)
                {
                    // count set to current articleNo-1 as Album search has extra node
                    count = articleNo - 1;
                    desiredNode = doc.GetElementsByTagName("Text")[articleNo - 1];
                }
                else
                {
                    // Start from 0 if first search
                    count = 0;
                }

                // Cycle through search results
                foreach (XmlNode result in allTitles)
                {
                    try
                    {
                        // Needs encoding to fix special characters
                        string currentDesc = Encoding.UTF8.GetString(iso8859.GetBytes
                            (allDescriptions[count].FirstChild.Value.ToString()));

                        // If title or description contain artist name and language 
                        // suggesting it's an album
                        if ((result.InnerText.Contains("album") ||
                            result.InnerText.Contains("EP") ||
                            currentDesc.Contains("album")) &&
                           currentDesc.IndexOf(chosenSong.Artist,
                            StringComparison.OrdinalIgnoreCase) >= 0) // case insensitive for more flexibility
                        {
                            //Desired Wiki has been found
                            desiredNode = result;
                            break;
                        }
                        count++;
                    }
                    catch
                    {

                    }
                }
            }

            // If searching for an artist
            else
            {
                int count;
                if (!firstSearch)
                {
                    // Set desiredNode to current result
                    count = articleNo;
                    desiredNode = doc.GetElementsByTagName("Text")[articleNo];
                }
                else
                {
                    // If this is first search, start from first result
                    count = 0;
                }

                // Cycle through results
                foreach (XmlNode result in allTitles)
                {
                    // Checks if result contains language to suggest 
                    // it is about a musical artist
                    try
                    {
                        if (result.InnerText.Contains("band") ||
                            result.InnerText.Contains("artist") ||
                            result.InnerText.Contains("musician") ||
                            allDescriptions[count].FirstChild.Value.ToString().Contains("band") ||
                            allDescriptions[count].FirstChild.Value.ToString().Contains("artist"))
                        {
                            // Set desiredNode to current result
                            desiredNode = result;
                            break;
                        }
                        count++;
                    }
                    catch
                    {

                    }
                }
            }

            try
            {
                // Now desired result is found, specific search to find the article is made
                string firstResult = Encoding.UTF8.GetString(iso8859.GetBytes(desiredNode.InnerText));

                string descriptionSearch = webClient.DownloadString(
                    "http://en.wikipedia.org/w/api.php?format=xml&action=query&exintro&explaintext" +
                    "&prop=extracts&titles=+" + firstResult + "&redirects=1");

                // Parse XML output to extract paragraph
                XmlDocument doc2 = new XmlDocument();
                return GetParagraph(doc2, descriptionSearch);
            }

            catch
            {
                // No suitable result found
                return "";
            }
        }

        private string GetParagraph(XmlDocument doc, string unrefinedResult)
        {
            // Extract paragraph through iso8859 encoding
            Encoding iso8859 = Encoding.GetEncoding("ISO-8859-1");
            doc.LoadXml(unrefinedResult);
            var desiredNode = doc.GetElementsByTagName("extract")[0];
            string descParagraph = desiredNode.InnerText;
            
            // Regular expression:
            // matches any leftover XML tags
            Regex regex = new Regex("\\<[^\\>]*\\>");
            // Remove unwanted characters, extract pure text
            descParagraph = regex.Replace(descParagraph, string.Empty);
            string result = String.Format(descParagraph);
            return Encoding.UTF8.GetString(iso8859.GetBytes(result));
        }

        private void LyricsLink_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Get lyrics, open url attached to object
            LyricsResult response = GetLyrics(Song);
            Process.Start(response.url);
        }

        private void NextWikiButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Move to next article for artist and display the new changes
            artistArticleNo++;
            GetInfo(Song.Artist, artistArticleNo, "artist", false, Song);
            DisplayChanges(false);
        }

        private void NextWikiButtonAlbum_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Move to next article for album and display the new changes
            albumArticleNo++;
            GetInfo(Song.Artist, albumArticleNo, "album", false, Song);
            DisplayChanges(false);
        }
    }
}
