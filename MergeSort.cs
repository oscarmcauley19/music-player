using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDesign
{
    static public class MergeSort
    {
        // Custom C# merge sort algorithm based on pseudocode from 
        // PG Online Textbook
        static public void Sort(List<SongChoice> inputList, string att)
        {
            // If there's actually a list to sort
            if (inputList.Count > 1)
            {
                // Find middle of list
                int mid = inputList.Count / 2;

                // Create two separate halves
                List<SongChoice> leftHalf = new List<SongChoice>();
                List<SongChoice> rightHalf = new List<SongChoice>();

                // Set each half of current list to seperate lists
                leftHalf.AddRange(inputList.GetRange(0, mid));
                rightHalf.AddRange(inputList.GetRange(mid, inputList.Count - mid));

                // Sort each separately based on given attribute att
                // e.g. Title or Artist
                Sort(leftHalf, att);
                Sort(rightHalf, att);

                int i = 0;
                int j = 0;
                int k = 0;

                while (i < leftHalf.Count && j < rightHalf.Count)
                {
                    // If leftHalf[i] < leftHalf[j] then run
                    // Code very complicated because it is getting value of given 
                    // attribute 'att' and is comparing string value for each
                    if (String.Compare(leftHalf[i].GetType().GetProperty(att).GetValue(leftHalf[i], null).ToString(),
                        rightHalf[j].GetType().GetProperty(att).GetValue(rightHalf[j], null).ToString(),
                        comparisonType: StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        // set kth value in inputList to ith value in leftHalf
                        inputList[k] = leftHalf[i];
                        i++; //increment
                    }
                    else
                    {
                        // set kth value in inputList to jth value in leftHalf
                        inputList[k] = rightHalf[j];
                        j++; //increment
                    }
                    k++; //increment
                }

                // if left elements are not merged
                while (i < leftHalf.Count)
                {
                    // set kth value in inputList to ith value in leftHalf
                    inputList[k] = leftHalf[i];
                    i++;
                    k++; //increment both k and i
                }

                // if right elements are not merged
                while (j < rightHalf.Count)
                {
                    // set kth value in inputList to jth value in leftHalf
                    inputList[k] = rightHalf[j];
                    j++;
                    k++; //increment both k and i
                }
            }
        }
    }
}
