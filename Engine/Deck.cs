using System;
using System.Collections.Generic;
using System.Linq;

namespace FinalProject331406710.Engine
{
    public class Deck
    {
        private Stack<Card> _cards;

        public Deck()
        {
            Shuffle();
        }

        public void Shuffle()
        {
            var cardList = new List<Card>();

            foreach (Suit s in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank r in Enum.GetValues(typeof(Rank)))
                {
                    cardList.Add(new Card(s, r));
                }
            }

            var rnd = new Random();
            var shuffledList = cardList.OrderBy(c => rnd.Next()).ToList();

            _cards = new Stack<Card>();
            foreach (var card in shuffledList)
            {
                _cards.Push(card);
            }
        }

        public Card Deal()
        {
            if (_cards.Count > 0)
                return _cards.Pop();
            else
                return null;
        }
    }
}