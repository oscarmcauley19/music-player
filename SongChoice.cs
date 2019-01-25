using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectDesign
{
    public class SongChoice : INotifyPropertyChanged
    {
        //sets attributes based on parameters passed through

        public Uri fileValue;
        public Uri File {
            get { return fileValue; }
            set { fileValue = value; }
        }

        public string Name { get; set; }
        public string Length { get; set; }
        public string Album { get; set; }
        public string Artist { get; set; }
        public ImageSource Picture { get; set; }

        //method for adding songs to a list that's passed through
        public List<SongChoice> AddSongs(List<SongChoice> inputList)
        {
            inputList.Add(new SongChoice() { File = this.File, Name = this.Name, Artist = this.Artist, Album = this.Album, Length = this.Length, Picture = this.Picture});
            //adds new song object using values already defined
            return inputList;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }

        }

    }
}
