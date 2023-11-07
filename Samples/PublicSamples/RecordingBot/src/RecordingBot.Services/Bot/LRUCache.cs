// ***********************************************************************
// Assembly         : RecordingBot.Services
// Author           : JasonTheDeveloper
// Created          : 09-07-2020
//
// Last Modified By : dannygar
// Last Modified On : 08-17-2020
// ***********************************************************************
// <copyright file="LRUCache.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>Initialize the HttpConfiguration for OWIN</summary>
// ***********************************************************************-

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

        /// <summary>
        /// Initializes a new instance of the <see cref="LRUCache" /> class.
        /// Constructor for the LRU cache.
        /// </summary>
        /// <param name="size">Size ofthe cache.</param>
        /// <exception cref="ArgumentException">size value too large; max value is {Max}</exception>
        public LRUCache(uint size)
        {
            if (size > Max)
            {
                throw new ArgumentException($"size value too large; max value is {Max}");
            }

            this.set = new uint[size];
        }

        /// <summary>
        /// Gets the count of items in the cache.
        /// </summary>
        /// <value>The count.</value>
        public uint Count { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "{" + string.Join(", ", this.set) + "}";
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

            lock (this.set)
            {
                // empty set, first item goes at front
                if (this.Count == 0)
                {
                    this.Count = 1;
                    this.set[0] = k;
                }

                // no change if k is at front of set
                else if (this.set[0] != k)
                {
                    // look for k in the set
                    uint ik = 0;  // index of k in set; 0 if k not found
                    for (uint i = 1; i < this.Count; i++)
                    {
                        if (this.set[i] == k)
                        {
                            ik = i;
                            break;
                        }
                    }

                    // if k not found, make room for it
                    if (ik == 0)
                    {
                        if (this.Count == this.set.Length)
                        {
                            // if set is full, record the item being evicted (and no change in # of items)
                            e = this.set[this.Count - 1];
                            ik = this.Count - 1;
                        }
                        else
                        {
                            // room to add new item: shift the entire set right and increment # of items
                            ik = this.Count++;
                        }
                    }

                    this.ShiftRight(ik);
                    this.set[0] = k;
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
            lock (this.set)
            {
                for (uint i = 0; i < this.Count; i++)
                {
                    // if found item k, remove it from the set
                    if (this.set[i] == k)
                    {
                        this.ShiftLeft(i);
                        this.Count--;
                        this.set[this.Count] = 0;
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
                this.set[x] = this.set[x - 1];
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
            while (x < this.set.Length - 1)
            {
                this.set[x] = this.set[x + 1];
                x++;
            }
        }
    }
}
