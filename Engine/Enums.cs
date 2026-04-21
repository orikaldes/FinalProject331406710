namespace FinalProject331406710.Engine
{
    // This list defines the 10 Poker Hand Ranks.
    public enum HandType
    {
        RoyalFlush = 10,
        StraightFlush = 9,
        FourOfAKind = 8,
        FullHouse = 7,
        Flush = 6,
        Straight = 5,
        ThreeOfAKind = 4,
        TwoPair = 3,
        Pair = 2,
        HighCard = 1
    }

    // This list defines the 4 Suits.
    public enum Suit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }

    // This list defines the 13 Card Ranks.
    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    // This tracks what "Stage" the game is currently in.
    public enum GameStage
    {
        PreDeal,        // Before the game starts
        PreFlopBetting, // Cards dealt to players, but no board cards yet
        Flop,           // Showing the first 3 community cards
        FlopBetting,    // Betting round after Flop
        Turn,           // Showing the 4th card
        TurnBetting,    // Betting round after Turn
        River,          // Showing the 5th card
        RiverBetting,   // Final betting round
        Showdown        // The hand is over, show cards to determine winner
    }

    // This list defines the moves a player can make in the game.
    public enum PlayerAction
    {
        Fold,   // Give up
        Check,  // Pass (bet 0)
        Call,   // Match the current bet
        Raise   // Increase the bet
    }
}