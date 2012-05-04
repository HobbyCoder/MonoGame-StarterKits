using System.Collections.Generic;
using Java.Security;
using Java.Security.Spec;


// Copyright 2010 Google Inc. All Rights Reserved.

namespace com.example.dungeons
{

    using PurchaseState = com.example.dungeons.Consts.PurchaseState;
    //using Base64 = com.example.dungeons.util.Base64;
    //using Base64DecoderException = com.example.dungeons.util.Base64DecoderException;

    //using JSONArray = org.json.JSONArray;
    //using JSONException = org.json.JSONException;
    //using JSONObject = org.json.JSONObject;

    using TextUtils = Android.Text.TextUtils;
    using Log = Android.Util.Log;
    using Android.Util;


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
        private const string TAG = "Security";

        private const string KEY_FACTORY_ALGORITHM = "RSA";
        private const string SIGNATURE_ALGORITHM = "SHA1withRSA";
        private static readonly SecureRandom RANDOM = new SecureRandom();

        /// <summary>
        /// This keeps track of the nonces that we generated and sent to the
        /// server.  We need to keep track of these until we get back the purchase
        /// state and send a confirmation message back to Android Market. If we are
        /// killed and lose this list of nonces, it is not fatal. Android Market will
        /// send us a new "notify" message and we will re-generate a new nonce.
        /// This has to be "static" so that the <seealso cref="BillingReceiver"/> can
        /// check if a nonce exists.
        /// </summary>
        private static HashSet<long?> sKnownNonces = new HashSet<long?>();

        /// <summary>
        /// A class to hold the verified purchase information.
        /// </summary>
        public class VerifiedPurchase
        {
            public PurchaseState purchaseState;
            public string notificationId;
            public string productId;
            public string orderId;
            public long purchaseTime;
            public string developerPayload;

            public VerifiedPurchase(PurchaseState purchaseState, string notificationId, string productId, string orderId, long purchaseTime, string developerPayload)
            {
                this.purchaseState = purchaseState;
                this.notificationId = notificationId;
                this.productId = productId;
                this.orderId = orderId;
                this.purchaseTime = purchaseTime;
                this.developerPayload = developerPayload;
            }
        }

        /// <summary>
        /// Generates a nonce (a random number used once). </summary>
        public static long generateNonce()
        {
            long nonce = RANDOM.NextLong();
            sKnownNonces.Add(nonce);
            return nonce;
        }

        public static void removeNonce(long nonce)
        {
            sKnownNonces.Remove(nonce);
        }

        public static bool isNonceKnown(long nonce)
        {
            return sKnownNonces.Contains(nonce);
        }

        /// <summary>
        /// Verifies that the data was signed with the given signature, and returns
        /// the list of verified purchases. The data is in JSON format and contains
        /// a nonce (number used once) that we generated and that was signed
        /// (as part of the whole data string) with a private key. The data also
        /// contains the <seealso cref="PurchaseState"/> and product ID of the purchase.
        /// In the general case, there can be an array of purchase transactions
        /// because there may be delays in processing the purchase on the backend
        /// and then several purchases can be batched together. </summary>
        /// <param name="signedData"> the signed JSON string (signed, not encrypted) </param>
        /// <param name="signature"> the signature for the data, signed with the private key </param>
        public static List<VerifiedPurchase> verifyPurchase(string signedData, string signature)
        {
            if (signedData == null)
            {
                Log.Error(TAG, "data is null");
                return null;
            }
            if (Consts.DEBUG)
            {
                Log.Info(TAG, "signedData: " + signedData);
            }
            bool verified = false;
            if (!TextUtils.IsEmpty(signature))
            {
                /// <summary>
                /// Compute your public key (that you got from the Android Market publisher site).
                /// 
                /// Instead of just storing the entire literal string here embedded in the
                /// program,  construct the key at runtime from pieces or
                /// use bit manipulation (for example, XOR with some other string) to hide
                /// the actual key.  The key itself is not secret information, but we don't
                /// want to make it easy for an adversary to replace the public key with one
                /// of their own and then fake messages from the server.
                /// 
                /// Generally, encryption keys / passwords should only be kept in memory
                /// long enough to perform the operation they need to perform.
                /// </summary>
                string base64EncodedPublicKey = "your public key here";
                IPublicKey key = Security.generatePublicKey(base64EncodedPublicKey);
                verified = Security.verify(key, signedData, signature);
                if (!verified)
                {
                    Log.w(TAG, "signature does not match data.");
                    return null;
                }
            }

            JSONObject jObject;
            JSONArray jTransactionsArray = null;
            int numTransactions = 0;
            long nonce = 0L;
            try
            {
                jObject = new JSONObject(signedData);

                // The nonce might be null if the user backed out of the buy page.
                nonce = jObject.optLong("nonce");
                jTransactionsArray = jObject.optJSONArray("orders");
                if (jTransactionsArray != null)
                {
                    numTransactions = jTransactionsArray.Length;
                }
            }
            catch (JSONException e)
            {
                return null;
            }

            if (!Security.isNonceKnown(nonce))
            {
                Log.w(TAG, "Nonce not found: " + nonce);
                return null;
            }

            List<VerifiedPurchase> purchases = new List<VerifiedPurchase>();
            try
            {
                for (int i = 0; i < numTransactions; i++)
                {
                    JSONObject jElement = jTransactionsArray.getJSONObject(i);
                    int response = jElement.getInt("purchaseState");
                    PurchaseState purchaseState = PurchaseState.valueOf(response);
                    string productId = jElement.getString("productId");
                    string packageName = jElement.getString("packageName");
                    long purchaseTime = jElement.getLong("purchaseTime");
                    string orderId = jElement.optString("orderId", "");
                    string notifyId = null;
                    if (jElement.has("notificationId"))
                    {
                        notifyId = jElement.getString("notificationId");
                    }
                    string developerPayload = jElement.optString("developerPayload", null);

                    // If the purchase state is PURCHASED, then we require a
                    // verified nonce.
                    if (purchaseState == PurchaseState.PURCHASED && !verified)
                    {
                        continue;
                    }
                    purchases.Add(new VerifiedPurchase(purchaseState, notifyId, productId, orderId, purchaseTime, developerPayload));
                }
            }
            catch (JSONException e)
            {
                Log.Error(TAG, "JSON exception: ", e);
                return null;
            }
            removeNonce(nonce);
            return purchases;
        }

