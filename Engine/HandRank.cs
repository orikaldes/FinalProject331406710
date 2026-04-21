using System;
using System.Collections.Generic;
using System.Linq;

namespace FinalProject331406710.Engine
{
    // "IComparable<HandRank>": This is a special interface.
    // It promises that this class has a "CompareTo" method, allowing us to sort lists of HandRanks easily.
    public class HandRank : IComparable<HandRank>
    {
        // The main category (e.g., FullHouse, Pair).
        public HandType HandType { get; set; }

        // A list of cards used to break ties.
        public List<Rank> Kickers { get; set; }

        public HandRank()
        {
            Kickers = new List<Rank>();
        }

        // The Logic for comparing this hand vs another hand.
        // Returns 1 if 'this' wins, -1 if 'other' wins, 0 if tie.
        public int CompareTo(HandRank other)
        {
            // 1. Compare the main Categories first.
            if (this.HandType > other.HandType) return 1;
            if (this.HandType < other.HandType) return -1;

            // 2. If Categories are equal (both have Flush), compare Kickers one by one.
            for (int i = 0; i < this.Kickers.Count; i++)
            {
                if (this.Kickers[i] > other.Kickers[i]) return 1;
                if (this.Kickers[i] < other.Kickers[i]) return -1;
            }

            // 3. If we get here, the hands are identical. It's a split pot.
            return 0;
        }

        // Helper to print the hand nicely
        public override string ToString()
        {
            string kickerStr = string.Join(", ", Kickers.Select(k => k.ToString()));
            return $"{HandType} (Kickers: {kickerStr})";
        }
    }
}