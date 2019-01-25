using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectDesign
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {

        public LoginPage()
        {
            InitializeComponent();
        }

        public static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            //Method returns boolean result of whether window is open
            return string.IsNullOrEmpty(name)
               ? Application.Current.Windows.OfType<T>().Any()
               : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }

        int LoginCheck(string username, string password, string connectionString)
        {
            // set default value so can later test if new value found
            int userID = -999;

            //Establish connection from param connectionString
            MySqlConnection connection = new MySqlConnection(connectionString);
            string command = "SELECT user_password, user_id FROM users WHERE user_username=@Username";
            MySqlCommand getValues = new MySqlCommand(command, connection);
            //Insert value for parameter Username
            getValues.Parameters.Clear();
            getValues.Parameters.AddWithValue("@Username", username);

            connection.Open();
            //Establish reader where resultant values are held
            MySqlDataReader reader = getValues.ExecuteReader();
            reader.Read();

            //Compare input password to the de-hashed one from DB
            bool result = PasswordEncrypter.Verify(password, reader.GetString(0));
            if (result == true)
            {
                //Set userID to int value
                userID = reader.GetInt32(1);
            }

            connection.Close();

            return userID;
        }

        private void SignUpClicked(object sender, MouseButtonEventArgs e)
        {
            //Refreshes content of page to display sign up screen
            SignUpPage newPage = new SignUpPage();
            Window currentWin = (Window)this.Parent;
            currentWin.Content = newPage;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            
            string connectionString = Properties.Settings.Default.DBConnectionString;
            try
            {
                string username = UsernameInput.Text;
                string password = PasswordInput.Password;

                int id = LoginCheck(username, password, connectionString);

                if (id != -999)
                {
                    User.id = id;
                    User.username = username;

                    Properties.Settings.Default.UserName = User.username;
                    Properties.Settings.Default.UserID = User.id;
                    Properties.Settings.Default.RememberUser = false;

                    if (IsWindowOpen<Window>("MainWindow"))
                    {
                        MainWindow openMain = new MainWindow();
                        openMain.Show();
                    }
                    Window currentWin = (Window)this.Parent;
                    currentWin.Close();
                }
                else
                {
                    MessageBox.Show("Error logging in. User not found.");
                }
        }
            catch
            {
                MessageBox.Show("Account not found.");
            }
}
    }
}
