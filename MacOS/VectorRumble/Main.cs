using System;
using System.Collections.Generic;
using System.Linq;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
#elif IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace VectorRumble
{
#if MONOMAC
	class Program
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();

			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate ();

				// Set our Application Icon
				NSImage appIcon = NSImage.ImageNamed ("monogameicon.png");
				NSApplication.SharedApplication.ApplicationIconImage = appIcon;
				
				NSApplication.Main (args);
			}
		}
	}

	class AppDelegate : NSApplicationDelegate
	{
		private VectorRumbleGame game;

		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			game = new VectorRumbleGame();
			game.Run();
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}
#elif IPHONE
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
		private VectorRumbleGame game;

		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new VectorRumbleGame();
			game.Run();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
#else
	static class Program
	{
		private static VectorRumbleGame game;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			game = new VectorRumbleGame();
			game.Run();
		}
	}
#endif
}


