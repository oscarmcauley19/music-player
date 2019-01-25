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
        public UploadWindow()
        {
            InitializeComponent();

        }

        private int GetSongCount(MySqlConnection connection)
        {
            //Command counts how many songs are present in specified account
            MySqlCommand command = new MySqlCommand("SELECT COUNT(song_id) FROM songs WHERE user_id=@userID", connection);
            //Insert current user ID into statement
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@userID", User.id);

            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            reader.Read();
            //Converts the count into int allowing for comparison ops
            int output = reader.GetInt32(0);
            connection.Close();

            return output;
        }

        private static void UploadFile(SongChoice song)
        {
            // Set FTP address, username and password from secure app settings
            String ftpurl = @Properties.Settings.Default.FTPAddress + "/user_" + User.id.ToString(); 
            String ftpusername = Properties.Settings.Default.FTPUsername;
            String ftppassword = Properties.Settings.Default.FTPPassword;

            string source = song.File.LocalPath;
            try
            {
                string filename = System.IO.Path.GetFileName(source);
                string ftpfullpath = ftpurl + "/" + filename;
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(ftpfullpath);
                request.Credentials = new NetworkCredential(ftpusername, ftppassword);

                request.KeepAlive = true;
                request.UseBinary = true;
                request.Method = WebRequestMethods.Ftp.UploadFile;

                FileStream fs = System.IO.File.OpenRead(song.File.LocalPath);
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();

                Stream ftpstream = request.GetRequestStream();
                ftpstream.Write(buffer, 0, buffer.Length);
                ftpstream.Close();
            }
            catch (WebException e)
            {
                String status = ((FtpWebResponse)e.Response).StatusDescription;
            }
        }

        private void DBRequest(MySqlConnection connection, SongChoice song)
        {
            MySqlCommand command = new MySqlCommand("INSERT INTO songs (song_name, song_artist, song_album, song_location, user_id)" +
            "VALUES (@name, @artist, @album, @location, @user_id)", connection);

            string path = song.File.LocalPath;
            string fileName = System.IO.Path.GetFileName(song.File.LocalPath);

            command.Parameters.Clear();
            command.Parameters.AddWithValue("@name", song.Name);
            command.Parameters.AddWithValue("@artist", song.Artist);
            command.Parameters.AddWithValue("@album", song.Album);
            command.Parameters.AddWithValue("@location", "user_" + User.id.ToString() + "/" + fileName);
            command.Parameters.AddWithValue("@user_id", User.id);

            connection.Open();
            command.ExecuteReader();
            connection.Close();
        }

        private void LoadSongs(System.Collections.IList rows, MySqlConnection connection)
        {
            UploadBar.Maximum = rows.Count;
            UploadBar.Visibility = Visibility.Visible;
            Task.Run(() =>
            {
                foreach (SongChoice current in rows)
                {
                    try
                    {
                        //MySqlCommand command = new MySqlCommand();
                        UploadFile(current);
                        DBRequest(connection, current);
                        this.Dispatcher.Invoke(() => //Use Dispather to Update UI Immediately  
                        {
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

        private bool CheckIfLoggedIn()
        {
            bool result;

            if(User.id > -1)
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedRows = uploadGrid.SelectedItems;
            // Get connectionString from application settings
            string connectionStringDB = Properties.Settings.Default.DBConnectionString;
            MySqlConnection connection = new MySqlConnection(connectionStringDB);
            int userLimit = 20;

            if (selectedRows.Count > 0)
            {
                if (CheckIfLoggedIn())
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
                            MessageBox.Show(String.Format("This upload surpasses limit of {0} songs.", userLimit));
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Error connecting to database. Upload cancelled.");
                    }
                }
                else
                {
                    MessageBox.Show("You must be logged into an account to perform this function.");
                }
            }
            
        }
    }
}
