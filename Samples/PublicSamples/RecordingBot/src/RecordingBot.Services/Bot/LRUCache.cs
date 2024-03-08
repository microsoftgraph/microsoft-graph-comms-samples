using System;

namespace RecordingBot.Services.Bot
{
    /// <summary>
    /// Helper LRUCache used for the multiview subscription process.
    /// </summary>
    public class LRUCache
    {
        /// <summary>
        /// Maximum size of the LRU cache.
        /// </summary>
        public const uint Max = 10;

        /// <summary>
        /// LRU Cache.
        /// </summary>
        private uint[] set;

        public LRUCache(uint size)
        {
            if (size > Max)
            {
                throw new ArgumentException($"size value too large; max value is {Max}");
            }

            set = new uint[size];
        }

        /// <summary>
        /// Gets the count of items in the cache.
        /// </summary>
        /// <value>The count.</value>
        public uint Count { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "{" + string.Join(", ", set) + "}";
        }

        /// <summary>
        /// Insert item k at the front of the set, possibly evicting item e;
        /// if k is already in the set, move it to the front, shifting the items
        /// before k over to the right one position.
        /// </summary>
        /// <param name="k">Item to insert.</param>
        /// <param name="e">Item to evict (optional).</param>
        public void TryInsert(uint k, out uint? e)
        {
            e = null;

            lock (set)
            {
                // empty set, first item goes at front
                if (Count == 0)
                {
                    Count = 1;
                    set[0] = k;
                }

                // no change if k is at front of set
                else if (set[0] != k)
                {
                    // look for k in the set
                    uint ik = 0;  // index of k in set; 0 if k not found
                    for (uint i = 1; i < Count; i++)
                    {
                        if (set[i] == k)
                        {
                            ik = i;
                            break;
                        }
                    }

                    // if k not found, make room for it
                    if (ik == 0)
                    {
                        if (Count == set.Length)
                        {
                            // if set is full, record the item being evicted (and no change in # of items)
                            e = set[Count - 1];
                            ik = Count - 1;
                        }
                        else
                        {
                            // room to add new item: shift the entire set right and increment # of items
                            ik = Count++;
                        }
                    }

                    ShiftRight(ik);
                    set[0] = k;
                }
            }
        }

        /// <summary>
        /// Remove item k from the set (if found); shifting any items after k
        /// to the left one position to fill the gap.
        /// </summary>
        /// <param name="k">Item to remove.</param>
        /// <returns>True if item was removed.</returns>
        public bool TryRemove(uint k)
        {
            lock (set)
            {
                for (uint i = 0; i < Count; i++)
                {
                    // if found item k, remove it from the set
                    if (set[i] == k)
                    {
                        ShiftLeft(i);
                        Count--;
                        set[Count] = 0;
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// shift all items starting at index 0 thru index x-1 right one position.
        /// allows the item at index x to be "moved" to the front of the set (at index 0).
        /// given a set:            { a, b, c, d, e }.
        /// shiftRight(2) produces: { a, a, b, d, e }, making room for 'c' to go to the front.
        /// </summary>
        /// <param name="x">Index of last element being shifted.</param>
        private void ShiftRight(uint x)
        {
            while (x > 0)
            {
                set[x] = set[x - 1];
                x--;
            }
        }

        /// <summary>
        /// shift items from index x+1 to the end of the set to the left one position.
        /// given the set:         { a, b, c, d, e, f },
        /// shiftLeft(3) produces: { a, b, c, e, f, f }, allowing the duplicate item at
        /// the end of the set to be removed.
        /// </summary>
        /// <param name="x">Index of items to shift left after this.</param>
        private void ShiftLeft(uint x)
        {
            while (x < set.Length - 1)
            {
                set[x] = set[x + 1];
                x++;
            }
        }
    }
}
