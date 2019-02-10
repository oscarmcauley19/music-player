using MySql.Data.MySqlClient;
using Renci.SshNet;
using SynologyClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

namespace ProjectDesign
{
    /// <summary>
    /// Interaction logic for UploadWindow.xaml
    /// </summary>
    public partial class UploadWindow : Window
    {
        const int userLimit = 400;

        public UploadWindow()
        {
            InitializeComponent();

        }

        private int GetSongCount(MySqlConnection connection)
        {
            // Command counts how many songs are present in specified account
            MySqlCommand command = new MySqlCommand("SELECT COUNT(song_id) FROM songs WHERE user_id=@userID", connection);

            // Insert current user ID into statement
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@userID", User.id);

            // Carry out query
            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            reader.Read();

            // Convert the count into int allowing for comparison ops
            int output = reader.GetInt32(0);
            connection.Close();

            return output;
        }

        private static void UploadFile(SongChoice song, string ftpurl, string ftpusername, string ftppassword)
        {
            // Get path of song to be uploaded
            string source = song.File.LocalPath;
            try
            {
                // Gets filename from full path & puts into FTP path
                string filename = System.IO.Path.GetFileName(source);
                string ftpfullpath = ftpurl + "/" + filename;

                // Make request to place file in given location
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(ftpfullpath);
                request.Credentials = new NetworkCredential(ftpusername, ftppassword);

                // Set criteria of file transfer
                request.KeepAlive = true;
                request.UseBinary = true;
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // Read mp3 file
                FileStream fs = System.IO.File.OpenRead(song.File.LocalPath);
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();

                // Send this filestream to server via FTP
                Stream ftpstream = request.GetRequestStream();
                ftpstream.Write(buffer, 0, buffer.Length);
                ftpstream.Close();
            }
            catch (WebException e)
            {
                // If error occurs, get response from FTP server
                String status = ((FtpWebResponse)e.Response).StatusDescription;
            }
        }

        private void DBRequest(MySqlConnection connection, SongChoice song)
        {
            // SQL query defined
            MySqlCommand command = new MySqlCommand("INSERT INTO songs (song_name, song_artist, song_album, song_location, user_id)" +
            "VALUES (@name, @artist, @album, @location, @user_id)", connection);

            // Extracts filename from song to put as server file location
            string path = song.File.LocalPath;
            string fileName = System.IO.Path.GetFileName(song.File.LocalPath);

            // Arguments securely given for parameters
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@name", song.Name);
            command.Parameters.AddWithValue("@artist", song.Artist);
            command.Parameters.AddWithValue("@album", song.Album);
            command.Parameters.AddWithValue("@location", "user_" + User.id.ToString() + "/" + fileName);
            command.Parameters.AddWithValue("@user_id", User.id);

            // Execute query
            connection.Open();
            command.ExecuteReader();
            connection.Close();
        }

        private void LoadSongs(System.Collections.IList rows, MySqlConnection connection)
        {
            // Setup upload bar to represent num of songs
            UploadBar.Maximum = rows.Count;
            UploadBar.Visibility = Visibility.Visible;

            // Task runs on separate thread
            // so rest of app will still work simultaneously
            Task.Run(() =>
            {
                foreach (SongChoice current in rows)
                {
                    try
                    {
                        // Upload given song
                        UploadFile(current,
                            @Properties.Settings.Default.FTPAddress + "/user_" + User.id.ToString(),
                            Properties.Settings.Default.FTPUsername,
                            Properties.Settings.Default.FTPPassword);

                        // Update database
                        DBRequest(connection, current);

                        this.Dispatcher.Invoke(() =>
                        {
                            // Upload bar is incremented as song uploaded
                            UploadBar.Value++;
                        });
                    }
                    catch
                    {
                        MessageBox.Show("Error occurred when uploading songs.");
                    }
                }
                System.Windows.MessageBox.Show("Process Complete");
            });
        }

        private bool CheckIfLoggedIn(int id)
        {
            // If ID is valid then logged in
            if(id > -1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedRows = uploadGrid.SelectedItems;

            // Get connectionString from application settings
            string connectionStringDB = Properties.Settings.Default.DBConnectionString;
            MySqlConnection connection = new MySqlConnection(connectionStringDB);

            if (selectedRows.Count > 0)
            {
                // Ensure no upload attempted if not actually logged in
                if (CheckIfLoggedIn(User.id))
                {
                    try
                    {
                        //Check if this addition will surpass limit of songs
                        if (selectedRows.Count + GetSongCount(connection) < userLimit)
                        {
                            //Begin upload process
                            LoadSongs(selectedRows, connection);
                        }
                        else
                        {
                            // Info displayed to user
                            MessageBox.Show(String.Format("This upload surpasses limit of {0} songs.", userLimit));
                        }
                    }
                    catch
                    {
                        // Error message if unsuccessful
                        MessageBox.Show("Error connecting to database. Upload cancelled.");
                    }
                }
                else
                {
                    // Info displayed to user
                    MessageBox.Show("You must be logged into an account to perform this function.");
                }
            }
            
        }
    }
}
