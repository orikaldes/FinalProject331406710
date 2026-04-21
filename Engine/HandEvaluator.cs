using System.Collections.Generic;
using System.Linq;

namespace FinalProject331406710.Engine
{
    public class HandEvaluator
    {
        private List<Card> _cards;
        private HandRank _bestHand;

        public HandRank Evaluate(List<Card> sevenCards)
        {
            // 1. Sort the cards from Highest (Ace) to Lowest (2).
            _cards = sevenCards.OrderByDescending(c => c.Rank).ToList();

            // 2. Initialize a blank result.
            _bestHand = new HandRank();

            // 3. The Checklist: Best possible -> Worst possible.
            if (CheckStraightFlush(true)) // Check specifically for Royal Flush 
                return _bestHand;
            if (CheckStraightFlush(false)) // Check for regular Straight Flush
                return _bestHand;
            if (CheckFourOfAKind())
                return _bestHand;
            if (CheckFullHouse())
                return _bestHand;
            if (CheckFlush())
                return _bestHand;
            if (CheckStraight())
                return _bestHand;
            if (CheckThreeOfAKind())
                return _bestHand;
            if (CheckTwoPair())
                return _bestHand;
            if (CheckPair())
                return _bestHand;

            // 4. If none of the above matched, it's just a High Card hand.
            CheckHighCard();
            return _bestHand;
        }

        private bool CheckStraightFlush(bool checkRoyal)
        {
            var flushGroups = _cards.GroupBy(c => c.Suit).Where(g => g.Count() >= 5);
            if (!flushGroups.Any()) return false;

            var flush = flushGroups.First().ToList();
            var straight = FindStraight(flush);

            if (straight == null) return false;

            if (checkRoyal)
            {
                if (straight.First() == Rank.Ace)
                {
                    _bestHand.HandType = HandType.RoyalFlush;
                    _bestHand.Kickers = straight;
                    return true;
                }
                return false;
            }
            else
            {
                _bestHand.HandType = HandType.StraightFlush;
                _bestHand.Kickers = straight;
                return true;
            }
        }

        private bool CheckFourOfAKind()
        {
            var four = _cards.GroupBy(c => c.Rank).Where(g => g.Count() == 4).FirstOrDefault();
            if (four == null) return false;

            _bestHand.HandType = HandType.FourOfAKind;
            _bestHand.Kickers.Add(four.Key);
            _bestHand.Kickers.Add(_cards.Where(c => c.Rank != four.Key).First().Rank);
            return true;
        }

        private bool CheckFullHouse()
        {
            var three = _cards.GroupBy(c => c.Rank).Where(g => g.Count() == 3).OrderByDescending(g => g.Key).FirstOrDefault();
            if (three == null) return false;

            var pair = _cards.GroupBy(c => c.Rank).Where(g => g.Count() >= 2 && g.Key != three.Key).OrderByDescending(g => g.Key).FirstOrDefault();
            if (pair == null) return false;

            _bestHand.HandType = HandType.FullHouse;
            _bestHand.Kickers.Add(three.Key);
            _bestHand.Kickers.Add(pair.Key);
            return true;
        }

        private bool CheckFlush()
        {
            var flushGroup = _cards.GroupBy(c => c.Suit).Where(g => g.Count() >= 5).FirstOrDefault();
            if (flushGroup == null) return false;

            _bestHand.HandType = HandType.Flush;
            _bestHand.Kickers = flushGroup.OrderByDescending(c => c.Rank).Select(c => c.Rank).Take(5).ToList();
            return true;
        }

        private bool CheckStraight()
        {
            var straight = FindStraight(_cards);
            if (straight == null) return false;

            _bestHand.HandType = HandType.Straight;
            _bestHand.Kickers = straight;
            return true;
        }

        private bool CheckThreeOfAKind()
        {
            var three = _cards.GroupBy(c => c.Rank).Where(g => g.Count() == 3).OrderByDescending(g => g.Key).FirstOrDefault();
            if (three == null) return false;

            _bestHand.HandType = HandType.ThreeOfAKind;
            _bestHand.Kickers.Add(three.Key);
            _bestHand.Kickers.AddRange(_cards.Where(c => c.Rank != three.Key).Select(c => c.Rank).Take(2));
            return true;
        }

        private bool CheckTwoPair()
        {
            var pairs = _cards.GroupBy(c => c.Rank).Where(g => g.Count() == 2).OrderByDescending(g => g.Key).ToList();
            if (pairs.Count < 2) return false;

            _bestHand.HandType = HandType.TwoPair;
            _bestHand.Kickers.Add(pairs[0].Key);
            _bestHand.Kickers.Add(pairs[1].Key);
            _bestHand.Kickers.Add(_cards.Where(c => c.Rank != pairs[0].Key && c.Rank != pairs[1].Key).First().Rank);
            return true;
        }

        private bool CheckPair()
        {
            var pair = _cards.GroupBy(c => c.Rank).Where(g => g.Count() == 2).OrderByDescending(g => g.Key).FirstOrDefault();
            if (pair == null) return false;

            _bestHand.HandType = HandType.Pair;
            _bestHand.Kickers.Add(pair.Key);
            _bestHand.Kickers.AddRange(_cards.Where(c => c.Rank != pair.Key).Select(c => c.Rank).Take(3));
            return true;
        }

        private void CheckHighCard()
        {
            _bestHand.HandType = HandType.HighCard;
            _bestHand.Kickers = _cards.Select(c => c.Rank).Take(5).ToList();
        }

        private List<Rank> FindStraight(List<Card> cards)
        {
            var uniqueRanks = cards.Select(c => c.Rank).Distinct().OrderByDescending(r => r).ToList();

            // Special Check: "The Wheel" (Ace, 2, 3, 4, 5).
            if (uniqueRanks.Contains(Rank.Ace) &&
                uniqueRanks.Contains(Rank.Two) &&
                uniqueRanks.Contains(Rank.Three) &&
                uniqueRanks.Contains(Rank.Four) &&
                uniqueRanks.Contains(Rank.Five))
            {
                return new List<Rank> { Rank.Five, Rank.Four, Rank.Three, Rank.Two, Rank.Ace };
            }

            // Normal Check: Look for 5 numbers in a row.
            for (int i = 0; i <= uniqueRanks.Count - 5; i++)
            {
                if ((int)uniqueRanks[i] - (int)uniqueRanks[i + 4] == 4)
                {
                    return uniqueRanks.Skip(i).Take(5).ToList();
                }
            }

            return null;
        }
    }
}