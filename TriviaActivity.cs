using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using FinalProject331406710.Engine;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;

namespace FinalProject331406710
{
    [Activity(Label = "Trivia", Theme = "@style/AppTheme")]
    public class TriviaActivity : AppCompatActivity
    {
        TextView _feedback;
        ImageView _b1, _b2, _b3, _b4, _b5;
        ImageView _a1, _a2, _b_c1, _b_c2;
        Button _btnA, _btnB, _btnSplit, _btnHint, _btnNext;

        Deck _deck;
        HandEvaluator _evaluator;
        int _correctAnswer;
        int _currentPrize = 100;

        List<Card> _handA_7Cards;
        List<Card> _handB_7Cards;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.trivia_layout);

            var toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Poker Trivia";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            _b1 = FindViewById<ImageView>(Resource.Id.trivBoard1);
            _b2 = FindViewById<ImageView>(Resource.Id.trivBoard2);
            _b3 = FindViewById<ImageView>(Resource.Id.trivBoard3);
            _b4 = FindViewById<ImageView>(Resource.Id.trivBoard4);
            _b5 = FindViewById<ImageView>(Resource.Id.trivBoard5);

            _a1 = FindViewById<ImageView>(Resource.Id.trivHandA1);
            _a2 = FindViewById<ImageView>(Resource.Id.trivHandA2);
            _b_c1 = FindViewById<ImageView>(Resource.Id.trivHandB1);
            _b_c2 = FindViewById<ImageView>(Resource.Id.trivHandB2);

            _btnA = FindViewById<Button>(Resource.Id.btnGuessA);
            _btnB = FindViewById<Button>(Resource.Id.btnGuessB);
            _btnSplit = FindViewById<Button>(Resource.Id.btnGuessSplit);
            _btnHint = FindViewById<Button>(Resource.Id.btnHint);
            _btnNext = FindViewById<Button>(Resource.Id.btnNextQuestion);
            _feedback = FindViewById<TextView>(Resource.Id.triviaFeedback);

            _deck = new Deck();
            _evaluator = new HandEvaluator();

            _btnA.Click += (s, e) => CheckAnswer(1);
            _btnB.Click += (s, e) => CheckAnswer(2);
            _btnSplit.Click += (s, e) => CheckAnswer(0);
            _btnHint.Click += (s, e) => ShowHint();
            _btnNext.Click += (s, e) => DealNewScenario();

            DealNewScenario();
        }

        private void DealNewScenario()
        {
            _deck.Shuffle();
            _feedback.Text = "";
            _btnNext.Visibility = ViewStates.Invisible;
            _btnHint.Enabled = true;
            EnableButtons(true);
            _currentPrize = 100;

            var board = new List<Card> { _deck.Deal(), _deck.Deal(), _deck.Deal(), _deck.Deal(), _deck.Deal() };
            var handA = new List<Card> { _deck.Deal(), _deck.Deal() };
            var handB = new List<Card> { _deck.Deal(), _deck.Deal() };

            _handA_7Cards = handA.Concat(board).ToList();
            _handB_7Cards = handB.Concat(board).ToList();

            SetCardImage(_b1, board[0]);
            SetCardImage(_b2, board[1]);
            SetCardImage(_b3, board[2]);
            SetCardImage(_b4, board[3]); SetCardImage(_b5, board[4]);
            SetCardImage(_a1, handA[0]); SetCardImage(_a2, handA[1]);
            SetCardImage(_b_c1, handB[0]); SetCardImage(_b_c2, handB[1]);

            var rankA = _evaluator.Evaluate(_handA_7Cards);
            var rankB = _evaluator.Evaluate(_handB_7Cards);

            int comparison = rankA.CompareTo(rankB);
            if (comparison > 0) _correctAnswer = 1;
            else if (comparison < 0) _correctAnswer = 2;
            else _correctAnswer = 0;
        }

        private void ShowHint()
        {
            _currentPrize = 0;

            var rankA = _evaluator.Evaluate(_handA_7Cards);
            var rankB = _evaluator.Evaluate(_handB_7Cards);

            string winnerText = "";
            if (_correctAnswer == 1) winnerText = "Hand A is stronger.";
            else if (_correctAnswer == 2) winnerText = "Hand B is stronger.";
            else winnerText = "It is a tie.";

            string message = $"Hand A has: {rankA.HandType}\n" +
                             $"Hand B has: {rankB.HandType}\n\n" +
                             $"{winnerText}\n\n" +
                             "(Prize reduced to 0)";

            new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle("Hint")
                .SetMessage(message)
                .SetPositiveButton("OK", (s, e) => { })
                .Show();
        }

        private void CheckAnswer(int guess)
        {
            EnableButtons(false);
            _btnHint.Enabled = false;
            _btnNext.Visibility = ViewStates.Visible;

            if (guess == _correctAnswer)
            {
                string msg = $"Correct! +{_currentPrize} Coins";
                Toast.MakeText(this, msg, ToastLength.Short).Show();
                _feedback.Text = msg;
                _feedback.SetTextColor(Color.Green);
                UserWallet.Balance += _currentPrize;
            }
            else
            {
                string correctStr = _correctAnswer == 1 ? "Hand A" : (_correctAnswer == 2 ? "Hand B" : "Split");
                string msg = $"Wrong! Winner was {correctStr}";
                Toast.MakeText(this, msg, ToastLength.Short).Show();
                _feedback.Text = msg;
                _feedback.SetTextColor(Color.Red);
            }
        }

        private void EnableButtons(bool enable)
        {
            _btnA.Enabled = enable;
            _btnB.Enabled = enable;
            _btnSplit.Enabled = enable;
        }

        private void SetCardImage(ImageView imageView, Card card)
        {
            string imageName = GetImageName(card);
            int resourceId = Resources.GetIdentifier(imageName, "drawable", PackageName);

            if (resourceId != 0)
                imageView.SetImageResource(resourceId);
            else
                imageView.SetImageResource(Resource.Drawable.card_back);
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

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return true;
        }
    }
}