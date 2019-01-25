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
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Threading;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using MySql.Data.MySqlClient;
using ProjectDesign.ViewModels;
using ProjectDesign.Views;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;
using Genius;
using ProjectDesign.com.wikia.lyrics;

namespace ProjectDesign
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            // Send notice to visual elements that value has changed
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        }

        //Initialise lists
        SongList unshuffledQueue = new SongList();
        SongList Queue = new SongList();
        SongList shuffledQueue = new SongList();
        SongList totalSongSelection = new SongList();
        bool sliderUpdating = true;

        SongChoice _CurrentSong = new SongChoice();
        public SongChoice CurrentSong
        {
            get { return _CurrentSong; }
            set
            {
                _CurrentSong = value;
                this.OnPropertyChanged("CurrentSong");
            }
        }

        //List<SongChoice> _totalSongSelection = new List<SongChoice>();
        //public List<SongChoice> totalSongSelection
        //{
        //    get { return _totalSongSelection; }
        //    set
        //    {
        //        _totalSongSelection = value;
        //        //this.OnPropertyChanged("totalSongSelection");
        //    }
        //}

        public MainWindow()
        {
            InitializeComponent();
            LoadDefaultSongs();

            this.DataContext = this; //sets data context to this region

            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;

            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }

            if (Properties.Settings.Default.RememberUser)
            {
                User.Login(Properties.Settings.Default.UserID, Properties.Settings.Default.UserName);
            }
            else
            {
                Properties.Settings.Default.UserID = -1;
                Properties.Settings.Default.UserName = User.username;
                User.Logout();
            }
        }

        private void AttemptUserLogin()
        {
            User.id = Convert.ToInt32(Properties.Settings.Default.UserID);
            User.username = Properties.Settings.Default.UserName;
        }

        private void ChooseFilesButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            OpenFileDialog selection = new OpenFileDialog()
            {
                // Enable multiple selection
                Multiselect = true,
                // Set filter for file extension to mp3s only
                DefaultExt = ".mp3",
                Filter = "MP3 Files (*.mp3)|*.mp3"
            };

            // Boolean value of whether a file is found or not
            Nullable<bool> result = selection.ShowDialog();

            LoadIntoGrid();

            if (result == true) //if a file is found
            {
                foreach (String filename in selection.FileNames) //for each file in the group of files selected
                {
                    try
                    {
                        totalSongSelection.Add(GenerateObjects(filename));
                    }
                    catch
                    {
                        MessageBox.Show("Files could not be imported.");
                    }
                }
                InitialiseQueues();
            }

        }

        private void InitialiseQueues()
        {
            ShuffleButton.Background = Brushes.Transparent;
            ShuffleButton.Foreground = Brushes.White;
            songGrid.Items.Refresh();

            SongList tempList = new SongList();
            tempList.AddRange(totalSongSelection);

            unshuffledQueue.ClearAndReplace(totalSongSelection);
            shuffledQueue.ClearAndReplace(tempList);
            shuffledQueue.Shuffle();

            if (ShuffleButton.IsChecked == true)
            {
                Queue.ClearAndReplace(shuffledQueue);
            }
            else
            {
                Queue.ClearAndReplace(unshuffledQueue);
            }
        }

        private void LoadIntoGrid()
        {
            CollectionViewSource itemCollectionViewSource;
            itemCollectionViewSource = (CollectionViewSource)(FindResource("ItemCollectionViewSource"));
            itemCollectionViewSource.Source = totalSongSelection.GetList();
            songGrid.ScrollIntoView(CollectionView.NewItemPlaceholder);
            //this initialises the data binding environment between the DataGrid and totalSongSelection list
            //through use of a item collection, which allows for sorting & filtering
        }

        private SongChoice GenerateObjects(string filepath)
        {

            //create URI object - will allow for songs to actually be played
            Uri fileUri = new Uri(@filepath);

            //Use TagLib extension to gain control of metadata values
            TagLib.File file = TagLib.File.Create(filepath);

            //round the value for duration and change format to minutes & seconds
            TimeSpan duration = file.Properties.Duration;
            int timespanSize = 7; //timespan is always this length
            int factor = (int)Math.Pow(10, (timespanSize));
            string roundedDuration = new TimeSpan(((long)Math.Round((1.0 * duration.Ticks / factor)) * factor)).ToString(@"mm\:ss");

            //create new SongChoice object and assigns values to attributes of object
            SongChoice song = new SongChoice()
            {
                File = fileUri //sets the actual file as an attribute so is linked to info displayed on grid
            };

            // Change nulls to string values for sorting later
            if (file.Tag.Title == null) { song.Name = ""; }
            else { song.Name = file.Tag.Title; }
            if (file.Tag.Album == null) { song.Album = ""; }
            else { song.Album = file.Tag.Album; }
            if (file.Tag.FirstAlbumArtist == null) { song.Artist = ""; }
            else { song.Artist = file.Tag.FirstAlbumArtist; }
            song.Length = roundedDuration.ToString();
            
            if (file.Tag.Pictures.Length >= 1)
            {
                TagLib.IPicture pic = file.Tag.Pictures[0];
                MemoryStream ms = new MemoryStream(pic.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);

                // Create Bitmap from image
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                // Set Picture attribute of song
                song.Picture = bitmap;
            }

            return song;
        }

        private MediaState GetMediaState(MediaElement myMedia)
        {
            FieldInfo hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
            object helperObject = hlp.GetValue(myMedia);
            FieldInfo stateField = helperObject.GetType().GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            MediaState state = (MediaState)stateField.GetValue(helperObject);
            return state;
        }

        private void PausePlay_Click(object sender, RoutedEventArgs e)
        {
            //Queue = songGrid.SelectedItems.OfType<Uri>().ToList();
            ;
            if (GetMediaState(Mp3Player) == MediaState.Play)
            {
                Mp3Player.Pause();
                PausePlayButton.Content = "4";
            }
            else
            {
                Mp3Player.Play();
                PausePlayButton.Content = ";";
            }
        }

        private void Mp3PlayerTimer_Tick(object sender, EventArgs e)
        {
            if (Mp3Player.Source != null && Mp3Player.NaturalDuration.HasTimeSpan) /* We will check if the source is null, and if the media element has a time span and if the answer is yes we execute the following code */
            {
                Mp3Slider.Maximum = Mp3Player.NaturalDuration.TimeSpan.TotalSeconds; /*This will set the maxim value (seconds) of the slider. I like to put it here in the Timer_Tick because I don't like to create a new mediaPlayer every time I open a song so this parameter will not update if I don't call that function again. Putting it here makes sure will always have the right value if songs are changing */
                if (sliderUpdating == true)
                {
                    Mp3Slider.Value = Mp3Player.Position.TotalSeconds; /* Here be careful because in some tutorial online, you will find Position.Seconds which is not a good solution because everytime the seconds arrive to 60, they reset from 1 again so will not work properly. */
                }

                else
                {
                    Mp3Slider.Value = 0; //We don't want the slider to stay in some strange position if the song ends//
                }
            }
        }

        private void Play (SongChoice current, string mode)
        {
            try
            {
                if (mode == "next")
                {
                    SongChoice nextSong = Queue.GetList()[Queue.FindIndex(CurrentSong) + 1];
                    CurrentSong = nextSong;
                    Mp3Player.Source = CurrentSong.File;
                }
                else if (mode == "prev")
                {
                    SongChoice prevSong = Queue.GetList()[Queue.FindIndex(CurrentSong) - 1];
                    CurrentSong = prevSong;
                    Mp3Player.Source = CurrentSong.File;
                }
                else
                {
                    var converter = new System.Windows.Media.BrushConverter();
                    var highlightBrush = (Brush)converter.ConvertFromString("#FFFFFF90");
                    //OnPropertyChanged("CurrentSong");
                    Mp3Player.Source = CurrentSong.File; //refreshes Mp3Player
                    Mp3Player.Play(); //Play song
                    PausePlayButton.Content = ";";
                }
            }
            catch
            {
                if(mode == "prev") { CurrentSong = Queue.GetList()[Queue.GetList().Count - 1]; }
                else { CurrentSong = Queue.GetList()[0]; }
                Mp3Player.Source = CurrentSong.File;
            }
            // Collapse image element if no art
            if (CurrentSong.Picture != null)
            {
                AlbumArt.Visibility = Visibility.Visible;
                LyricsButton.Visibility = Visibility.Visible;
            }
            else
            {
                AlbumArt.Visibility = Visibility.Collapsed;
            }
        }

        //private void playNext()
        //{
        //    try
        //    {
        //        SongChoice nextSong = Queue[Queue.FindIndex(a => a == CurrentSong) + 1];
        //        CurrentSong = nextSong;
        //        Mp3Player.Source = CurrentSong.File;
        //        if(CurrentSong.Picture != null) { ArtSection.Visibility = Visibility.Visible; }
        //        else { ArtSection.Visibility = Visibility.Collapsed; }
        //    }
        //    catch
        //    {
        //        CurrentSong = Queue[0];
        //        Mp3Player.Source = CurrentSong.File;
        //    }
        //}

        //private void playPrevious()
        //{
        //    try
        //    {
        //        SongChoice prevSong = Queue[Queue.FindIndex(a => a == CurrentSong) - 1];
        //        CurrentSong = prevSong;
        //        Mp3Player.Source = CurrentSong.File;
        //        if (CurrentSong.Picture != null) { ArtSection.Visibility = Visibility.Visible; }
        //        else { ArtSection.Visibility = Visibility.Collapsed; }
        //    }
        //    catch
        //    {

        //    }
            
        //}

        private void HandleChecked(object sender, RoutedEventArgs e)
        {
            //ShuffleButton.Background = Brushes.SlateBlue;
            ShuffleButton.Foreground = Brushes.DarkTurquoise;
            shuffledQueue.ClearAndReplace(totalSongSelection);
            shuffledQueue.Shuffle();
            Queue.ClearAndReplace(shuffledQueue);
        }

        private void HandleUnchecked(object sender, RoutedEventArgs e)
        {
            ShuffleButton.Background = Brushes.Transparent;
            ShuffleButton.Foreground = Brushes.White;
            Queue.Clear();
            Queue.AddRange(unshuffledQueue);
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e) //when a song is double clicked in grid
        {
            CurrentSong = songGrid.SelectedItem as SongChoice; //sets SongChoice object CurrentSong to selected item
            Play(CurrentSong, "current");
            if (CurrentSong.Picture != null)
            {
                AlbumArt.Visibility = Visibility.Visible;
                LyricsButton.Visibility = Visibility.Visible;
            }
            else
            {
                AlbumArt.Visibility = Visibility.Collapsed;
            }
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            //var columnHeader = sender as DataGridColumnHeader;
            //if (columnHeader != null)
            //{
            if (sender is DataGridColumnHeader columnHeader)
            {
                string sortMetric = columnHeader.Content.ToString();
                string sortDirection = columnHeader.SortDirection.ToString();

                switch (sortMetric)
                {
                    case " T I T L E":
                        if (sortDirection == "Ascending")
                        {
                            //Queue = Queue.OrderByDescending(n => n.Name).ToList();
                            //MergeSort(Queue, "Name");
                            Queue.AlphabeticalSort("Name");
                            Queue.Reverse();

                        }
                        else
                        {
                            //Queue = Queue.OrderBy(n => n.Name).ToList();
                            //MergeSort(Queue, "Name");
                            Queue.AlphabeticalSort("Name");
                        }
                        break;
                    case " A L B U M":
                        if (sortDirection == "Ascending")
                        {
                            //Queue = Queue.OrderByDescending(a => a.Album).ToList();
                            //MergeSort(Queue, "Album");
                            Queue.AlphabeticalSort("Album");
                            Queue.Reverse();
                        }
                        else
                        {
                            //Queue = Queue.OrderBy(a => a.Album).ToList();
                            //MergeSort(Queue, "Album");
                            Queue.AlphabeticalSort("Album");
                        }
                        break;
                    case " A R T I S T":
                        if (sortDirection == "Ascending")
                        {
                            //Queue = Queue.OrderByDescending(a => a.Artist).ToList();
                            //MergeSort(Queue, "Artist");
                            Queue.AlphabeticalSort("Artist");
                            Queue.Reverse();
                        }
                        else
                        {
                            //Queue = Queue.OrderBy(a => a.Artist).ToList();
                            //MergeSort(Queue, "Artist");
                            Queue.AlphabeticalSort("Artist");
                            Queue.Reverse();
                        }
                        break;
                    case " D U R A T I O N":
                        if (sortDirection == "Ascending")
                        {
                            //Queue = Queue.OrderByDescending(l => TimeSpan.Parse(l.Length)).ToList();
                            Queue.OrderTimeByDescending();
                        }
                        else
                        {
                            //Queue = Queue.OrderBy(l => TimeSpan.Parse(l.Length)).ToList();
                            Queue.OrderTimeByAscending();
                        }
                        break;
                }
            }
        }

        public static List<string> GetAttributes(List<SongChoice> inputList, string att)
        {
            List<string> tempValues = new List<string>();
            foreach (SongChoice song in inputList)
            {
                tempValues.Add(song.GetType().GetProperty(att).GetValue(song, null).ToString());
            }
            return tempValues;
        }

        // Sort list of songs based on specified attribute (att)
        //public static void MergeSort(List<SongChoice> inputList, string att)
        //{
        //    if (inputList.Count > 1)
        //    {
        //        int mid = inputList.Count / 2;
        //        List<SongChoice> leftHalf = new List<SongChoice>();
        //        List<SongChoice> rightHalf = new List<SongChoice>();

        //        // Sets each half of current list to seperate lists
        //        leftHalf.AddRange(inputList.GetRange(0, mid));
        //        rightHalf.AddRange(inputList.GetRange(mid, inputList.Count - mid));

        //        MergeSort(leftHalf, att);
        //        MergeSort(rightHalf, att);

        //        int i = 0;
        //        int j = 0;
        //        int k = 0;

        //        while (i < leftHalf.Count && j < rightHalf.Count)
        //        {
        //            // If leftHalf[i] < leftHalf[j]
        //            if (String.Compare(leftHalf[i].GetType().GetProperty(att).GetValue(leftHalf[i], null).ToString(),
        //                rightHalf[j].GetType().GetProperty(att).GetValue(rightHalf[j], null).ToString(),
        //                comparisonType: StringComparison.OrdinalIgnoreCase) < 0)
        //            {
        //                inputList[k] = leftHalf[i];
        //                i++;
        //            }
        //            else
        //            {
        //                inputList[k] = rightHalf[j];
        //                j++;
        //            }
        //            k++;
        //        }
                
        //        // if left elements are not merged
        //        while (i < leftHalf.Count)
        //        {
        //            inputList[k] = leftHalf[i];
        //            i++;
        //            k++;
        //        }

        //        // if right elements are not merged
        //        while (j < rightHalf.Count)
        //        {
        //            inputList[k] = rightHalf[j];
        //            j++;
        //            k++;
        //        }
        //    }
        //}

        //static public void MainMerge(List<SongChoice> values, int left, int mid, int right, string att)
        //{
        //    SongChoice[] temp = new SongChoice[100];
        //    int i, eol, num, pos;

        //    eol = (mid - 1);
        //    pos = left;
        //    num = (right - left + 1);

        //    while ((left <= eol) && (mid <= right))
        //    {
        //        if (values[left].GetType().GetProperty(att).GetValue(values[left], null).ToString()
        //            .CompareTo(values[mid].GetType().GetProperty(att).GetValue(values[mid], null).ToString()) < 0)
        //            temp[pos++] = values[left++];
        //        else
        //            temp[pos++] = values[mid++];
        //    }

        //    while (left <= eol)
        //        temp[pos++] = values[left++];

        //    while (mid <= right)
        //        temp[pos++] = values[mid++];

        //    for (i = 0; i < num; i++)
        //    {
        //        values[right] = temp[right];
        //        right--;
        //    }
        //}

        //public static void MergeSort(List<SongChoice> values, int left, int right, string att)
        //{
        //    int mid;
        //    if (right > left)
        //    {
        //        mid = (right + left) / 2;
        //        MergeSort(values, left, mid, att);
        //        MergeSort(values, (mid + 1), right, att);

        //        MainMerge(values, left, (mid + 1), right, att);
        //    }
        //}

        private void RW_Click(object sender, RoutedEventArgs e) //when RW button clicked
        {
            if (Mp3Player.Position < TimeSpan.FromSeconds(4)) //if song is less then 4 seconds in
            {
                Play(CurrentSong, "prev"); //play previous song
            }
            else
            {
                Mp3Player.Position = TimeSpan.FromSeconds(0); //otherwise, start song again
            }
        }

        private void FFW_Click(object sender, RoutedEventArgs e) //when FFW button clicked
        {
            Play(CurrentSong, "next"); //play next song
        }

        private void Mp3Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            {
                sliderUpdating = false; //When is set to false the slider is not updating automatically so we can change it
            }
        }

        private void Mp3Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Mp3Player.NaturalDuration.TimeSpan.TotalSeconds > 0)
                {
                    try
                    {
                        Mp3Player.Position = TimeSpan.FromSeconds(Mp3Slider.Value);
                    }
                    catch { return; }
                }
                sliderUpdating = true; //Don't forget to set it true here if no the slider will not update automatically when we release it if is set false.
            }
            catch { }
        }

        private void Mp3Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            Play(CurrentSong, "next");
        }

        private void Mp3Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (Mp3Player.NaturalDuration.HasTimeSpan)
            {
                DispatcherTimer Mp3PlayerTimer = new DispatcherTimer();
                Mp3PlayerTimer.Interval = TimeSpan.FromMilliseconds(1000);
                Mp3PlayerTimer.Tick += Mp3PlayerTimer_Tick;
                Mp3PlayerTimer.Start();
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            UploadWindow newwin = new UploadWindow();
            newwin.Show();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadPage newwin = new DownloadPage();
            newwin.Show();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow newwin = new LoginWindow();
            newwin.Show();
        }

        //private bool handle = true;
        //private void ViewComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (handle) Handle();
        //    handle = true;
        //}

        //private void ViewComboBox_DropDownClosed(object sender, EventArgs e)
        //{
        //    ComboBox viewCB = sender as ComboBox;
        //    handle = !viewCB.IsDropDownOpen;
        //    Handle();
        //}

        //private void Handle()
        //{
        //    switch (ViewComboBox.SelectedItem.ToString().Split(new string[] { ": " }, StringSplitOptions.None).Last())
        //    {
        //        //case "Songs":
        //        //    var control = ContentControlMain.Content as SongsView;
        //        //    if (control != null)
        //        //    {
        //        //        control.songGrid();
        //        //    }
        //        //    break;
        //        case "Artists":
        //            DataContext = new ArtistsViewModel();
        //            break;
        //        case "Albums":
        //            DataContext = new AlbumsViewModel();
        //            break;
        //    }
        //}

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Sets settings to open in maximized mode 
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                // Sets settings to current window size
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
                Properties.Settings.Default.Maximized = false;
            }

            if (User.remember)
            {
                Properties.Settings.Default.UserName = User.username;
                Properties.Settings.Default.UserID = User.id;
                Properties.Settings.Default.RememberUser = true;
            }
            else
            {
                Properties.Settings.Default.RememberUser = false;
            }
            Properties.Settings.Default.Save();

        }

        private void SongGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //private bool handle = true;
        //private void ViewComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (handle)Handle();
        //    handle = true;
        //}

        //private void ViewComboBox_DropDownClosed(object sender, EventArgs e)
        //{
        //    ComboBox viewCB = sender as ComboBox;
        //    handle = !viewCB.IsDropDownOpen;
        //    Handle();
        //}

        //private void Handle()
        //{
        //    switch (ViewComboBox.SelectedItem.ToString().Split(new string[] { ": " }, StringSplitOptions.None).Last())
        //    {
        //        case "Songs":
        //            DataContext = new SongsViewModel();
        //            break;
        //        case "Artists":
        //            DataContext = new ArtistsViewModel();
        //            break;
        //        case "Albums":
        //            DataContext = new AlbumsViewModel();
        //            break;
        //    }
        //}

        private Random rng = new Random(); //generates new random number

        private List<SongChoice> Shuffle(List<SongChoice> list) //method for shuffle taking undefined list as input
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                SongChoice value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow newwin = new SettingsWindow();
            newwin.Show();
        }

        private void UserDisplay_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (UserExpander.Header as string == null || UserExpander.Header as string == "")
            {
                UserExpander.Visibility = Visibility.Collapsed;
            }
            else
            {
                UserExpander.Visibility = Visibility.Visible;
            }
        }

        private void LoadDefaultSongs()
        {
            try
            {
                foreach (string filename in Directory.EnumerateFiles(Properties.Settings.Default.DefaultPath, "*.mp3"))
                {
                    LoadIntoGrid();
                    totalSongSelection.Add(GenerateObjects(filename));
                    InitialiseQueues();
                    songGrid.Items.Refresh();
                }
            }
            catch { }
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            User.id = -1;
            User.username = null;
            Properties.Settings.Default.RememberUser = false;
            UserExpander.Visibility = Visibility.Collapsed;

        }

        private void LyricsButton_Click(object sender, RoutedEventArgs e)
        {
            LyricsWindow newWindow = new LyricsWindow(CurrentSong);
            newWindow.Show();
        }
    }
}
