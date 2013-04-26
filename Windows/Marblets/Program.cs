#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Marblets
{
	static class Program
	{
		private static MarbletsGame game;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main ()
		{
			game = new MarbletsGame ();
			game.Run ();
		}
	}
}
