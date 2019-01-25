using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
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
using Ookii;
using Ookii.Dialogs;
using System.Net;
using System.IO;
using TagLib;
using System.Collections;
using System.Threading;

namespace ProjectDesign
{
    /// <summary>
    /// Interaction logic for DownloadPage.xaml
    /// </summary>
    /// 
    public partial class DownloadPage : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name) //Called when property changes to trigger event
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public DownloadPage()
        {
            InitializeComponent();

            //Set data context for controls
            this.DataContext = this;

            //Set values as arguments to following methods
            List<SongChoice> songsFromAccount = new List<SongChoice>();
            DataTable dataTable = new DataTable();
            string connectionStringDB = Properties.Settings.Default.DBConnectionString;

            //Sets datatable to output of database query
            dataTable = loadFromDB(connectionStringDB, dataTable);
            //Outputs songs from database to DataGrid
            downloadGrid.ItemsSource = dataTable.DefaultView;
        }

        private DataTable loadFromDB(string connectionString, DataTable dt)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    //Creates command from query and given connection
                    MySqlCommand getInfoCmd = new MySqlCommand("SELECT song_id, song_name, song_artist, song_album, song_location " +
                        "FROM songs INNER JOIN users ON songs.user_id = users.user_id WHERE songs.user_id = @idInput;", connection);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(getInfoCmd);

                    //Inserts user ID from app into DB query
                    getInfoCmd.Parameters.Clear();
                    getInfoCmd.Parameters.AddWithValue("@idInput", User.id);

                    //Fills datatable with output - each column is a field in DB
                    connection.Open();
                    adapter.Fill(dt);
                    connection.Close();
                }
            }
            catch
            {
                //Output error message
                MessageBox.Show("Error connecting to server. Check connection.");
            }

            return dt;
        }

        private static void DownloadFile(string serverFilePath, string newFilePath)
        {
            // Set FTP address, username and password from secure app settings
            String ftpurl = @Properties.Settings.Default.FTPAddress + "/user_" + User.id.ToString();
            String ftpusername = Properties.Settings.Default.FTPUsername;
            String ftppassword = Properties.Settings.Default.FTPPassword;

            try
            {
                // Get actual file name from whole path
                string filename = System.IO.Path.GetFileName(serverFilePath);
                // Add filename to original path and destination path
                string ftpfullpath = ftpurl + "/" + filename;
                newFilePath = newFilePath + "/" + filename;
                
                // Create FTP download request
                FtpWebRequest ftp = (FtpWebRequest)FtpWebRequest.Create(ftpfullpath);
                ftp.Credentials = new NetworkCredential(ftpusername, ftppassword);
                ftp.Method = WebRequestMethods.Ftp.DownloadFile;

                // Get response from server
                FtpWebResponse response = (FtpWebResponse)ftp.GetResponse();
                Stream responseStream = response.GetResponseStream();

                // Create new file in specified location
                FileStream file = System.IO.File.Create(newFilePath);
                byte[] buffer = new byte[32 * 1024];
                int read;

                // Write data to new file
                while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    file.Write(buffer, 0, read);
                }

                // Close all connections
                response.Close();
                responseStream.Close();
                file.Close();
            }
            catch(Exception ex)
            {
                //Output error message
                //MessageBox.Show("Error transferring files. Check that file location specified is valid.");
                MessageBox.Show(ex.ToString());
            }
        }

        private List<SongChoice> ProcessFiles(List<SongChoice> inputList, DataTable dt)
        {
            int numOfColumns = dt.Columns.Count;
            string filename;
            DownloadBar.Maximum = dt.Columns.Count;

            // Run for every row in datatable
            foreach (DataRow dr in dt.Rows)
            {
                for (int i = 0; i < numOfColumns; i++)
                {
                    // Create file from filename in element i in row
                    filename = @Properties.Settings.Default.FTPAddress + "/" + dr[i].ToString();
                    Uri fileUri = new Uri(@filename);

                    // Use TagLib extension to gain control of metadata values
                    TagLib.File file = TagLib.File.Create(@filename);

                    // Round the value for duration and change format to minutes & seconds
                    TimeSpan duration = file.Properties.Duration;
                    int timespanSize = 7; //timespan is always this length
                    int factor = (int)Math.Pow(10, (timespanSize));
                    string roundedDuration = new TimeSpan(((long)Math.Round((1.0 * duration.Ticks / factor)) * factor)).ToString(@"mm\:ss");

                    // Create new SongChoice object and assigns values gained to attributes of object
                    SongChoice song = new SongChoice();
                    song.File = fileUri;
                    song.Name = file.Tag.Title;
                    song.Album = file.Tag.Album;
                    song.Length = roundedDuration.ToString();
                    song.Artist = file.Tag.FirstAlbumArtist;

                    // Adds new song to input list
                    inputList = song.AddSongs(inputList);
                    DownloadBar.Value++;
                }
            }

            return inputList;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Sets chosen file path from GetPath method and outputs to textbox
            PathBox.Text = GetPath();
        }

        private void MoveFiles(string newPath)
        {
            // Get rows that have been selected by user
            var selectedSongs = downloadGrid.SelectedItems;
            DownloadBar.Maximum = downloadGrid.SelectedItems.Count;

            Task.Run(() =>
            {
                try
                {
                    // Run for every song selected
                    foreach (DataRowView song in selectedSongs)
                    {
                        // Get path from 5th element in datagrid where file location held
                        string path = @Properties.Settings.Default.FTPAddress + "/" + song[4].ToString();

                        // Checks if file doesn#t already exist
                        if (System.IO.File.Exists(newPath) == false)
                        {
                            DownloadFile(path, newPath);
                        }
                        else
                        {
                            // Output specific error message
                            string msg = string.Format("Song {0} could not be transferred.\n It may already exist in this location.", song[1]);
                            MessageBox.Show(msg);
                        }

                        this.Dispatcher.Invoke(() => //Use Dispather to Update UI Immediately  
                        {
                            DownloadBar.Value++;
                        });

                    }
                }
                catch
                {
                    // Output error message
                    MessageBox.Show("Error occurred. Check the directory you entered is correct.");
                }

                MessageBox.Show("Download Complete");
            });
        }

        private string GetPath()
        {
            // Opens file dialog from Ookii library
            VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            dlg.ShowDialog();

            // Get chosen path and output
            string path = dlg.SelectedPath;
            return path;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            //Check if specified location is valid
            if(downloadGrid.SelectedItems.Count > 0)
            {
                if (Directory.Exists(PathBox.Text))
                {
                    DownloadBar.Visibility = Visibility.Visible;
                    // Begins download process based on path in textbox
                    MoveFiles(PathBox.Text);
                }
                else
                {
                    MessageBox.Show("Directory not found.");
                }
            }
        }
    }
}
