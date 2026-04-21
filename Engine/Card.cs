namespace FinalProject331406710.Engine
{
    public class Card
    {
        public Suit Suit { get; private set; }
        public Rank Rank { get; private set; }

        public Card(Suit s, Rank r)
        {
            Suit = s;
            Rank = r;
        }

        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }
    }
}