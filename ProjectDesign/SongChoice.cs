using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDesign
{
    class SongChoice
    {

        public string Name { get; set; }
        public string Length { get; set; }
        public string Album { get; set; }
        public string Artist { get; set; }


        //public List<SongChoice> Songs = new List<SongChoice>();

        public List<SongChoice> AddSongs(List<SongChoice> inputList)
        {
            inputList.Add(new SongChoice() { Name = this.Name, Artist = this.Artist, Album = this.Album, Length = this.Length });
            return inputList;
        }


    }
}
