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
	[Activity (Label = "Shooter", MainLauncher = true,
	           Theme = "@style/Theme.Splash",
	           AlwaysRetainTaskState=true,
	           ScreenOrientation=Android.Content.PM.ScreenOrientation.Landscape,
	           LaunchMode=Android.Content.PM.LaunchMode.SingleInstance,
	           ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | 
	           Android.Content.PM.ConfigChanges.KeyboardHidden | 
	           Android.Content.PM.ConfigChanges.Keyboard)]
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


