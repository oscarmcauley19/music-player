using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDesign
{
    static public class MergeSort
    {
        static public void Sort(List<SongChoice> inputList, string att)
        {
            if (inputList.Count > 1)
            {
                int mid = inputList.Count / 2;
                List<SongChoice> leftHalf = new List<SongChoice>();
                List<SongChoice> rightHalf = new List<SongChoice>();

                // Sets each half of current list to seperate lists
                leftHalf.AddRange(inputList.GetRange(0, mid));
                rightHalf.AddRange(inputList.GetRange(mid, inputList.Count - mid));

                Sort(leftHalf, att);
                Sort(rightHalf, att);

                int i = 0;
                int j = 0;
                int k = 0;

                while (i < leftHalf.Count && j < rightHalf.Count)
                {
                    // If leftHalf[i] < leftHalf[j]
                    if (String.Compare(leftHalf[i].GetType().GetProperty(att).GetValue(leftHalf[i], null).ToString(),
                        rightHalf[j].GetType().GetProperty(att).GetValue(rightHalf[j], null).ToString(),
                        comparisonType: StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        inputList[k] = leftHalf[i];
                        i++;
                    }
                    else
                    {
                        inputList[k] = rightHalf[j];
                        j++;
                    }
                    k++;
                }

                // if left elements are not merged
                while (i < leftHalf.Count)
                {
                    inputList[k] = leftHalf[i];
                    i++;
                    k++;
                }

                // if right elements are not merged
                while (j < rightHalf.Count)
                {
                    inputList[k] = rightHalf[j];
                    j++;
                    k++;
                }
            }
        }
    }
}
