using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject331406710.Engine
{
    public class Player
    {
        public string Name { get; private set; }
        public int Stack { get; set; }

        // Tracks how much this player has bet in the CURRENT round
        public int CurrentBetInRound { get; set; }

        // The 2 cards the player is holding.
        public List<Card> HoleCards { get; set; }

        // The best hand the player can make. Calculated at the end.
        public HandRank BestHand { get; set; }

        public bool IsFolded { get; set; } // True if they gave up.
        public bool IsAllIn { get; set; }  // True if they bet everything.

        public Player(string name, int initialStack)
        {
            Name = name;
            Stack = initialStack;
            HoleCards = new List<Card>();
        }

        public void ResetForNewHand()
        {
            HoleCards.Clear();
            BestHand = null;
            CurrentBetInRound = 0;
            IsFolded = false;
            IsAllIn = false;
            // Note: We do NOT reset 'Stack', because that money carries over!
        }

        // Logic for taking money from the stack and putting it into a bet.
        public int PlaceBet(int amount)
        {
            int amountToBet = amount;

            if (amountToBet >= Stack)
            {
                amountToBet = Stack;
                IsAllIn = true;
            }

            Stack -= amountToBet;
            CurrentBetInRound += amountToBet;

            return amountToBet;
        }
    }
}