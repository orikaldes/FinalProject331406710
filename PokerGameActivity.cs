using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using FinalProject331406710.Engine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameManager = FinalProject331406710.Engine.GameManager;
using Android.Content;
using System.Threading.Tasks;

namespace FinalProject331406710
{
    [Activity(Label = "Poker Table", Theme = "@style/AppTheme")]
    public class PokerGameActivity : AppCompatActivity
    {
        Button _foldButton, _checkCallButton, _raiseButton;
        TextView _foldButtonText, _checkCallButtonText, _raiseButtonText;
        TextView _resultsText, _potText, _turnIndicatorText;

        ImageView _b1, _b2, _b3, _b4, _b5;
        ImageView _pc1, _pc2;
        ImageView _p1c1, _p1c2;
        ImageView _p2c1, _p2c2;

        ImageView _p1TurnDot, _p2TurnDot, _playerTurnDot;
        LinearLayout _p1CardContainer, _p2CardContainer, _playerCardContainer;

        TextView _playerStackText, _playerBetText;
        TextView _p1StackText, _p1BetText;
        TextView _p2StackText, _p2BetText;

        private GameManager _gameManager;
        private string _myId;
        private bool _statsRecordedThisHand = false;

        private bool _isWaitingForBot = false;
        private bool _skipBotDelay = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.PokerGameLayout);

            var prefs = GetSharedPreferences("user_prefs", FileCreationMode.Private);
            _myId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", "");

            var toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Texas Hold'em";

            FindAllViews();

            _foldButton.Click += OnFoldClicked;
            _checkCallButton.Click += OnCheckCallClicked;
            _raiseButton.Click += OnRaiseClicked;

            ImageView playerAvatar = FindViewById<ImageView>(Resource.Id.playerAvatar);
            var db = Helper.Getdbcommand(this);
            var me = db.Table<Users>().Where(u => u.Id == _myId).FirstOrDefault();
            if (me != null && !string.IsNullOrEmpty(me.ProfileImagePath))
            {
                var bitmap = Android.Graphics.BitmapFactory.DecodeFile(me.ProfileImagePath);
                playerAvatar.SetImageBitmap(bitmap);
            }

            int startingStack = Intent.GetIntExtra("GAME_STACK", 1000);

