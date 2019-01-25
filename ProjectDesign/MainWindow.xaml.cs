using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void filesClicked(object sender, MouseButtonEventArgs e) //click event when choose files is selected
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog selection = new Microsoft.Win32.OpenFileDialog();

            //Enable multiple selection
            selection.Multiselect = true;

            // Set filter for file extension to mp3s only
            selection.DefaultExt = ".mp3";
            selection.Filter = "MP3 Files (*.mp3)|*.mp3";

            //Boolean value of whether a file is found or not
            Nullable<bool> result = selection.ShowDialog();
        
            if (result == true) //if a file is found
            {

                List<SongChoice> songsSelected = new List<SongChoice>(); //creates new list of SongChoice objects

                CollectionViewSource itemCollectionViewSource;
                itemCollectionViewSource = (CollectionViewSource)(FindResource("ItemCollectionViewSource"));
                itemCollectionViewSource.Source = songsSelected;
                //this initialises the data binding environment between the DataGrid and songsSelected list
                //through use of a item collection, which allows for sorting & filtering

                foreach (String filename in selection.FileNames) //for each file in the group of files selected
                {
                    //FileStream FS = new FileStream(filename, FileMode.Open, FileAccess.Read);

                    //Use TagLib extension to gain control of metadata values
                    TagLib.File file = TagLib.File.Create(filename);

                    //create new SongChoice object and assigns values gained to attributes of object
                    SongChoice song = new SongChoice();
                    song.Name = file.Tag.Title;
                    song.Album = file.Tag.Album;
                    song.Length = file.Properties.Duration.ToString();
                    song.Artist = file.Tag.FirstAlbumArtist;

                    //Sets list to the output of AddSongs method, which is the current list with new song added
                    songsSelected = song.AddSongs(songsSelected);
                    
                    //songsSelected.Add(new SongChoice { Name = title, Artist = artist, Album = album, Length = length });
                }
            }
        }
    }
}
