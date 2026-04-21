using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using FinalProject331406710.Engine;
using System.Collections.Generic;
using Android.Speech.Tts;

namespace FinalProject331406710
{
    // Implement TextToSpeech.IOnInitListener to handle the speech engine setup
    [Activity(Label = "Tutorial", Theme = "@style/AppTheme")]
    public class TutorialActivity : AppCompatActivity, TextToSpeech.IOnInitListener
    {
        private class TutorialStep
        {
            public string Title;
            public string Text;
            public List<List<Card>> Examples;
            public List<string> ExampleLabels;

            public TutorialStep(string title, string text)
            {
                Title = title;
                Text = text;
                Examples = new List<List<Card>>();
                ExampleLabels = new List<string>();
            }

            public void AddExample(string label, List<Card> cards)
            {
                ExampleLabels.Add(label);
                Examples.Add(cards);
            }
        }

        TextView _title, _body, _indicator;
        Button _btnPrev, _btnNext, _btnSpeak;
        LinearLayout _exampleContainer;

        List<TutorialStep> _steps;
        int _currentIndex = 0;

        // Text To Speech Engine
        TextToSpeech _tts;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.tutorial_layout);

            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.Title = "Poker School";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            _title = FindViewById<TextView>(Resource.Id.tutorialTitle);
            _body = FindViewById<TextView>(Resource.Id.tutorialText);
            _indicator = FindViewById<TextView>(Resource.Id.pageIndicator);
            _btnPrev = FindViewById<Button>(Resource.Id.btnPrev);
            _btnNext = FindViewById<Button>(Resource.Id.btnNext);
            _btnSpeak = FindViewById<Button>(Resource.Id.btnSpeak);
            _exampleContainer = FindViewById<LinearLayout>(Resource.Id.exampleContainer);

            // Initialize the Text-To-Speech engine
            _tts = new TextToSpeech(this, this);

            InitializeContent();
            UpdateUI();

            _btnPrev.Click += (s, e) => {
                if (_currentIndex > 0)
                {
                    _currentIndex--;
                    StopSpeaking();
                    UpdateUI();
                }
            };

            _btnNext.Click += (s, e) => {
                if (_currentIndex < _steps.Count - 1)
                {
                    _currentIndex++;
                    StopSpeaking();
                    UpdateUI();
                }
                else { Finish(); }
            };

