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

namespace com.example.dungeons
{

    public class Compatibility
    {
        //private static Method startIntentSender;
        public static int START_NOT_STICKY;

        //private static final Class[] START_INTENT_SENDER_SIG = new Class[] {
        //    IntentSender.class, Intent.class, int.class, int.class, int.class
        //};

        //static {
        //    initCompatibility();
        //};

        private static void initCompatibility()
        {
            //try {
            //    final Field field = Service.class.getField("START_NOT_STICKY");
            //    START_NOT_STICKY = field.getInt(null);
            //} catch (Exception e) {
            //    START_NOT_STICKY = 2;			
            //}
            //try {
            //    startIntentSender = Activity.class.getMethod("startIntentSender",
            //            START_INTENT_SENDER_SIG);
            //} catch (SecurityException e) {
            //    startIntentSender = null;
            //} catch (NoSuchMethodException e) {
            //    startIntentSender = null;
            //}
        }

        public static void startIntentSender(Activity activity, IntentSender intentSender, Intent intent)
        {
            //if (startIntentSender != null) {
            //     final Object[] args = new Object[5];
            //     args[0] = intentSender;
            //     args[1] = intent;
            //     args[2] = Integer.valueOf(0);
            //     args[3] = Integer.valueOf(0);
            //     args[4] = Integer.valueOf(0);
            //     try {
            //         startIntentSender.invoke(activity, args);
            //     } catch (Exception e) {
            //         Log.e(Compatibility.class.getSimpleName(), "startIntentSender", e);
            //     }
            //}
        }

        public static bool isStartIntentSenderSupported()
        {
            return true;
            //return startIntentSender != null;
        }
    }
}