        /// <summary>
        /// Generates a PublicKey instance from a string containing the
        /// Base64-encoded public key.
        /// </summary>
        /// <param name="encodedPublicKey"> Base64-encoded public key </param>
        /// <exception cref="IllegalArgumentException"> if encodedPublicKey is invalid </exception>
        public static IPublicKey generatePublicKey(string encodedPublicKey)
        {
            try
            {
                byte[] decodedKey = Base64.decode(encodedPublicKey);
                KeyFactory keyFactory = KeyFactory.GetInstance(KEY_FACTORY_ALGORITHM);
                return keyFactory.GeneratePublic(new X509EncodedKeySpec(decodedKey));
            }
            catch (NoSuchAlgorithmException e)
            {
                throw new System.Exception(e.Message);
            }
            catch (InvalidKeySpecException e)
            {
                Log.Error(TAG, "Invalid key specification.");
                throw new System.ArgumentException(e.Message);
            }
            catch (Base64DecoderException e)
            {
                Log.Error(TAG, "Base64 decoding failed.");
                throw new System.ArgumentException(e);
            }
        }

        /// <summary>
        /// Verifies that the signature from the server matches the computed
        /// signature on the data.  Returns true if the data is correctly signed.
        /// </summary>
        /// <param name="publicKey"> public key associated with the developer account </param>
        /// <param name="signedData"> signed data from server </param>
        /// <param name="signature"> server signature </param>
        /// <returns> true if the data and signature match </returns>
        public static bool verify(IPublicKey publicKey, string signedData, string signature)
        {
            if (Consts.DEBUG)
            {
                Log.Info(TAG, "signature: " + signature);
            }
            Signature sig;
            try
            {
                sig = Signature.GetInstance(SIGNATURE_ALGORITHM);
                sig.InitVerify(publicKey);
                System.Text.UTF8Encoding en = new System.Text.UTF8Encoding();
                sig.Update(en.GetBytes(signedData));
                //is 0 correct?
                if (!sig.Verify(Base64.Decode(signature, 0)))
                {
                    Log.Error(TAG, "Signature verification failed.");
                    return false;
                }
                return true;
            }
            catch (NoSuchAlgorithmException e)
            {
                Log.Error(TAG, "NoSuchAlgorithmException.");
            }
            catch (InvalidKeyException e)
            {
                Log.Error(TAG, "Invalid key specification.");
            }
            catch (SignatureException e)
            {
                Log.Error(TAG, "Signature exception.");
            }
            //catch (Base64DecoderException e)
            //{
            //    Log.Error(TAG, "Base64 decoding failed.");
            //}
            return false;
        }
    }

}