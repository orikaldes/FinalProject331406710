using System.Collections.Generic;
using System.Linq;

namespace FinalProject331406710.Engine
{
    public class GameManager
    {
        // --- State Variables ---
        public List<Player> Players { get; set; }
        public List<Card> Board { get; set; }
        public int Pot { get; set; }
        public int CurrentBetToCall { get; set; }
        public int TurnIndex { get; set; }
        public GameStage CurrentStage { get; set; }
        public string LastAction { get; set; }

        // --- NEW: Tracks who the Dealer is ---
        public int DealerIndex { get; set; }

        public Deck Deck { get; set; }
        public HandEvaluator Evaluator { get; set; }

        private int _playerWhoLastRaisedIndex;

        public GameManager()
        {
            Deck = new Deck();
            Evaluator = new HandEvaluator();
            Players = new List<Player>();
            Board = new List<Card>();
        }

        public Player CurrentPlayer
        {
            get { return Players[TurnIndex]; }
        }

        public void StartGame(int humanStackSize)
        {
            Players.Clear();
            Players.Add(new Player("Player", humanStackSize));
            Players.Add(new Player("P1", 5000));
            Players.Add(new Player("P2", 5000));

            // Randomly pick who starts as the Dealer for the very first hand
            DealerIndex = new System.Random().Next(Players.Count);

            StartNewHand();
        }

        public void StartNewHand()
        {
            Deck.Shuffle();
            Board.Clear();
            Pot = 0;
            CurrentBetToCall = 0;
            CurrentStage = GameStage.PreFlopBetting;

            // The player to the left of the dealer starts
            int nextToAct = (DealerIndex + 1) % Players.Count;
            TurnIndex = nextToAct;
            _playerWhoLastRaisedIndex = nextToAct;

            foreach (var player in Players)
            {
                player.ResetForNewHand();

                // --- NEW: Bot Auto-Rebuy to keep the game alive ---
                if (player.Stack <= 0 && player.Name != "Player")
                {
                    player.Stack = 1000;
                }

                player.HoleCards.Add(Deck.Deal());
                player.HoleCards.Add(Deck.Deal());
            }

            for (int i = 0; i < 5; i++)
            {
                Board.Add(Deck.Deal());
            }

            LastAction = $"New Hand! {CurrentPlayer.Name} acts first.";
        }

        public void ProcessPlayerAction(PlayerAction action, int raiseAmount = 0)
        {
            if (CurrentStage == GameStage.Showdown) return;
            Player player = CurrentPlayer;

            switch (action)
            {
                case PlayerAction.Fold:
                    player.IsFolded = true;
                    LastAction = $"{player.Name} Folds.";
                    break;

                case PlayerAction.Check:
                    LastAction = $"{player.Name} Checks.";
                    break;

                case PlayerAction.Call:
                    int amountToCall = CurrentBetToCall - player.CurrentBetInRound;
                    int actualBet = player.PlaceBet(amountToCall);
                    Pot += actualBet;
                    LastAction = $"{player.Name} Calls {actualBet}.";
                    break;

                case PlayerAction.Raise:
                    int amountToPutIn = raiseAmount - player.CurrentBetInRound;
                    int actualRaise = player.PlaceBet(amountToPutIn);
                    Pot += actualRaise;
                    CurrentBetToCall = raiseAmount;
                    _playerWhoLastRaisedIndex = TurnIndex;
                    LastAction = $"{player.Name} Raises to {raiseAmount}.";
                    break;
            }

            AdvanceTurn();
        }

        private void AdvanceTurn()
        {
            if (CurrentStage == GameStage.Showdown) return;

            if (Players.Count(p => !p.IsFolded) <= 1)
            {
                CurrentStage = GameStage.Showdown;
                return;
            }

            int activePlayersCount = Players.Count(p => !p.IsFolded && !p.IsAllIn);
            if (activePlayersCount == 0)
            {
                RunToLink();
                return;
            }

            // Find the next active player
            do
            {
                TurnIndex = (TurnIndex + 1) % Players.Count;
            }
            while (Players[TurnIndex].IsFolded || Players[TurnIndex].IsAllIn);

            // If we made a full circle back to the last raiser, the round is over
            if (TurnIndex == _playerWhoLastRaisedIndex)
            {
                if (Players[TurnIndex].CurrentBetInRound >= CurrentBetToCall || Players[TurnIndex].IsAllIn)
                {
                    AdvanceGameStage();
                }
            }
        }

        private void RunToLink()
        {
            while (CurrentStage < GameStage.Showdown)
            {
                CurrentStage++;
            }
            LastAction = "All players All-In! Showdown.";
        }

        private void AdvanceGameStage()
        {
            CurrentStage++;

            int activePlayers = Players.Count(p => !p.IsFolded && !p.IsAllIn);
            if (activePlayers < 2 && CurrentStage < GameStage.Showdown)
            {
                AdvanceGameStage();
                return;
            }

            switch (CurrentStage)
            {
                case GameStage.Flop:
                    LastAction = "--- Dealing the Flop ---";
                    CurrentStage = GameStage.FlopBetting;
                    ResetBettingRound();
                    break;
                case GameStage.Turn:
                    LastAction = "--- Dealing the Turn ---";
                    CurrentStage = GameStage.TurnBetting;
                    ResetBettingRound();
                    break;
                case GameStage.River:
                    LastAction = "--- Dealing the River ---";
                    CurrentStage = GameStage.RiverBetting;
                    ResetBettingRound();
                    break;
                case GameStage.Showdown:
                    LastAction = "--- Showdown ---";
                    break;
            }
        }

        private void ResetBettingRound()
        {
            CurrentBetToCall = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].CurrentBetInRound = 0;
            }

            // Action always starts with the player left of the dealer on Flop/Turn/River
            int nextToAct = (DealerIndex + 1) % Players.Count;

            while (Players[nextToAct].IsFolded || Players[nextToAct].IsAllIn)
            {
                nextToAct = (nextToAct + 1) % Players.Count;
            }

            TurnIndex = nextToAct;
            _playerWhoLastRaisedIndex = nextToAct;
        }

        public void AwardPotAndStartNewHand()
        {
            var hands = new List<(Player Player, HandRank Hand)>();
            foreach (var p in Players.Where(p => !p.IsFolded))
            {
                var allCards = new List<Card>(p.HoleCards);
                allCards.AddRange(Board);
                p.BestHand = Evaluator.Evaluate(allCards);
                hands.Add((p, p.BestHand));
            }

            if (hands.Any())
            {
                var winningEntry = hands.OrderByDescending(h => h.Hand).First();
                winningEntry.Player.Stack += Pot;
            }

            // Move the Dealer Button to the next player!
            DealerIndex = (DealerIndex + 1) % Players.Count;

            StartNewHand();
        }
    }
}