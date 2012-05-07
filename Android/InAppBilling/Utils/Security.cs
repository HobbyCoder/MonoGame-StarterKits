using System.Collections.Generic;
using Java.Security;
using Java.Security.Spec;


// Copyright 2010 Google Inc. All Rights Reserved.

namespace InAppBilling
{

    using PurchaseState = InAppBilling.Consts.PurchaseState;
    //using Base64 = InAppBilling.util.Base64;
    //using Base64DecoderException = InAppBilling.util.Base64DecoderException;

    //using JSONArray = org.json.JSONArray;
    //using JSONException = org.json.JSONException;
    //using JSONObject = org.json.JSONObject;

    using TextUtils = Android.Text.TextUtils;
    using Log = Android.Util.Log;
    using Android.Util;
    using Android.Content;
    using Java.Lang;
    using Android.Provider;


    /// <summary>
    /// Security-related methods. For a secure implementation, all of this code
    /// should be implemented on a server that communicates with the
    /// application on the device. For the sake of simplicity and clarity of this
    /// example, this code is included here and is executed on the device. If you
    /// must verify the purchases on the phone, you should obfuscate this code to
    /// make it harder for an attacker to replace the code with stubs that treat all
    /// purchases as verified.
    /// </summary>
    public class Security
    {
        private static HashSet<long> knownNonces = new HashSet<long>();
        private static SecureRandom RANDOM = new SecureRandom();
        private static string TAG = typeof(Security).FullName;

        /** Generates a nonce (a random number used once). */
        public static long generateNonce()
        {
            long nonce = RANDOM.NextLong();
            knownNonces.Add(nonce);
            return nonce;
        }

        public static bool isNonceKnown(long nonce)
        {
            return knownNonces.Contains(nonce);
        }

        public static void removeNonce(long nonce)
        {
            knownNonces.Remove(nonce);
        }

        public static string obfuscate(Context context, byte[] salt, string original)
        {
            AESObfuscator obfuscator = getObfuscator(context, salt);
            return obfuscator.obfuscate(original);
        }

        private static AESObfuscator _obfuscator = null;

        private static AESObfuscator getObfuscator(Context context, byte[] salt)
        {
            if (_obfuscator == null)
            {
                string installationId = Installation.id(context);
                string deviceId = Settings.Secure.GetString(context.ContentResolver, Settings.Secure.AndroidId);
                string password = installationId + deviceId + context.PackageName;
                _obfuscator = new AESObfuscator(salt, password);
            }
            return _obfuscator;
        }


        public static string unobfuscate(Context context, byte[] salt, string obfuscated)
        {
            AESObfuscator obfuscator = getObfuscator(context, salt);
            try
            {
                return obfuscator.unobfuscate(obfuscated);
            }
            catch (Exception e)
            {
                Log.Warn(TAG, "Invalid obfuscated data or key");
            }
            return null;
        }
    }

}