            _gameManager = new GameManager();
            _gameManager.StartGame(startingStack);
            _statsRecordedThisHand = false;
            UpdateAllUI();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.game_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.action_leave_table)
            {
                ConfirmCashOut();
                return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void ConfirmCashOut()
        {
            string message = "Your current stack will be returned to your wallet.";
            Player player = _gameManager.Players[0];

            if (player.CurrentBetInRound > 0 && _gameManager.CurrentStage != GameStage.Showdown)
            {
                message += "\n\nWARNING: You have an active bet! Leaving now will FOLD your hand and you will forfeit the chips currently in the pot.";
            }

            new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle("Leave Table?")
                .SetMessage(message)
                .SetPositiveButton("Leave", (sender, args) =>
                {
                    player.IsFolded = true;
                    int cashOutAmount = player.Stack;
                    UserWallet.Balance += cashOutAmount;
                    Finish();
                })
                .SetNegativeButton("Stay", (sender, args) => { })
                .Show();
        }

        [System.Obsolete]
        public override void OnBackPressed()
        {
            ConfirmCashOut();
        }

        private void OnFoldClicked(object sender, System.EventArgs e)
        {
            if (_isWaitingForBot) { _skipBotDelay = true; return; }

            if (_gameManager.CurrentStage == GameStage.Showdown)
            {
                if (_gameManager.Players[0].Stack <= 0)
                {
                    Toast.MakeText(this, "Game Over! You ran out of chips.", ToastLength.Long).Show();
                    Finish();
                    return;
                }

                _gameManager.AwardPotAndStartNewHand();
                _statsRecordedThisHand = false;
                UpdateAllUI();
                return;
            }
            _gameManager.ProcessPlayerAction(PlayerAction.Fold);
            UpdateAllUI();
        }

        private void OnCheckCallClicked(object sender, System.EventArgs e)
        {
            if (_isWaitingForBot) { _skipBotDelay = true; return; }

            if (_gameManager.CurrentStage == GameStage.Showdown)
            {
                if (_gameManager.Players[0].Stack <= 0)
                {
                    Toast.MakeText(this, "Game Over! You ran out of chips.", ToastLength.Long).Show();
                    Finish();
                    return;
                }

                _gameManager.AwardPotAndStartNewHand();
                _statsRecordedThisHand = false;
                UpdateAllUI();
                return;
            }

            Player player = _gameManager.CurrentPlayer;
            int amountToCall = _gameManager.CurrentBetToCall - player.CurrentBetInRound;

            if (amountToCall == 0)
                _gameManager.ProcessPlayerAction(PlayerAction.Check);
            else
                _gameManager.ProcessPlayerAction(PlayerAction.Call);

            UpdateAllUI();
        }

        private void OnRaiseClicked(object sender, System.EventArgs e)
        {
            if (_isWaitingForBot) { _skipBotDelay = true; return; }

            if (_gameManager.CurrentStage == GameStage.Showdown)
            {
                if (_gameManager.Players[0].Stack <= 0)
                {
                    Toast.MakeText(this, "Game Over! You ran out of chips.", ToastLength.Long).Show();
                    Finish();
                    return;
                }

                _gameManager.AwardPotAndStartNewHand();
                _statsRecordedThisHand = false;
                UpdateAllUI();
                return;
            }

            Player player = _gameManager.CurrentPlayer;
            View dialogView = LayoutInflater.Inflate(Resource.Layout.raise_dialog, null);
            var raiseSlider = dialogView.FindViewById<SeekBar>(Resource.Id.raiseSlider);
            var raiseAmountText = dialogView.FindViewById<EditText>(Resource.Id.raiseAmountText);
            var allInButton = dialogView.FindViewById<Button>(Resource.Id.allInButton);

            int minBet = (_gameManager.CurrentBetToCall == 0) ? 10 : _gameManager.CurrentBetToCall * 2;
            int maxBet = player.Stack + player.CurrentBetInRound;
            if (minBet > maxBet) minBet = maxBet;

            raiseSlider.Max = maxBet - minBet;
            raiseSlider.Progress = 0;
            raiseAmountText.Text = minBet.ToString();

            raiseSlider.ProgressChanged += (s, args) =>
            {
                int newAmount = minBet + args.Progress;
                raiseAmountText.Text = newAmount.ToString();
            };

            allInButton.Click += (s, a) =>
            {
                raiseAmountText.Text = maxBet.ToString();
                raiseSlider.Progress = raiseSlider.Max;
            };

            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            builder.SetTitle("Select Raise Amount");
            builder.SetView(dialogView);
            builder.SetPositiveButton("Raise", (dialog, which) =>
            {
                if (int.TryParse(raiseAmountText.Text, out int finalRaiseAmount))
                {
                    if (finalRaiseAmount < minBet) finalRaiseAmount = minBet;
                    if (finalRaiseAmount > maxBet) finalRaiseAmount = maxBet;

                    _gameManager.ProcessPlayerAction(PlayerAction.Raise, finalRaiseAmount);
                    UpdateAllUI();
                }
            });
            builder.SetNegativeButton("Cancel", (dialog, which) => { });
            builder.Show();
        }

        private void UpdateTurnIndicator()
        {
            _playerTurnDot.Visibility = ViewStates.Invisible;
            _p1TurnDot.Visibility = ViewStates.Invisible;
            _p2TurnDot.Visibility = ViewStates.Invisible;

            _playerTurnDot.ClearAnimation();
            _p1TurnDot.ClearAnimation();
            _p2TurnDot.ClearAnimation();

            if (_gameManager.CurrentStage == GameStage.Showdown) return;

            ImageView activeDot = null;
            if (_gameManager.CurrentPlayer.Name == "Player") activeDot = _playerTurnDot;
            else if (_gameManager.CurrentPlayer.Name == "P1") activeDot = _p1TurnDot;
            else if (_gameManager.CurrentPlayer.Name == "P2") activeDot = _p2TurnDot;

            if (activeDot != null)
            {
                activeDot.Visibility = ViewStates.Visible;
                var anim = new Android.Views.Animations.AlphaAnimation(1.0f, 0.1f)
                {
                    Duration = 600,
                    RepeatMode = Android.Views.Animations.RepeatMode.Reverse,
                    RepeatCount = Android.Views.Animations.Animation.Infinite
                };
                activeDot.StartAnimation(anim);
            }
        }

        private void UpdateAllUI()
        {
            _potText.Text = $"Pot: {_gameManager.Pot}";

            string dealerName = _gameManager.Players[_gameManager.DealerIndex].Name;
            _turnIndicatorText.Text = $"{_gameManager.CurrentPlayer.Name}'s Turn (Dealer: {dealerName})";

            _playerCardContainer.SetBackgroundResource(0);
            _p1CardContainer.SetBackgroundResource(0);
            _p2CardContainer.SetBackgroundResource(0);

            UpdateTurnIndicator();

            Player player = _gameManager.Players[0];
            _playerStackText.Text = $"Stack: {player.Stack}";
            _playerBetText.Text = $"Bet: {player.CurrentBetInRound}";
            SetCardFaceUp(_pc1, player.HoleCards[0]);
            SetCardFaceUp(_pc2, player.HoleCards[1]);
            _pc1.Alpha = _pc2.Alpha = player.IsFolded ? 0.5f : 1.0f;

            Player p1 = _gameManager.Players[1];
            _p1StackText.Text = $"Stack: {p1.Stack}";
            _p1BetText.Text = $"Bet: {p1.CurrentBetInRound}";
            SetCardFaceUp(_p1c1, p1.HoleCards[0]);
            SetCardFaceUp(_p1c2, p1.HoleCards[1]);
            _p1c1.Alpha = _p1c2.Alpha = p1.IsFolded ? 0.5f : 1.0f;

            Player p2 = _gameManager.Players[2];
            _p2StackText.Text = $"Stack: {p2.Stack}";
            _p2BetText.Text = $"Bet: {p2.CurrentBetInRound}";
            SetCardFaceUp(_p2c1, p2.HoleCards[0]);
            SetCardFaceUp(_p2c2, p2.HoleCards[1]);
            _p2c1.Alpha = _p2c2.Alpha = p2.IsFolded ? 0.5f : 1.0f;

            SetCardFaceDown(_b1); SetCardFaceDown(_b2); SetCardFaceDown(_b3);
            SetCardFaceDown(_b4); SetCardFaceDown(_b5);

            if (_gameManager.CurrentStage == GameStage.Showdown)
            {
                SetCardFaceUp(_b1, _gameManager.Board.Count > 0 ? _gameManager.Board[0] : null);
                SetCardFaceUp(_b2, _gameManager.Board.Count > 1 ? _gameManager.Board[1] : null);
                SetCardFaceUp(_b3, _gameManager.Board.Count > 2 ? _gameManager.Board[2] : null);
                SetCardFaceUp(_b4, _gameManager.Board.Count > 3 ? _gameManager.Board[3] : null);
                SetCardFaceUp(_b5, _gameManager.Board.Count > 4 ? _gameManager.Board[4] : null);

                _resultsText.Text = RunShowdown();
                _turnIndicatorText.Text = "Hand Over. Click any button for new hand.";
            }
            else
            {
                if (_gameManager.CurrentStage >= GameStage.Flop && _gameManager.Board.Count >= 3)
                {
                    SetCardFaceUp(_b1, _gameManager.Board[0]);
                    SetCardFaceUp(_b2, _gameManager.Board[1]);
                    SetCardFaceUp(_b3, _gameManager.Board[2]);
                }
                if (_gameManager.CurrentStage >= GameStage.Turn && _gameManager.Board.Count >= 4)
                {
                    SetCardFaceUp(_b4, _gameManager.Board[3]);
                }
                if (_gameManager.CurrentStage >= GameStage.River && _gameManager.Board.Count >= 5)
                {
                    SetCardFaceUp(_b5, _gameManager.Board[4]);
                }
                _resultsText.Text = _gameManager.LastAction;
            }

            Player currentPlayer = _gameManager.CurrentPlayer;
            int amountToCall = _gameManager.CurrentBetToCall - currentPlayer.CurrentBetInRound;

            if (_gameManager.CurrentStage == GameStage.Showdown)
            {
                _foldButtonText.Text = "Next";
                _checkCallButtonText.Text = "Next";
                _raiseButtonText.Text = "Next";

                _foldButton.Enabled = true;
                _checkCallButton.Enabled = true;
                _raiseButton.Enabled = true;

                _isWaitingForBot = false;
            }
            else
            {
                bool isHumanTurn = (currentPlayer.Name == "Player");

                if (isHumanTurn)
                {
                    _foldButtonText.Text = "Fold";
                    _raiseButtonText.Text = "Raise";
                    if (amountToCall == 0)
                        _checkCallButtonText.Text = "Check";
                    else
                        _checkCallButtonText.Text = $"Call ({amountToCall})";

                    _foldButton.Enabled = true;
                    _checkCallButton.Enabled = true;
                    _raiseButton.Enabled = true;
                }
                else
                {
                    _foldButtonText.Text = "Skip >>";
                    _checkCallButtonText.Text = "Skip >>";
                    _raiseButtonText.Text = "Skip >>";

                    _foldButton.Enabled = true;
                    _checkCallButton.Enabled = true;
                    _raiseButton.Enabled = true;

                    if (!_isWaitingForBot)
                    {
                        ExecuteBotTurnAsync();
                    }
                }
            }
        }

        private async void ExecuteBotTurnAsync()
        {
            _isWaitingForBot = true;
            _skipBotDelay = false;

            // Wait up to 20 seconds
            for (int i = 0; i < 200; i++)
            {
                if (_skipBotDelay) break;
                await Task.Delay(100);
            }

            // --- FIX: Safely route back to the Main UI Thread! ---
            RunOnUiThread(() =>
            {
                // Safety check: Don't crash if the user left the table while the bot was thinking
                if (IsFinishing || IsDestroyed) return;

                _isWaitingForBot = false;

                if (_gameManager.CurrentStage == GameStage.Showdown || _gameManager.CurrentPlayer.Name == "Player")
                    return;

                Player bot = _gameManager.CurrentPlayer;
                int amountToCall = _gameManager.CurrentBetToCall - bot.CurrentBetInRound;

                if (amountToCall == 0)
                {
                    if (new System.Random().Next(100) < 15 && bot.Stack >= 50)
                        _gameManager.ProcessPlayerAction(PlayerAction.Raise, bot.CurrentBetInRound + 50);
                    else
                        _gameManager.ProcessPlayerAction(PlayerAction.Check);
                }
                else
                {
                    if (amountToCall > bot.Stack / 3)
                        _gameManager.ProcessPlayerAction(PlayerAction.Fold);
                    else
                        _gameManager.ProcessPlayerAction(PlayerAction.Call);
                }

                UpdateAllUI();
            });
        }

        private string RunShowdown()
        {
            var hands = new List<(Player Player, HandRank Hand)>();
            foreach (var p in _gameManager.Players.Where(p => !p.IsFolded))
            {
                var allCards = new List<Card>(p.HoleCards);
                allCards.AddRange(_gameManager.Board);

                if (allCards.Count >= 5)
                {
                    p.BestHand = _gameManager.Evaluator.Evaluate(allCards);
                }
                else
                {
                    p.BestHand = new HandRank { HandType = HandType.HighCard };
                }

                hands.Add((p, p.BestHand));
            }

            if (hands.Count == 0) return "No winners.";
            var winningEntry = hands.OrderByDescending(h => h.Hand).First();

            foreach (var p in _gameManager.Players)
            {
                bool isWinner = (!p.IsFolded && p == winningEntry.Player);

                if (p.Name == "Player")
                {
                    if (isWinner) _playerCardContainer.SetBackgroundResource(Resource.Drawable.winner_glow);
                    else { SetCardFaceDown(_pc1); SetCardFaceDown(_pc2); _pc1.Alpha = 1.0f; _pc2.Alpha = 1.0f; }
                }
                else if (p.Name == "P1")
                {
                    if (isWinner) _p1CardContainer.SetBackgroundResource(Resource.Drawable.winner_glow);
                    else { SetCardFaceDown(_p1c1); SetCardFaceDown(_p1c2); _p1c1.Alpha = 1.0f; _p1c2.Alpha = 1.0f; }
                }
                else if (p.Name == "P2")
                {
                    if (isWinner) _p2CardContainer.SetBackgroundResource(Resource.Drawable.winner_glow);
                    else { SetCardFaceDown(_p2c1); SetCardFaceDown(_p2c2); _p2c1.Alpha = 1.0f; _p2c2.Alpha = 1.0f; }
                }
            }

            if (!_statsRecordedThisHand && winningEntry.Player.Name == "Player")
            {
                string winType = _gameManager.Board.Count == 5 ? winningEntry.Hand.HandType.ToString() : "Opponent Folded";
                Helper.RecordHandWin(this, _myId, winType, _gameManager.Pot);
                _statsRecordedThisHand = true;
            }

            var results = new StringBuilder();
            results.AppendLine($"--- {winningEntry.Player.Name} WINS! ---");
            foreach (var entry in hands)
            {
                if (_gameManager.Board.Count == 5)
                    results.AppendLine($"{entry.Player.Name}: {entry.Hand}");
                else
                    results.AppendLine($"{entry.Player.Name}: Won by Default");
            }

            return results.ToString();
        }

        private void FindAllViews()
        {
            _foldButton = FindViewById<Button>(Resource.Id.foldButton);
            _checkCallButton = FindViewById<Button>(Resource.Id.checkCallButton);
            _raiseButton = FindViewById<Button>(Resource.Id.raiseButton);

            _foldButtonText = FindViewById<TextView>(Resource.Id.foldButtonText);
            _checkCallButtonText = FindViewById<TextView>(Resource.Id.checkCallButtonText);
            _raiseButtonText = FindViewById<TextView>(Resource.Id.raiseButtonText);

            _resultsText = FindViewById<TextView>(Resource.Id.resultsText);
            _potText = FindViewById<TextView>(Resource.Id.potText);
            _turnIndicatorText = FindViewById<TextView>(Resource.Id.turnIndicatorText);

            _b1 = FindViewById<ImageView>(Resource.Id.boardCard1);
            _b2 = FindViewById<ImageView>(Resource.Id.boardCard2);
            _b3 = FindViewById<ImageView>(Resource.Id.boardCard3);
            _b4 = FindViewById<ImageView>(Resource.Id.boardCard4);
            _b5 = FindViewById<ImageView>(Resource.Id.boardCard5);
            _pc1 = FindViewById<ImageView>(Resource.Id.playerCard1);
            _pc2 = FindViewById<ImageView>(Resource.Id.playerCard2);
            _p1c1 = FindViewById<ImageView>(Resource.Id.p1Card1);
            _p1c2 = FindViewById<ImageView>(Resource.Id.p1Card2);
            _p2c1 = FindViewById<ImageView>(Resource.Id.p2Card1);
            _p2c2 = FindViewById<ImageView>(Resource.Id.p2Card2);

            _p1TurnDot = FindViewById<ImageView>(Resource.Id.p1TurnDot);
            _p2TurnDot = FindViewById<ImageView>(Resource.Id.p2TurnDot);
            _playerTurnDot = FindViewById<ImageView>(Resource.Id.playerTurnDot);

            _p1CardContainer = FindViewById<LinearLayout>(Resource.Id.p1CardContainer);
            _p2CardContainer = FindViewById<LinearLayout>(Resource.Id.p2CardContainer);
            _playerCardContainer = FindViewById<LinearLayout>(Resource.Id.playerCardContainer);

            _playerStackText = FindViewById<TextView>(Resource.Id.playerStackText);
            _playerBetText = FindViewById<TextView>(Resource.Id.playerBetText);
            _p1StackText = FindViewById<TextView>(Resource.Id.p1StackText);
            _p1BetText = FindViewById<TextView>(Resource.Id.p1BetText);
            _p2StackText = FindViewById<TextView>(Resource.Id.p2StackText);
            _p2BetText = FindViewById<TextView>(Resource.Id.p2BetText);
        }

        private void SetCardFaceDown(ImageView cardSlot)
        {
            cardSlot.SetImageResource(Resource.Drawable.card_back);
        }

        private void SetCardFaceUp(ImageView cardSlot, Card card)
        {
            if (card == null)
            {
                SetCardFaceDown(cardSlot);
                return;
            }

            string imageName = GetImageName(card);
            int resourceId = Resources.GetIdentifier(imageName, "drawable", PackageName);

            if (resourceId != 0)
                cardSlot.SetImageResource(resourceId);
            else
                cardSlot.SetImageResource(Resource.Drawable.card_back);
        }

        private string GetImageName(Card card)
        {
            string suit = card.Suit.ToString().ToLower();
            string rank;
            switch (card.Rank)
            {
                case Rank.Ace: rank = "ace"; break;
                case Rank.King: rank = "king"; break;
                case Rank.Queen: rank = "queen"; break;
                case Rank.Jack: rank = "jack"; break;
                default: rank = ((int)card.Rank).ToString("D2"); break;
            }
            return $"{suit}_{rank}";
        }
    }
}