            // Trigger Speech when button is clicked
            _btnSpeak.Click += (s, e) => {
                SpeakText(_steps[_currentIndex].Text);
            };
        }

        // --- TEXT TO SPEECH LOGIC ---
        public void OnInit(OperationResult status)
        {
            // Set language to English once the engine is ready
            if (status == OperationResult.Success)
            {
                _tts.SetLanguage(Java.Util.Locale.Us);
            }
        }

        private void SpeakText(string text)
        {
            if (_tts != null)
            {
                // QueueMode.Flush stops any current speech and immediately reads the new text
                _tts.Speak(text, QueueMode.Flush, null, null);
            }
        }

        private void StopSpeaking()
        {
            if (_tts != null && _tts.IsSpeaking)
            {
                _tts.Stop();
            }
        }

        protected override void OnDestroy()
        {
            // Clean up the TTS engine to prevent memory leaks when leaving the screen
            if (_tts != null)
            {
                _tts.Stop();
                _tts.Shutdown();
            }
            base.OnDestroy();
        }
        // ------------------------------

        private void InitializeContent()
        {
            _steps = new List<TutorialStep>();

            var step1 = new TutorialStep("The Rules",
                "Texas Hold'em is played with a standard 52-card deck.\n\n" +
                "1. You get 2 personal cards (Hole Cards).\n" +
                "2. 5 Community Cards are dealt in the middle (Flop, Turn, River).\n" +
                "3. You make the best 5-card hand using any combination of your cards and the board.\n\n" +
                "The following pages explain the hand rankings from Best to Worst.");
            _steps.Add(step1);

            var step2 = new TutorialStep("1. Royal Flush",
                "The best possible hand in poker.\n\n" +
                "It consists of the Ace, King, Queen, Jack, and Ten, all of the same suit.");
            step2.AddExample("Example (Spades)",
                new List<Card> {
                    new Card(Suit.Spades, Rank.Ten), new Card(Suit.Spades, Rank.Jack),
                    new Card(Suit.Spades, Rank.Queen), new Card(Suit.Spades, Rank.King),
                    new Card(Suit.Spades, Rank.Ace)
                });
            _steps.Add(step2);

            var step3 = new TutorialStep("2. Straight Flush",
                "Five cards in a sequence, all in the same suit.\n\n" +
                "If two players have a straight flush, the one with the higher top card wins.");
            step3.AddExample("Example (Hearts)",
                new List<Card> {
                    new Card(Suit.Hearts, Rank.Five), new Card(Suit.Hearts, Rank.Six),
                    new Card(Suit.Hearts, Rank.Seven), new Card(Suit.Hearts, Rank.Eight),
                    new Card(Suit.Hearts, Rank.Nine)
                });
            _steps.Add(step3);

            var step4 = new TutorialStep("3. Four of a Kind",
                "Four cards of the same rank.\n\n" +
                "The fifth card (the kicker) does not matter for the hand type, but can break ties.");
            step4.AddExample("Four Jacks",
                new List<Card> {
                    new Card(Suit.Clubs, Rank.Jack), new Card(Suit.Diamonds, Rank.Jack),
                    new Card(Suit.Hearts, Rank.Jack), new Card(Suit.Spades, Rank.Jack),
                    null
                });
            _steps.Add(step4);

            var step5 = new TutorialStep("4. Full House",
                "Three cards of one rank and two cards of another rank.\n\n" +
                "Ranked first by the triplet, then by the pair.");
            step5.AddExample("Kings Full of Twos",
                new List<Card> {
                    new Card(Suit.Clubs, Rank.King), new Card(Suit.Diamonds, Rank.King), new Card(Suit.Spades, Rank.King),
                    new Card(Suit.Hearts, Rank.Two), new Card(Suit.Spades, Rank.Two)
                });
            _steps.Add(step5);

            var step6 = new TutorialStep("5. Flush",
                "Any five cards of the same suit, but not in a sequence.\n\n" +
                "The highest card determines the strength of the flush.");
            step6.AddExample("Ace High Flush",
                new List<Card> {
                    new Card(Suit.Clubs, Rank.Two), new Card(Suit.Clubs, Rank.Five),
                    new Card(Suit.Clubs, Rank.Nine), new Card(Suit.Clubs, Rank.Jack),
                    new Card(Suit.Clubs, Rank.Ace)
                });
            _steps.Add(step6);

            var step7 = new TutorialStep("6. Straight",
                "Five cards in a sequence, but not of the same suit.\n\n" +
                "An Ace can be high (10-J-Q-K-A) or low (A-2-3-4-5).");
            step7.AddExample("Six High Straight",
                new List<Card> {
                    new Card(Suit.Clubs, Rank.Two), new Card(Suit.Diamonds, Rank.Three),
                    new Card(Suit.Spades, Rank.Four), new Card(Suit.Hearts, Rank.Five),
                    new Card(Suit.Clubs, Rank.Six)
                });
            _steps.Add(step7);

            var step8 = new TutorialStep("7. Three of a Kind",
                "Three cards of the same rank.\n\n" +
                "Also known as 'Trips' or 'Set'.");
            step8.AddExample("Three Queens",
                new List<Card> {
                    new Card(Suit.Clubs, Rank.Queen), new Card(Suit.Diamonds, Rank.Queen), new Card(Suit.Spades, Rank.Queen),
                    null, null
                });
            _steps.Add(step8);

            var step9 = new TutorialStep("8. Two Pair",
                "Two different pairs.\n\n" +
                "If two players have two pair, the highest pair wins.");
            step9.AddExample("Eights and Fives",
                new List<Card> {
                    new Card(Suit.Clubs, Rank.Eight), new Card(Suit.Diamonds, Rank.Eight),
                    new Card(Suit.Spades, Rank.Five), new Card(Suit.Hearts, Rank.Five),
                    null
                });
            _steps.Add(step9);

            var step10 = new TutorialStep("9. One Pair",
                "Two cards of the same rank.\n\n" +
                "This is a very common winning hand.");
            step10.AddExample("Pair of Tens",
                new List<Card> {
                    new Card(Suit.Clubs, Rank.Ten), new Card(Suit.Diamonds, Rank.Ten),
                    null, null, null
                });
            _steps.Add(step10);

            var step11 = new TutorialStep("10. High Card",
                "When you haven't made any of the hands above, the highest card plays.");
            step11.AddExample("Ace High",
                new List<Card> {
                    new Card(Suit.Clubs, Rank.Ace),
                    null, null, null, null
                });
            _steps.Add(step11);
        }

        private void UpdateUI()
        {
            TutorialStep currentStep = _steps[_currentIndex];

            _title.Text = currentStep.Title;
            _body.Text = currentStep.Text;
            _indicator.Text = $"{_currentIndex + 1} / {_steps.Count}";

            _btnPrev.Enabled = _currentIndex > 0;
            _btnNext.Text = (_currentIndex == _steps.Count - 1) ? "Finish" : "Next >";

            _exampleContainer.RemoveAllViews();

            for (int i = 0; i < currentStep.Examples.Count; i++)
            {
                var label = new TextView(this);
                label.Text = currentStep.ExampleLabels[i];
                label.SetTextColor(Android.Graphics.Color.Yellow);
                label.TextSize = 18;
                label.SetTypeface(null, TypefaceStyle.Bold);
                var paramsLabel = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent);
                paramsLabel.TopMargin = 30;
                label.LayoutParameters = paramsLabel;
                _exampleContainer.AddView(label);

                var cardRow = new LinearLayout(this);
                cardRow.Orientation = Orientation.Horizontal;
                cardRow.SetGravity(GravityFlags.Center);
                var paramsRow = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.WrapContent);
                paramsRow.TopMargin = 10;
                cardRow.LayoutParameters = paramsRow;

                foreach (var card in currentStep.Examples[i])
                {
                    var iv = new ImageView(this);

                    if (card == null)
                    {
                        iv.SetImageResource(Resource.Drawable.card_back);
                    }
                    else
                    {
                        string imageName = GetImageName(card);
                        int resId = Resources.GetIdentifier(imageName, "drawable", PackageName);

                        if (resId != 0) iv.SetImageResource(resId);
                        else iv.SetImageResource(Resource.Drawable.card_back);
                    }

                    var paramsIv = new LinearLayout.LayoutParams(150, 210);
                    paramsIv.SetMargins(8, 0, 8, 0);
                    iv.LayoutParameters = paramsIv;

                    cardRow.AddView(iv);
                }

                _exampleContainer.AddView(cardRow);
            }
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
            StopSpeaking();
            Finish();
            return true;
        }
    }
}