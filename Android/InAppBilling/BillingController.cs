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
using Android.Util;
using Android.Text;
using System.Json;

namespace com.example.dungeons
{
    using ResponseCode = Consts.ResponseCode;
    using PurchaseState = Consts.PurchaseState;

    public class BillingController
    {

        public enum BillingStatus
        {
            UNKNOWN, SUPPORTED, UNSUPPORTED
        }

        /**
         * Used to provide on-demand values to the billing controller.
         */
        public interface IConfiguration
        {

            /**
             * Returns a salt for the obfuscation of purchases in local memory.
             * 
             * @return array of 20 random bytes.
             */
            byte[] getObfuscationSalt();

            /**
             * Returns the public key used to verify the signature of responses of
             * the Market Billing service.
             * 
             * @return Base64 encoded public key.
             */
            string getPublicKey();
        }

        private static BillingStatus status = BillingStatus.UNKNOWN;

        private static HashSet<String> automaticConfirmations = new HashSet<String>();
        private static IConfiguration configuration = null;
        private static bool debug = false;
        private static ISignatureValidator validator = null;

        private const String JSON_NONCE = "nonce";
        private const String JSON_ORDERS = "orders";
        private static Dictionary<string, HashSet<String>> manualConfirmations = new Dictionary<String, HashSet<String>>();

        private static List<IBillingObserver> observers = new List<IBillingObserver>();

        public const String LOG_TAG = "Billing";

        static Dictionary<long, BillingRequest> pendingRequests = new Dictionary<long, BillingRequest>();

        /**
         * Adds the specified notification to the set of manual confirmations of the
         * specified item.
         * 
         * @param itemId
         *            id of the item.
         * @param notificationId
         *            id of the notification.
         */
        private void addManualConfirmation(string itemId, string notificationId)
        {
            HashSet<String> notifications = manualConfirmations[itemId];
            if (notifications == null)
            {
                notifications = new HashSet<String>();
                manualConfirmations.Add(itemId, notifications);
            }
            notifications.Add(notificationId);
        }

        /**
         * Returns the billing status. If it is currently unknown, checks the
         * billing status asynchronously. Observers will receive a
         * {@link IBillingObserver#onBillingChecked(bool)} notification in either
         * case.
         * 
         * @param context
         * @return the current billing status (unknown, supported or unsupported).
         * @see IBillingObserver#onBillingChecked(bool)
         */
        public static BillingStatus checkBillingSupported(Context context)
        {
            if (status == BillingStatus.UNKNOWN)
            {
                BillingService.checkBillingSupported(context);
            }
            else
            {
                bool supported = status == BillingStatus.SUPPORTED;
                onBillingChecked(supported);
            }
            return status;
        }

