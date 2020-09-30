using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ReverseRegex.Containers
{
    public class RangeList : IEnumerable<(int start, int end)>
    {
        public readonly List<(int start, int end)> Ranges = new List<(int start, int end)>();

        /// <summary>
        /// Returns the index into the <see cref="Ranges"/> list that the <paramref name="value"/> sits at, or the bitwise inverse of the index of the element larger than <paramref name="value"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int Index(int value)
        {
            var left = 0;
            var right = Ranges.Count - 1;

            while(left <= right)
            {
                var middle = (right - left) / 2 + left;
                if (value < Ranges[middle].start)
                {
                    right = middle - 1;
                }
                else if (value > Ranges[middle].end)
                {
                    left = middle + 1;
                }
                else
                {
                    return middle;
                }
            }

            return ~left;
        }

        public bool Contains(int value) => Index(value) >= 0;

        public void Add(int value)
        {
            if(Ranges.Count == 0)
            {
                Ranges.Add((value, value));
                return;
            }

            var index = Index(value);

            if(index < 0)
            {
                index = ~index;
                if (index == Ranges.Count)
                {
                    if (Ranges[^1].end + 1 == value)
                    {
                        Ranges[^1] = (Ranges[^1].start, value);
                    }
                    else
                    {
                        Ranges.Add((value, value));
                    }
                }
                else if(Ranges[index].start - 1 == value)
                {
                    if (index > 0 && Ranges[index - 1].end + 1 == value)
                    {
                        Ranges[index - 1] = (Ranges[index - 1].start, Ranges[index].end);
                        Ranges.RemoveAt(index);
                    }
                    else
                    {
                        Ranges[index] = (value, Ranges[index].end);
                    }
                }
                else if(index > 0 && Ranges[index - 1].end + 1 == value)
                {
                    Ranges[index - 1] = (Ranges[index - 1].start, value);
                }
                else
                {
                    Ranges.Insert(index, (value, value));
                }
            }
        }

        public void Add(int start, int end)
        {
            if(Ranges.Count == 0)
            {
                Ranges.Add((start, end));
                return;
            }

            if(start == end)
            {
                Add(start);
                return;
            }

            if(end < start)
            {
                (end, start) = (start, end);
            }

            var startIndex = Index(start);
            var endIndex = Index(end);

            if(startIndex == endIndex)
            {
                if(startIndex < 0)
                {
                    var index = ~startIndex;
                    if((index == Ranges.Count || Ranges[index].start - 1 != end) && (index == 0 || Ranges[index - 1].end + 1 != start))
                    {
                        // Entirely contained in the gap outside existing ranges without touching
                        Ranges.Insert(index, (start, end));
                        return;
                    }
                }
                else
                {
                    // Entirely contained inside an existing range
                    return;
                }
            }

            if (startIndex < 0)
            {
                startIndex = ~startIndex;

                if(startIndex > 0)
                {
                    if(Ranges[startIndex - 1].end + 1 == start)
                    {
                        startIndex--;
                        start = Ranges[startIndex].start;
                    }
                }
            }
            else
            {
                start = Ranges[startIndex].start;
            }

            if (endIndex < 0)
            {
                endIndex = ~endIndex;

                if(endIndex < Ranges.Count && Ranges[endIndex].start - 1 == end)
                {
                    end = Ranges[endIndex].end;
                }
                else
                {
                    endIndex--;
                }
            }
            else
            {
                end = Ranges[endIndex].end;
            }

            Debug.Assert(startIndex <= endIndex);
            Ranges[startIndex] = (start, end);
            if(startIndex < endIndex)
            {
                Ranges.RemoveRange(startIndex + 1, endIndex - startIndex);
            }
        }

        public IEnumerator<(int start, int end)> GetEnumerator() => Ranges.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int this[int i]
        {
            get
            {
                foreach ((int start, int end) in Ranges)
                {
                    var len = end - start + 1;
                    if(i < len)
                    {
                        return start + i;
                    }
                    i -= len;
                }
                throw new IndexOutOfRangeException();
            }
        }


        public int Count => Ranges.Aggregate(0, (l, r) => l + r.end - r.start + 1);
    }
}
