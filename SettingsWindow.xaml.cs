using Ookii.Dialogs;
using System;
using System.Collections.Generic;
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

namespace ProjectDesign
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        // saveAccount is actually private
        // This is shorthand way of making get-set methods
        // in C#
        public bool saveAccount { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();

            // Fill path box with predefined one if already set
            DefaultPathBox.Text = Properties.Settings.Default.DefaultPath;

            // Tick box if user has already chosen to be remembered
            if(Properties.Settings.Default.RememberUser == true)
            {
                RememberCB.IsChecked = true;
            }
            else
            {
                RememberCB.IsChecked = false;
            }
        }

        // saveAccount is true when box is checked
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            saveAccount = true;
        }

        // saveAccount is false when box is unchecked
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            saveAccount = false;
        }

        // When browse button clicked
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browsing dialog
            VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            dlg.ShowDialog();

            // Get chosen path and output
            DefaultPathBox.Text =  dlg.SelectedPath;
        }

        // When save button clicked
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Specified path saved in settings for future sessions
            Properties.Settings.Default.DefaultPath = DefaultPathBox.Text;

            // If user chose to be remembered & is actually logged in
            if (saveAccount && User.username != null)
            {
                //User will be remembered when program opened
                Properties.Settings.Default.RememberUser = true;
                User.remember = true;
            }
            else
            {
                // Won't be remembered
                Properties.Settings.Default.RememberUser = false;
            }
            //Close window
            this.Close();
        }
    }
}
