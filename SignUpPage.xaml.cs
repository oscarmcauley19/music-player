using MySql.Data.MySqlClient;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectDesign
{
    /// <summary>
    /// Interaction logic for SignUpPage.xaml
    /// </summary>
    public partial class SignUpPage : Page
    {
        public SignUpPage()
        {
            InitializeComponent();
        }

        private bool DetailsToDB()
        {
            // Get connection details from app settings
            string connectionString = Properties.Settings.Default.DBConnectionString;
            try
            {
                // Parse details
                string username = UsernameInput.Text;
                string hashedPassword = PasswordEncrypter.Hash(PasswordInput.Password);

                // Establish connection from details held in connection string
                MySqlConnection connection = new MySqlConnection(connectionString);
                
                // Create command with values from parameters
                MySqlCommand add = new MySqlCommand("INSERT INTO users (user_username, user_password)" +
                    "VALUES (@Username, @HashedPass);", connection);
                add.Parameters.Clear();
                add.Parameters.AddWithValue("@Username", username);
                add.Parameters.AddWithValue("@HashedPass", hashedPassword);

                // Create second command to get User ID
                string getIDString = "SELECT user_id FROM  users WHERE user_username=@Username";
                MySqlCommand getID = new MySqlCommand(getIDString, connection);
                getID.Parameters.Clear();
                getID.Parameters.AddWithValue("@Username", username);

                // Run the upload command
                connection.Open();
                add.ExecuteReader();
                connection.Close();

                // Get User ID using second command
                connection.Open();
                MySqlDataReader reader = getID.ExecuteReader();
                reader.Read();
                string userID = reader[0].ToString();
                connection.Close();

                // FTP request to create folder for user in server
                CreateFolder(Properties.Settings.Default.FTPAddress, userID,
                    Properties.Settings.Default.FTPUsername, Properties.Settings.Default.FTPPassword);

                return true;
            }
            // Catch specific error code
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                if (ex.Code == 1062) //Error code for duplicate entry
                {
                    MessageBox.Show("This account already exists.");
                }
                else // General error code if other issue
                {
                    MessageBox.Show("Error creating account.");
                }
                return false;
            }
        }

        private void CreateFolder(string ftpPath, string ID, string username, string pword)
        {
            // FTP request to upload file to user location in server
            string folderPath = Properties.Settings.Default.FTPAddress + "/user_" + ID;
            WebRequest request = WebRequest.Create(folderPath);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(username, pword);

            // Output confirmation (testing)
            //using (var resp = (FtpWebResponse)request.GetResponse())
            //{
            //    MessageBox.Show(resp.StatusCode.ToString());
            //}
        }

        // When 'go to login page' button clicked
        private void LogIn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Switch to login page
            LoginPage newPage = new LoginPage();
            Window currentWin = (Window)this.Parent;
            currentWin.Content = newPage;
        }

        // When submit button is clicked
        private void SubmitButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            // Check length criteria for username
            if (UsernameInput.Text.Length < 13 && UsernameInput.Text.Length > 2)
            {
                // Same for password
                if (PasswordInput.Password.Length < 16 && PasswordInput.Password.Length > 7)
                {
                    // Check pword contains number
                    bool containsNum = PasswordInput.Password.Any(c => char.IsDigit(c));
                    if (containsNum)
                    {
                        // Finally check password repeat is actually correct
                        if (PasswordInput.Password == PasswordCheck.Password)
                        {
                            // If upload of details has been successful
                            if (DetailsToDB())
                            {
                                // Switch to sign up confirmation page
                                SignUpConfirmationPage confPage = new SignUpConfirmationPage();
                                Window currentWin = (Window)this.Parent;
                                currentWin.Content = confPage;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Passwords entered do not match up.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Password does not contain a number.");
                    }
                }
                else
                {
                    MessageBox.Show("Password is of incorrect length");
                }
            }
            else
            {
                MessageBox.Show("Username is of incorrect length");
            }
        }
    }
}
