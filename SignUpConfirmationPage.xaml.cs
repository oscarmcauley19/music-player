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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectDesign
{
    /// <summary>
    /// Interaction logic for SignUpConfirmationPage.xaml
    /// </summary>
    public partial class SignUpConfirmationPage : Page
    {
        public SignUpConfirmationPage()
        {
            InitializeComponent();
        }

        private void Return_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoginPage newPage = new LoginPage();
            Window currentWin = (Window)this.Parent;
            currentWin.Content = newPage;
        }
    }
}
