using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDesign
{
    public class SongList
    {
        // songs contains actual list of SongChoice objects
        private List<SongChoice> songs = new List<SongChoice>();
        private Random randomNum = new Random();

        // Retrieve list of objects from SongList object
        public List<SongChoice> GetList()
        {
            return songs;
        }

        // Add song to list
        public void Add(SongChoice song)
        {
            this.songs.Add(song);
        }

        // Add multiple songs at once
        public void AddRange(SongList inputList)
        {
            foreach(SongChoice song in inputList.GetList())
            {
                songs.Add(song);
            }
        }

        // Replace ALL contents of SongList with new songs
        public void ClearAndReplace(SongList inputList)
        {
            songs.Clear();
            foreach (SongChoice song in inputList.GetList())
            {
                songs.Add(song);
            }
        }

        // Empty list
        public void Clear()
        {
            songs.Clear();
        }

        // Sort SongList alphabetically based on given attribute e.g Artist
        public void AlphabeticalSort(string attribute, bool ascending)
        {
            // Run merge sort once if just name
            if (attribute == "Name")
            {
                MergeSort.Sort(songs, attribute);

                // If in descending order then reverse
                if (!ascending)
                {
                    songs.Reverse();
                }
            }

            else
            {
                try
                {
                    // Sort list based on attributes
                    // This won't sort songs within same artist or album
                    // so further sorting needed
                    MergeSort.Sort(songs, attribute);

                    List<SongChoice> newList = new List<SongChoice>();
                    List<string> tempList = new List<string>();

                    // For every song in this SongList
                    foreach (SongChoice song in this.GetList())
                    {
                        // Retrieves value of chosen attribute for that song
                        var result = song.GetType().GetProperty(attribute).GetValue(song, null);

                        // Adds every unique value to a list - e.g every different
                        // artist in a list of songs
                        if (!tempList.Contains(result.ToString()))
                        {
                            tempList.Add(result.ToString());
                        }
                    }

                    // Reverses if descending has been chosen
                    if (!ascending)
                    {
                        tempList.Reverse();
                    }

                    // Temp list to hold all songs of same artist or album
                    List<SongChoice> smallList = new List<SongChoice>();

                    // For each unique artist/album (depending on attribute)
                    foreach (string item in tempList)
                    {
                        // Add every song that has given attribute with value
                        // identical to item
                        smallList.AddRange(this.GetList().FindAll(i => i.GetType()
                        .GetProperty(attribute).GetValue(i, null).ToString() == item));

                        // Sort this collection of songs & add to overall list
                        MergeSort.Sort(smallList, "Name");
                        newList.AddRange(smallList);

                        // Empty temp list for next artist/album
                        smallList.Clear();
                    }

                    // Set this SongList to new order
                    songs.Clear();
                    songs.AddRange(newList);
                }
                catch { }
            }
        }

        // Shuffles song list
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

        // Reverse order of list
        public void Reverse()
        {
            songs.Reverse();
        }

        // Order by length of song
        public void OrderTimeByDescending()
        {
            songs = songs.OrderByDescending(l => TimeSpan.Parse(l.Length)).ToList();
        }

        public void OrderTimeByAscending()
        {
            songs = songs.OrderBy(l => TimeSpan.Parse(l.Length)).ToList();
        }

        // Find index of given song in SongList
        public int FindIndex(SongChoice song)
        {
            return songs.FindIndex(a => a == song);
        }
    }
}
