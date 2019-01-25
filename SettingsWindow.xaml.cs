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
        public SettingsWindow()
        {
            InitializeComponent();
            DefaultPathBox.Text = Properties.Settings.Default.DefaultPath;

            if(Properties.Settings.Default.RememberUser == true)
            {
                RememberCB.IsChecked = true;
            }
            else
            {
                RememberCB.IsChecked = false;
            }
        }

        bool _saveAccount;
        public bool saveAccount
        {
            get { return _saveAccount; }
            set { _saveAccount = value; }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            saveAccount = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            saveAccount = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DefaultPath = DefaultPathBox.Text;
            if (RememberCB.IsChecked == true && User.username != null)
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

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            dlg.ShowDialog();

            // Get chosen path and output
            DefaultPathBox.Text =  dlg.SelectedPath;
        }
    }
}
