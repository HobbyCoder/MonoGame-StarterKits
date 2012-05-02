using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.Xna.Framework;

namespace Shooter
{
	[Activity (Label = "Shooter", MainLauncher = true)]
	public class Activity1 : AndroidGameActivity
	{

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate(bundle);
            Game1.Activity = this;
            var g = new Game1();
            SetContentView(g.Window);
            g.Run();
		}
	}
}