        /**
         * Requests to confirm all pending notifications for the specified item.
         * 
         * @param context
         * @param itemId
         *            id of the item whose purchase must be confirmed.
         * @return true if pending notifications for this item were found, false
         *         otherwise.
         */
        public static bool confirmNotifications(Context context, string itemId)
        {
            var notifications = manualConfirmations[itemId];
            if (notifications != null)
            {
                //confirmNotifications(context, notifications.ToArray(new String[] {}));
                confirmNotifications(context, itemId);
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
         * Requests to confirm all specified notifications.
         * 
         * @param context
         * @param notifyIds
         *            array with the ids of all the notifications to confirm.
         */
        private static void confirmNotifications(Context context, string[] notifyIds)
        {
            Intent intent = new Intent(context, typeof(BillingController));
            BillingService bis = new BillingService();
            bis.confirmNotifications(intent, int.Parse(notifyIds[0]));
        }

        /**
	 * Returns the number of purchases for the specified item. Refunded and
	 * cancelled purchases are not subtracted. See
	 * {@link #countPurchasesNet(Context, String)} if they need to be.
	 * 
	 * @param context
	 * @param itemId
	 *            id of the item whose purchases will be counted.
	 * @return number of purchases for the specified item.
	 */
        public static int countPurchases(Context context, string itemId) 
        {
		    byte[] salt = getSalt();
		    itemId = salt != null ? Security.obfuscate(context, salt, itemId) : itemId;
		    return TransactionManager.countPurchases(context, itemId);
	    }
     
        /**
         * Requests purchase information for the specified notification. Immediately
         * followed by a call to
         * {@link #onPurchaseInformationResponse(long, bool)} and later to
         * {@link #onPurchaseStateChanged(Context, String, String)}, if the request
         * is successful.
         * 
         * @param context
         * @param notifyId
         *            id of the notification whose purchase information is
         *            requested.
         */
        private static void getPurchaseInformation(Context context, string notifyId)
        {
            long nonce = Security.generateNonce();
            BillingService.getPurchaseInformation(context, new string[] { notifyId }, nonce);
        }

        /**
         * Gets the salt from the configuration and logs a warning if it's null.
         * 
         * @return salt.
         */
        private static byte[] getSalt()
        {
            byte[] salt = null;
            if (configuration == null || ((salt = configuration.getObfuscationSalt()) == null))
            {
                Log.Warn(LOG_TAG, "Can't (un)obfuscate purchases without salt");
            }
            return salt;
        }

        /**
         * Lists all transactions stored locally, including cancellations and
         * refunds.
         * 
         * @param context
         * @return list of transactions.
         */
        public static List<Transaction> getTransactions(Context context)
        {
            List<Transaction> transactions = TransactionManager.getTransactions(context);
            unobfuscate(context, transactions);
            return transactions;
        }

        /**
         * Lists all transactions of the specified item, stored locally.
         * 
         * @param context
         * @param itemId
         *            id of the item whose transactions will be returned.
         * @return list of transactions.
         */
        public static List<Transaction> getTransactions(Context context, String itemId)
        {
            byte[] salt = getSalt();
            itemId = salt != null ? Security.obfuscate(context, salt, itemId) : itemId;
            List<Transaction> transactions = TransactionManager.getTransactions(context, itemId);
            unobfuscate(context, transactions);
            return transactions;
        }

        /**
         * Returns true if the specified item has been registered as purchased in
         * local memory. Note that if the item was later canceled or refunded this
         * will still return true. Also note that the item might have been purchased
         * in another installation, but not yet registered in this one.
         * 
         * @param context
         * @param itemId
         *            item id.
         * @return true if the specified item is purchased, false otherwise.
         */
        public static bool isPurchased(Context context, String itemId)
        {
            byte[] salt = getSalt();
            itemId = salt != null ? Security.obfuscate(context, salt, itemId) : itemId;
            return TransactionManager.isPurchased(context, itemId);
        }

        /**
         * Notifies observers of the purchase state change of the specified item.
         * 
         * @param itemId
         *            id of the item whose purchase state has changed.
         * @param state
         *            new purchase state of the item.
         */
        private static void notifyPurchaseStateChange(string itemId, PurchaseState state)
        {
            foreach (IBillingObserver o in observers)
            {
                o.onPurchaseStateChanged(itemId, state);
            }
        }

        /**
         * Obfuscates the specified purchase. Only the order id, product id and
         * developer payload are obfuscated.
         * 
         * @param context
         * @param purchase
         *            purchase to be obfuscated.
         * @see #unobfuscate(Context, Transaction)
         */
        static void obfuscate(Context context, Transaction purchase)
        {
            byte[] salt = getSalt();
            if (salt == null)
            {
                return;
            }
            purchase.orderId = Security.obfuscate(context, salt, purchase.orderId);
            purchase.productId = Security.obfuscate(context, salt, purchase.productId);
            purchase.developerPayload = Security.obfuscate(context, salt, purchase.developerPayload);
        }

        /**
         * Called after the response to a
         * {@link net.robotmedia.billing.request.CheckBillingSupported} request is
         * received.
         * 
         * @param supported
         */
        public static void onBillingChecked(bool supported)
        {
            status = supported ? BillingStatus.SUPPORTED : BillingStatus.UNSUPPORTED;
            foreach (IBillingObserver o in observers)
            {
                o.onBillingChecked(supported);
            }
        }

        /**
         * Called when an IN_APP_NOTIFY message is received.
         * 
         * @param context
         * @param notifyId
         *            notification id.
         */
        protected static void onNotify(Context context, String notifyId)
        {
            Log.Info("Info", "Notification " + notifyId + " available");

            getPurchaseInformation(context, notifyId);
        }

        /**
         * Called after the response to a
         * {@link net.robotmedia.billing.request.RequestPurchase} request is
         * received.
         * 
         * @param itemId
         *            id of the item whose purchase was requested.
         * @param purchaseIntent
         *            intent to purchase the item.
         */
        public static void onPurchaseIntent(String itemId, PendingIntent purchaseIntent)
        {
            foreach (IBillingObserver o in observers)
            {
                o.onPurchaseIntent(itemId, purchaseIntent);
            }
        }

        public static void onPurchaseStateChanged(Context context, string signedData, string signature) {
		Log.Info(LOG_TAG, "Purchase state changed");

		if (TextUtils.IsEmpty(signedData)) {
			Log.Warn(LOG_TAG, "Signed data is empty");
			return;
		}

		if (!debug) {
			if (TextUtils.IsEmpty(signature)) {
				Log.Warn(LOG_TAG, "Empty signature requires debug mode");
				return;
			}
			 ISignatureValidator validator = BillingController.validator != null ? BillingController.validator
					: new DefaultSignatureValidator(BillingController.configuration);
			if (!validator.validate(signedData, signature)) {
				Log.Warn(LOG_TAG, "Signature does not match data.");
				return;
			}
		}

		List<Transaction> purchases;
		try {
			JsonObject jObject = new JsonObject(signedData);
			if (!verifyNonce(jObject)) {
				Log.Warn(LOG_TAG, "Invalid nonce");
				return;
			}
			purchases = parsePurchases(jObject);
		} catch (Exception e) {
			Log.Error(LOG_TAG, "JSON exception: ", e);
			return;
		}

		List<String> confirmations = new List<String>();
		foreach (Transaction p in purchases) {
			if (p.notificationId != null && automaticConfirmations.Contains(p.productId)) {
				confirmations.Add(p.notificationId);
			} else {
				// TODO: Discriminate between purchases, cancellations and
				// refunds.
                BillingController bc = new BillingController();
                bc.addManualConfirmation(p.productId, p.notificationId);
			}
			storeTransaction(context, p);
			notifyPurchaseStateChange(p.productId, p.purchaseState);
		}
		if (confirmations.Count() != 0) {
            string[] notifyIds = confirmations.ToArray();
                //(new String[confirmations.Count()]);
			confirmNotifications(context, notifyIds);
		}
	}

        /**
         * Called after a {@link net.robotmedia.billing.BillingRequest} is
         * sent.
         * 
         * @param requestId
         *            the id the request.
         * @param request
         *            the billing request.
         */
        public static void onRequestSent(long requestId, BillingRequest request)
        {
            Log.Info(LOG_TAG, "Request " + requestId + " of type " + request.getRequestType() + " sent");

            if (request.isSuccess())
            {
                pendingRequests.put(requestId, request);
            }
            else if (request.hasNonce())
            {
                Security.removeNonce(request.getNonce());
            }
        }

        /**
         * Called after a {@link net.robotmedia.billing.BillingRequest} is
         * sent.
         * 
         * @param context
         * @param requestId
         *            the id of the request.
         * @param responseCode
         *            the response code.
         * @see net.robotmedia.billing.request.ResponseCode
         */
        protected static void onResponseCode(Context context, long requestId, int responseCode)
        {
            ResponseCode response = (ResponseCode)responseCode;
            Log.Info(LOG_TAG, "Request " + requestId + " received response " + response);

            BillingRequest request = pendingRequests[requestId];
            if (request != null)
            {
                pendingRequests.remove(requestId);
                request.onResponseCode(response);
            }
        }

        public static void onTransactionsRestored()
        {
            foreach (IBillingObserver o in observers)
            {
                o.onTransactionsRestored();
            }
        }

        /**
         * Parse all purchases from the JSON data received from the Market Billing
         * service.
         * 
         * @param data
         *            JSON data received from the Market Billing service.
         * @return list of purchases.
         * @throws JSONException
         *             if the data couldn't be properly parsed.
         */
        private static List<Transaction> parsePurchases(JsonObject data)
        {
            List<Transaction> purchases = new List<Transaction>();
            JsonArray orders = data.optJsonArray(JSON_ORDERS);
            int numTransactions = 0;
            if (orders != null)
            {
                numTransactions = orders.Count();
            }
            for (int i = 0; i < numTransactions; i++)
            {
                JsonObject jElement = orders.getJSONObject(i);
                Transaction p = Transaction.parse(jElement);
                purchases.Add(p);
            }
            return purchases;
        }

        /**
         * Registers the specified billing observer.
         * 
         * @param observer
         *            the billing observer to add.
         * @return true if the observer wasn't previously registered, false
         *         otherwise.
         * @see #unregisterObserver(IBillingObserver)
         */
        public static bool registerObserver(IBillingObserver observer)
        {
            if (observers.Contains(observer))
                return true;
            else
            {
                observers.Add(observer);
                return false;
            }
        }

        /**
         * Requests the purchase of the specified item. The transaction will not be
         * confirmed automatically.
         * 
         * @param context
         * @param itemId
         *            id of the item to be purchased.
         * @see #requestPurchase(Context, String, bool)
         */
        public static void requestPurchase(Context context, String itemId)
        {
            requestPurchase(context, itemId, false);
        }

        /**
         * Requests the purchase of the specified item with optional automatic
         * confirmation.
         * 
         * @param context
         * @param itemId
         *            id of the item to be purchased.
         * @param confirm
         *            if true, the transaction will be confirmed automatically. If
         *            false, the transaction will have to be confirmed with a call
         *            to {@link #confirmNotifications(Context, String)}.
         * @see IBillingObserver#onPurchaseIntent(String, PendingIntent)
         */
        public static void requestPurchase(Context context, String itemId, bool confirm)
        {
            if (confirm)
            {
                automaticConfirmations.Add(itemId);
            }
            BillingService.requestPurchase(context, itemId, null);
        }

        /**
         * Requests to restore all transactions.
         * 
         * @param context
         */
        public static void restoreTransactions(Context context)
        {
            long nonce = Security.generateNonce();
            BillingService.restoreTransations(context, nonce);
        }

        /**
         * Sets the configuration instance of the controller.
         * 
         * @param config
         *            configuration instance.
         */
        public static void setConfiguration(IConfiguration config)
        {
            configuration = config;
        }

        /**
         * Sets debug mode.
         * 
         * @param value
         */
        public static void setDebug(bool value)
        {
            debug = value;
        }

        /**
         * Sets a custom signature validator. If no custom signature validator is
         * provided,
         * {@link net.robotmedia.billing.signature.DefaultSignatureValidator} will
         * be used.
         * 
         * @param validator
         *            signature validator instance.
         */
        public static void setSignatureValidator(ISignatureValidator validator)
        {
            BillingController.validator = validator;
        }

        /**
         * Starts the specified purchase intent with the specified activity.
         * 
         * @param activity
         * @param purchaseIntent
         *            purchase intent.
         * @param intent
         */
        public static void startPurchaseIntent(Activity activity, PendingIntent purchaseIntent, Intent intent)
        {
            if (Compatibility.isStartIntentSenderSupported())
            {
                // This is on Android 2.0 and beyond. The in-app buy page activity
                // must be on the activity stack of the application.
                Compatibility.startIntentSender(activity, purchaseIntent.IntentSender, intent);
            }
            else
            {
                // This is on Android version 1.6. The in-app buy page activity must
                // be on its own separate activity stack instead of on the activity
                // stack of the application.
                try
                {
                    purchaseIntent.Send(activity, 0 /* code */, intent);
                }
                catch (Android.App.PendingIntent.CanceledException e)
                {
                    Log.Error(LOG_TAG, "Error starting purchase intent", e);
                }
            }
        }



        static void storeTransaction(Context context, Transaction t) {
		Transaction t2 = t.clone();
		obfuscate(context, t2);
		TransactionManager.addTransaction(context, t2);
	}

        static void unobfuscate(Context context, List<Transaction> transactions)
        {
            foreach (Transaction p in transactions)
            {
                unobfuscate(context, p);
            }
        }

        /**
         * Unobfuscate the specified purchase.
         * 
         * @param context
         * @param purchase
         *            purchase to unobfuscate.
         * @see #obfuscate(Context, Transaction)
         */
        static void unobfuscate(Context context, Transaction purchase)
        {
            byte[] salt = getSalt();
            if (salt == null)
            {
                return;
            }
            purchase.orderId = Security.unobfuscate(context, salt, purchase.orderId);
            purchase.productId = Security.unobfuscate(context, salt, purchase.productId);
            purchase.developerPayload = Security.unobfuscate(context, salt, purchase.developerPayload);
        }




        /**
        * Unregisters the specified billing observer.
        * 
        * @param observer
        * the billing observer to unregister.
        * @return true if the billing observer was unregistered, false otherwise.
        * @see #registerObserver(IBillingObserver)
        */
        public static bool unregisterObserver(IBillingObserver observer)
        {
            return observers.Remove(observer);
        }

        private static bool verifyNonce(JsonObject data)
        {
            long nonce = data[JSON_NONCE];
            if (Security.isNonceKnown(nonce))
            {
                Security.removeNonce(nonce);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void onRequestPurchaseResponse(String itemId, ResponseCode response)
        {
            foreach (IBillingObserver o in observers)
            {
                o.onRequestPurchaseResponse(itemId, response);
            }
        }


    }
}