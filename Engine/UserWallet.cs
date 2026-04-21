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
    // static class: There is only ONE UserWallet in the entire app.
    public static class UserWallet
    {
        // We start with 50,000 coins.
        public static int Balance { get; set; } = 50000;
    }
}