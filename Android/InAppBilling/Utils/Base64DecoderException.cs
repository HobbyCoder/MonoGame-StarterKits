using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

/**
 * Exception thrown when encountering an invalid Base64 input character.
 *
 * @author nelson
 */
public class Base64DecoderException : Exception {
  public Base64DecoderException() {
    //super();
  }

  public Base64DecoderException(string s) : base(s) {
  }

  private static long serialVersionUID = 1L;
}