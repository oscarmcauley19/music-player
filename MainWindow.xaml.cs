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
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;
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
                // Allows XAML to track when CurrentSong updated
                this.OnPropertyChanged("CurrentSong");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadDefaultSongs();

            this.DataContext = this; //sets data context to MainWindow class

            // Set dimensions remembered from last session
            SetWindowDimensions(
                Properties.Settings.Default.Top,
                Properties.Settings.Default.Left,
                Properties.Settings.Default.Height,
                Properties.Settings.Default.Width,
                Properties.Settings.Default.Maximized);

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

        private void SetWindowDimensions(double top, double left, double height, double width, bool maximised)
        {
            // Set dimensions based on params
            this.Top = top;
            this.Left = left ;
            this.Height = height;
            this.Width = width;

            if (maximised)
            {
                WindowState = WindowState.Maximized;
            }
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
                        // Add song to list
                        totalSongSelection.Add(GenerateObjects(filename));
                    }
                    catch
                    {
                        // Error message
                        MessageBox.Show("Files could not be imported.");
                    }
                }
                InitialiseQueues();
            }

        }

        private void InitialiseQueues()
        {
            // Reset the song grid with new items
            songGrid.Items.Refresh();

            SongList tempList = new SongList();
            tempList.AddRange(totalSongSelection);

            // Unshuffled & shuffled queues exist alongside each other
            unshuffledQueue.ClearAndReplace(totalSongSelection);
            shuffledQueue.ClearAndReplace(tempList);
            shuffledQueue.Shuffle();

            // If shuffle is chosen
            if (ShuffleButton.IsChecked == true)
            {
                // Sets current queue to shuffled one
                Queue.ClearAndReplace(shuffledQueue);
            }
            else
            {
                // Sets current queue to unshuffled one
                Queue.ClearAndReplace(unshuffledQueue);
            }
        }

        private void LoadIntoGrid()
        {
            CollectionViewSource itemCollectionViewSource;
            itemCollectionViewSource = (CollectionViewSource)(FindResource("ItemCollectionViewSource"));
            itemCollectionViewSource.Source = totalSongSelection.GetList();
            songGrid.ScrollIntoView(CollectionView.NewItemPlaceholder);
            // this initialises the data binding environment between the DataGrid and totalSongSelection list
            // through use of a item collection, which allows for sorting & filtering
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
            // Code from StackOverflow to find whether media element is playing
            // No easy way to do so in WPF
            FieldInfo hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
            object helperObject = hlp.GetValue(myMedia);
            FieldInfo stateField = helperObject.GetType().GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            MediaState state = (MediaState)stateField.GetValue(helperObject);
            return state;
        }
        
        // Method runs when pause button clicked
        private void PausePlay_Click(object sender, RoutedEventArgs e)
        {
            //Queue = songGrid.SelectedItems.OfType<Uri>().ToList();
            ;
            if (GetMediaState(Mp3Player) == MediaState.Play)
            {
                // Pause if currently playing
                Mp3Player.Pause();
                // In font used 4 corresponds to play symbol
                PausePlayButton.Content = "4";
            }
            else
            {
                // Play if currently paused
                Mp3Player.Play();
                // In font used ; corresponds to pause symbol
                PausePlayButton.Content = ";";
            }
        }

        private void Mp3PlayerTimer_Tick(object sender, EventArgs e)
        {
            // Check if the source is null, and if the media element has a time span 
            // If yes, then run
            if (Mp3Player.Source != null && Mp3Player.NaturalDuration.HasTimeSpan) 
            {
                // Set max value of slider in seconds
                Mp3Slider.Maximum = Mp3Player.NaturalDuration.TimeSpan.TotalSeconds;
                if (sliderUpdating == true)
                {
                    Mp3Slider.Value = Mp3Player.Position.TotalSeconds;
                }

                else
                {
                    // Reset position of ticker after song ends
                    Mp3Slider.Value = 0;
                }
            }
        }

        private SongChoice Play (SongChoice current, string mode)
        {
            try
            {
                if (mode == "next")
                {
                    // Skip forward a song
                    SongChoice nextSong = Queue.GetList()[Queue.FindIndex(current) + 1];
                    current = nextSong;
                    // Play new song
                    Mp3Player.Source = current.File;
                    return current;
                }
                else if (mode == "prev")
                {
                    // Go back a song
                    SongChoice prevSong = Queue.GetList()[Queue.FindIndex(current) - 1];
                    current = prevSong;
                    // Play new song
                    Mp3Player.Source = current.File;
                    return current;
                }
                else
                {
                    //var converter = new BrushConverter();
                    //var highlightBrush = (Brush)converter.ConvertFromString("#FFFFFF90");
                    Mp3Player.Source = current.File; //refreshes Mp3Player
                    Mp3Player.Play(); //Play song
                    PausePlayButton.Content = ";";
                    return current;
                }
            }
            // if indexing error occurs
            catch
            {
                if(mode == "prev")
                {
                    // Go back to end of queue when rewinding from 1st song
                    current = Queue.GetList()[Queue.GetList().Count - 1];
                }
                else
                {
                    // If going forward & reached end of queue, go back to start
                    current = Queue.GetList()[0];
                }
                Mp3Player.Source = current.File;
                return current;
            }
            // Collapse image element if no art
            if (current.Album != null || current.Artist != null)
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
            // Shuffle button changes colour to show it's on
            ShuffleButton.Foreground = Brushes.DeepPink;
            ShuffleButton.FontWeight = FontWeights.Bold;
            //Re-shuffle queue and set to current
            shuffledQueue.ClearAndReplace(totalSongSelection);
            shuffledQueue.Shuffle();
            Queue.ClearAndReplace(shuffledQueue);
        }

        private void HandleUnchecked(object sender, RoutedEventArgs e)
        {
            // Return button to normal look to show it's off
            ShuffleButton.Background = Brushes.Transparent;
            ShuffleButton.Foreground = Brushes.White;
            ShuffleButton.FontWeight = FontWeights.Normal;
            // Set Queue to unshuffled again
            Queue.ClearAndReplace(unshuffledQueue);
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e) //when a song is double clicked in grid
        {
            CurrentSong = songGrid.SelectedItem as SongChoice; //sets SongChoice object CurrentSong to selected item
            CurrentSong = Play(CurrentSong, "current");
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
                    // title header clicked
                    case " T I T L E":
                        // if current is ascending, new will be descending
                        if (sortDirection == "Ascending")
                        {
                            // sorted in descending by song title
                            unshuffledQueue.AlphabeticalSort("Name", false);
                        }
                        else
                        {
                            // sorted in ascending by song title
                            unshuffledQueue.AlphabeticalSort("Name", true);
                        }
                        break;
                    // album header clicked
                    case " A L B U M":
                        if (sortDirection == "Ascending")
                        {
                            // CUSTOM merge sort algorithm orders first on album,
                            // then by title so songs of same album are also ordered
                            unshuffledQueue.AlphabeticalSort("Album", false);
                        }
                        else
                        {
                            unshuffledQueue.AlphabeticalSort("Album", true);
                        }
                        break;
                    // artist header clicked
                    case " A R T I S T":
                        if (sortDirection == "Ascending")
                        {
                            // CUSTOM merge sort algorithm orders first on artist,
                            // then by title so songs of same artist are also ordered
                            unshuffledQueue.AlphabeticalSort("Artist", false);
                        }
                        else
                        {
                            unshuffledQueue.AlphabeticalSort("Artist", true);
                        }
                        break;
                    // duration header clicked
                    case " D U R A T I O N":
                        if (sortDirection == "Ascending")
                        {
                            // Default C# algorithm used as custom merge sort
                            // designed for strings - this method is hard
                            // to replicate
                            unshuffledQueue.OrderTimeByDescending();
                        }
                        else
                        {

                            unshuffledQueue.OrderTimeByAscending();
                        }
                        break;
                }

                // if shuffle is disabled
                if (ShuffleButton.IsChecked == false)
                {
                    // set current queue to new order
                    Queue.ClearAndReplace(unshuffledQueue);
                }
            }
        }

        //public static List<string> GetAttributes(List<SongChoice> inputList, string att)
        //{
        //    // 
        //    List<string> tempValues = new List<string>();
        //    foreach (SongChoice song in inputList)
        //    {
        //        tempValues.Add(song.GetType().GetProperty(att).GetValue(song, null).ToString());
        //    }
        //    return tempValues;
        //}

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

        private void RW_Click(object sender, RoutedEventArgs e) // when RW button clicked
        {
            if (Mp3Player.Position < TimeSpan.FromSeconds(4)) // if song is less then 4 seconds in
            {
                CurrentSong = Play(CurrentSong, "prev"); // play previous song
            }
            else
            {
                Mp3Player.Position = TimeSpan.FromSeconds(0); // otherwise, start song again
            }
        }

        private void FFW_Click(object sender, RoutedEventArgs e) //when FFW button clicked
        {
            CurrentSong = Play(CurrentSong, "next"); //play next song
        }

        private void Mp3Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            {
                // When is set to false the slider is not updating automatically so we can change it
                sliderUpdating = false; 
            }
        }

        // When user releases mouse, ticker is in new position
        private void Mp3Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Mp3Player.NaturalDuration.TimeSpan.TotalSeconds > 0)
                {
                    try
                    {
                        // Sets new position set by user
                        Mp3Player.Position = TimeSpan.FromSeconds(Mp3Slider.Value);
                    }
                    catch { return; }
                }

                // Ensure slider continues to update automatically
                sliderUpdating = true;
            }
            catch { }
        }

        // When song ends
        private void Mp3Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Set CurrentSong next in queue
            CurrentSong = Play(CurrentSong, "next");
        }

        // If media player is given a file to play
        private void Mp3Player_MediaOpened(object sender, RoutedEventArgs e)
        { 
            // error handling - only attempts time tracking if actually possible
            if (Mp3Player.NaturalDuration.HasTimeSpan)
            {
                // Timer starts
                DispatcherTimer Mp3PlayerTimer = new DispatcherTimer();
                Mp3PlayerTimer.Interval = TimeSpan.FromMilliseconds(1000);
                Mp3PlayerTimer.Tick += Mp3PlayerTimer_Tick;
                Mp3PlayerTimer.Start();
            }
        }

        // Upload button clicked
        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            // Open upload window
            UploadWindow newwin = new UploadWindow();
            newwin.Show();
        }

        // Download button clicked
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            // Open download window
            DownloadWindow newwin = new DownloadWindow();
            newwin.Show();
        }

        // Login button clicked
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Open login window
            LoginWindow newwin = new LoginWindow();
            newwin.Show();
        }

        // Settings Button Clicked
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Settings window opened
            SettingsWindow newwin = new SettingsWindow();
            newwin.Show();
        }

        // Sign out button clicked in user dropdown menu
        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            // ID set to signal value - tells program no user
            User.id = -1;
            User.username = null;
            // Stop program trying to auto-login on startup
            Properties.Settings.Default.RememberUser = false;
            // Close user display as no one logged in anymore
            UserExpander.Visibility = Visibility.Collapsed;

        }

        // 'Discover' button clicked
        private void LyricsButton_Click(object sender, RoutedEventArgs e)
        {
            // Discover/lyrics window opened
            LyricsWindow newWindow = new LyricsWindow(CurrentSong);
            newWindow.Show();
        }

        // Runs when user closes main window
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

            // If user has chosen to be remembered
            if (User.remember)
            {
                // Sets user details to defaults for next session
                Properties.Settings.Default.UserName = User.username;
                Properties.Settings.Default.UserID = User.id;
                Properties.Settings.Default.RememberUser = true;
            }
            else
            {
                // Remember user is set to false - means program won't
                // try to run login procedure automatically on start
                Properties.Settings.Default.RememberUser = false;
            }

            // Properties are saved for next session
            Properties.Settings.Default.Save();
        }

        private void SongGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Empty but kept in case wanted to add functionality
        }

        // Runs when user login status has changed and the user display has been updated
        private void UserDisplay_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            // If no user is logged in
            if (UserExpander.Header as string == null || UserExpander.Header as string == "")
            {
                // Hide user section
                UserExpander.Visibility = Visibility.Collapsed;
            }
            // If new login
            else
            {
                // Display the user section
                UserExpander.Visibility = Visibility.Visible;
            }
        }

        // Run at start to load the default directory of songs given by user
        private void LoadDefaultSongs()
        {
            try
            {
                // For every song in location
                foreach (string filename in Directory.EnumerateFiles
                    (Properties.Settings.Default.DefaultPath, "*.mp3"))
                {
                    // Do initialisation procedure for adding songs
                    LoadIntoGrid();
                    totalSongSelection.Add(GenerateObjects(filename));
                    InitialiseQueues();

                    // Display songs to user
                    songGrid.Items.Refresh();
                }
            }
            // No message to user as error simply means they
            // haven't chosen to auto-load files
            catch { }
        }
    }
}
