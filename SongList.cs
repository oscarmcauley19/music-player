using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDesign
{
    public class SongList
    {
        private List<SongChoice> songs = new List<SongChoice>();
        private Random randomNum = new Random();

        public void Add(SongChoice song)
        {
            this.songs.Add(song);
        }

        public List<SongChoice> GetList()
        {
            return songs;
        }

        public void ClearAndReplace(SongList inputList)
        {
            songs.Clear();
            foreach (SongChoice song in inputList.GetList())
            {
                songs.Add(song);
            }
        }

        public void AddRange(SongList inputList)
        {
            foreach(SongChoice song in inputList.GetList())
            {
                songs.Add(song);
            }
        }

        public void Clear()
        {
            songs.Clear();
        }

        public void AlphabeticalSort(string attribute, bool ascending)
        {
            if (attribute == "Name")
            {
                MergeSort.Sort(songs, attribute);
            }
            
            MergeSort.Sort(songs, attribute);
            List<SongChoice> newList = new List<SongChoice>();

            List<string> tempList = new List<string>();
            //tempList.AddRange(this);
            foreach (SongChoice song in this.GetList())
            {
                var result = song.GetType().GetProperty(attribute).GetValue(song, null);
                if (!tempList.Contains(result.ToString()))
                {
                    tempList.Add(result.ToString());
                }
            }
            if (!ascending)
            {
                tempList.Reverse();
            }

            List<SongChoice> smallList = new List<SongChoice>();
            foreach (string item in tempList)
            {
                smallList.AddRange(this.GetList().FindAll(i => i.GetType()
                .GetProperty(attribute).GetValue(i, null).ToString() == item));
                MergeSort.Sort(smallList, "Name");
                newList.AddRange(smallList);
                smallList.Clear();
            }
            songs.Clear();
            songs.AddRange(newList);
            //songs = songs.OrderBy(a => a.Artist).ToList();      
        }

        public void Shuffle()
        {
            int n = songs.Count;
            while (n > 1)
            {
                n--;
                int k = randomNum.Next(n + 1);
                SongChoice value = songs[k];
                songs[k] = songs[n];
                songs[n] = value;
            }
        }

        public void Reverse()
        {
            songs.Reverse();
        }

        public void OrderTimeByDescending()
        {
            songs = songs.OrderByDescending(l => TimeSpan.Parse(l.Length)).ToList();
        }

        public void OrderTimeByAscending()
        {
            songs = songs.OrderBy(l => TimeSpan.Parse(l.Length)).ToList();
        }

        public int FindIndex(SongChoice song)
        {
            return songs.FindIndex(a => a == song);
        }
    }
}
