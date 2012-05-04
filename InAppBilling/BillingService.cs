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

using System.Runtime.CompilerServices;
using Android.Util;
using Java.Lang;

namespace com.example.dungeons
{
    using ResponseCode = com.example.dungeons.Consts.ResponseCode;

    /// <summary>
    /// This class sends messages to Android Market on behalf of the application by
    /// connecting (binding) to the MarketBillingService. The application
    /// creates an instance of this class and invokes billing requests through this service.
    /// 
    /// The <seealso cref="BillingReceiver"/> class starts this service to process commands
    /// that it receives from Android Market.
    /// 
    /// You should modify and obfuscate this code before using it.
    /// </summary>
    public class BillingService : Service, IServiceConnection
    {
        private const string TAG = "BillingService";

        /// <summary>
        /// The service connection to the remote MarketBillingService. </summary>
        private static IMarketBillingService mService;

        /// <summary>
        /// The list of requests that are pending while we are waiting for the
        /// connection to the MarketBillingService to be established.
        /// </summary>
        private static LinkedList<BillingRequest> mPendingRequests = new LinkedList<BillingRequest>();

        /// <summary>
        /// The list of requests that we have sent to Android Market but for which we have
        /// not yet received a response code. The HashMap is indexed by the
        /// request Id that each request receives when it executes.
        /// </summary>
        private static Dictionary<long?, BillingRequest> mSentRequests = new Dictionary<long?, BillingRequest>();

        /// <summary>
        /// The base class for all requests that use the MarketBillingService.
        /// Each derived class overrides the run() method to call the appropriate
        /// service interface.  If we are already connected to the MarketBillingService,
        /// then we call the run() method directly. Otherwise, we bind
        /// to the service and save the request on a queue to be run later when
        /// the service is connected.
        /// </summary>
        public abstract class BillingRequest
        {
            private readonly BillingService outerInstance;

            private readonly int mStartId;
            protected internal long mRequestId;

            public BillingRequest(BillingService outerInstance, int startId)
            {
                this.outerInstance = outerInstance;
                mStartId = startId;
            }

            public virtual int startId
            {
                get
                {
                    return mStartId;
                }
            }

            /// <summary>
            /// Run the request, starting the connection if necessary. </summary>
            /// <returns> true if the request was executed or queued; false if there
            /// was an error starting the connection </returns>
            public virtual bool runRequest()
            {
                if (runIfConnected())
                {
                    return true;
                }

                if (bindToMarketBillingService())
                {
                    // Add a pending request to run when the service is connected.
                    mPendingRequests.AddLast(this);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Try running the request directly if the service is already connected. </summary>
            /// <returns> true if the request ran successfully; false if the service
            /// is not connected or there was an error when trying to use it </returns>
            public virtual bool runIfConnected()
            {
                if (Consts.DEBUG)
                {
                    Log.Debug(TAG, typeof(BillingService).FullName);
                }
                if (mService != null)
                {
                    try
                    {
                        mRequestId = run();
                        if (Consts.DEBUG)
                        {
                            Log.Debug(TAG, "request id: " + mRequestId);
                        }
                        if (mRequestId >= 0)
                        {
                            mSentRequests.Add(mRequestId, this);
                        }
                        return true;
                    }
                    catch (RemoteException e)
                    {
                        onRemoteException(e);
                    }
                }
                return false;
            }

            /// <summary>
            /// Called when a remote exception occurs while trying to execute the
            /// <seealso cref="#run()"/> method.  The derived class can override this to
            /// execute exception-handling code. </summary>
            /// <param name="e"> the exception </param>
            protected internal virtual void onRemoteException(RemoteException e)
            {
                Log.Warn(TAG, "remote billing service crashed");
                mService = null;
            }

            /// <summary>
            /// The derived class must implement this method. </summary>
            /// <exception cref="RemoteException"> </exception>
            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected abstract long run() throws RemoteException;
            protected internal abstract long run();

            /// <summary>
            /// This is called when Android Market sends a response code for this
            /// request. </summary>
            /// <param name="responseCode"> the response code </param>
            protected internal virtual void responseCodeReceived(Consts.ResponseCode responseCode)
            {
            }

            protected internal virtual Bundle makeRequestBundle(string method)
            {
                Bundle request = new Bundle();
                request.PutString(Consts.BILLING_REQUEST_METHOD, method);
                request.PutInt(Consts.BILLING_REQUEST_API_VERSION, 1);
                request.PutString(Consts.BILLING_REQUEST_PACKAGE_NAME, packageName);
                return request;
            }

            protected internal virtual void logResponseCode(string method, Bundle response)
            {
                ResponseCode responseCode = (ResponseCode)(response.GetInt(Consts.BILLING_RESPONSE_RESPONSE_CODE));
                if (Consts.DEBUG)
                {
                    Log.Error(TAG, method + " received " + responseCode.ToString());
                }
            }
        }

        /// <summary>
        /// Wrapper class that checks if in-app billing is supported.
        /// </summary>
        public class CheckBillingSupported : BillingRequest
        {
            public CheckBillingSupported()
                //: base(-1)
            {
                // This object is never created as a side effect of starting this
                // service so we pass -1 as the startId to indicate that we should
                // not stop this service after executing this request.
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected long run() throws RemoteException
            protected internal override long run()
            {
                Bundle request = makeRequestBundle("CHECK_BILLING_SUPPORTED");
                Bundle response = mService.sendBillingRequest(request);
                int responseCode = response.GetInt(Consts.BILLING_RESPONSE_RESPONSE_CODE);
                if (Consts.DEBUG)
                {
                    Log.Info(TAG, "CheckBillingSupported response code: " + (Consts.ResponseCode)responseCode);
                }
                bool billingSupported = (responseCode == (int)Consts.ResponseCode.RESULT_OK);
                ResponseHandler.checkBillingSupportedResponse(billingSupported);
                return Consts.BILLING_RESPONSE_INVALID_REQUEST_ID;
            }
        }

        /// <summary>
        /// Wrapper class that requests a purchase.
        /// </summary>
        public class RequestPurchase : BillingRequest
        {
            public readonly string mProductId;
            public readonly string mDeveloperPayload;

            public RequestPurchase(string itemId)
                : this(itemId, null)
            {
            }

            public RequestPurchase(string itemId, string developerPayload)
                //: base(-1)
            {
                // This object is never created as a side effect of starting this
                // service so we pass -1 as the startId to indicate that we should
                // not stop this service after executing this request.
                mProductId = itemId;
                mDeveloperPayload = developerPayload;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected long run() throws RemoteException
            protected internal override long run()
            {
                Bundle request = makeRequestBundle("REQUEST_PURCHASE");
                request.PutString(Consts.BILLING_REQUEST_ITEM_ID, mProductId);
                // Note that the developer payload is optional.
                if (mDeveloperPayload != null)
                {
                    request.PutString(Consts.BILLING_REQUEST_DEVELOPER_PAYLOAD, mDeveloperPayload);
                }
                Bundle response = mService.sendBillingRequest(request);
                PendingIntent pendingIntent = (PendingIntent)response.GetParcelable(Consts.BILLING_RESPONSE_PURCHASE_INTENT.ToString());
                if (pendingIntent == null)
                {
                    Log.Error(TAG, "Error with requestPurchase");
                    return Consts.BILLING_RESPONSE_INVALID_REQUEST_ID;
                }

                Intent intent = new Intent();
                ResponseHandler.buyPageIntentResponse(pendingIntent, intent);
                return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID, Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
            }

            protected internal override void responseCodeReceived(ResponseCode responseCode)
		    {
			    ResponseHandler.responseCodeReceived(BillingService.this, this, responseCode);
		    }
        }

        /// <summary>
        /// Wrapper class that confirms a list of notifications to the server.
        /// </summary>
        public class ConfirmNotifications : BillingRequest
        {
            internal readonly string[] mNotifyIds;

            public ConfirmNotifications(int startId, string[] notifyIds)
                //: base(startId)
            {
                mNotifyIds = notifyIds;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected long run() throws RemoteException
            protected internal override long run()
            {
                Bundle request = makeRequestBundle("CONFIRM_NOTIFICATIONS");
                request.PutStringArray(Consts.BILLING_REQUEST_NOTIFY_IDS, mNotifyIds);
                Bundle response = mService.sendBillingRequest(request);
                logResponseCode("confirmNotifications", response);
                return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID, Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
            }
        }

        /// <summary>
        /// Wrapper class that sends a GET_PURCHASE_INFORMATION message to the server.
        /// </summary>
        public class GetPurchaseInformation : BillingRequest
        {
            internal long mNonce;
            internal readonly string[] mNotifyIds;

            public GetPurchaseInformation(int startId, string[] notifyIds)
                //: base(startId)
            {
                mNotifyIds = notifyIds;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected long run() throws RemoteException
            protected internal override long run()
            {
                mNonce = Security.generateNonce();

                Bundle request = makeRequestBundle("GET_PURCHASE_INFORMATION");
                request.PutLong(Consts.BILLING_REQUEST_NONCE, mNonce);
                request.PutStringArray(Consts.BILLING_REQUEST_NOTIFY_IDS, mNotifyIds);
                Bundle response = mService.sendBillingRequest(request);
                logResponseCode("getPurchaseInformation", response);
                return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID, Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
            }

            protected internal override void onRemoteException(RemoteException e)
            {
                base.onRemoteException(e);
                Security.removeNonce(mNonce);
            }
        }

        /// <summary>
        /// Wrapper class that sends a RESTORE_TRANSACTIONS message to the server.
        /// </summary>
        public class RestoreTransactions : BillingRequest
        {
            internal long mNonce;

            public RestoreTransactions()
                //: base(-1)
            {
                // This object is never created as a side effect of starting this
                // service so we pass -1 as the startId to indicate that we should
                // not stop this service after executing this request.
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected long run() throws RemoteException
            protected internal override long run()
            {
                mNonce = Security.generateNonce();

                Bundle request = makeRequestBundle("RESTORE_TRANSACTIONS");
                request.PutLong(Consts.BILLING_REQUEST_NONCE, mNonce);
                Bundle response = mService.sendBillingRequest(request);
                logResponseCode("restoreTransactions", response);
                return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID, Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
            }

            protected internal override void onRemoteException(RemoteException e)
            {
                base.onRemoteException(e);
                Security.removeNonce(mNonce);
            }

            protected internal override void responseCodeReceived(ResponseCode responseCode)
		    {
			    ResponseHandler.responseCodeReceived(AttachBaseContext, this, responseCode);
		    }
        }

        //public virtual void BillingService()
        //{
        //    //base();
        //}

        public virtual Context context
        {
            set
            {
                AttachBaseContext(value);
            }
        }

        /// <summary>
        /// We don't support binding to this service, only starting the service.
        /// </summary>
        public override IBinder onBind(Intent intent)
        {
            return null;
        }

        public override void onStart(Intent intent, int startId)
        {
            handleCommand(intent, startId);
        }

        /// <summary>
        /// The <seealso cref="BillingReceiver"/> sends messages to this service using intents.
        /// Each intent has an action and some extra arguments specific to that action. </summary>
        /// <param name="intent"> the intent containing one of the supported actions </param>
        /// <param name="startId"> an identifier for the invocation instance of this service </param>
        public virtual void handleCommand(Intent intent, int startId)
        {
            string action = intent.Action;
            if (Consts.DEBUG)
            {
                Log.Info(TAG, "handleCommand() action: " + action);
            }
            if (Consts.ACTION_CONFIRM_NOTIFICATION.Equals(action))
            {
                string[] notifyIds = intent.GetStringArrayExtra(Consts.NOTIFICATION_ID);
                confirmNotifications(startId, notifyIds);
            }
            else if (Consts.ACTION_GET_PURCHASE_INFORMATION.Equals(action))
            {
                string notifyId = intent.GetStringExtra(Consts.NOTIFICATION_ID);
                getPurchaseInformation(startId, new string[] { notifyId });
            }
            else if (Consts.ACTION_PURCHASE_STATE_CHANGED.Equals(action))
            {
                string signedData = intent.GetStringExtra(Consts.INAPP_SIGNED_DATA);
                string signature = intent.GetStringExtra(Consts.INAPP_SIGNATURE);
                purchaseStateChanged(startId, signedData, signature);
            }
            else if (Consts.ACTION_RESPONSE_CODE.Equals(action))
            {
                long requestId = intent.GetLongExtra(Consts.INAPP_REQUEST_ID, -1);
                int responseCodeIndex = intent.GetIntExtra(Consts.INAPP_RESPONSE_CODE, (int)Consts.ResponseCode.RESULT_ERROR);
                Consts.ResponseCode responseCode = (Consts.ResponseCode)responseCodeIndex;
                checkResponseCode(requestId, responseCode);
            }
        }

        /// <summary>
        /// Binds to the MarketBillingService and returns true if the bind
        /// succeeded. </summary>
        /// <returns> true if the bind succeeded; false otherwise </returns>
        private static bool bindToMarketBillingService()
        {
            try
            {
                if (Consts.DEBUG)
                {
                    Log.Info(TAG, "binding to Market billing service");
                }
                bool bindResult =  BindService(new Intent(Consts.MARKET_BILLING_SERVICE_ACTION), this, Bind.AutoCreate); // ServiceConnection.

                if (bindResult)
                {
                    return true;
                }
                else
                {
                    Log.Error(TAG, "Could not bind to service.");
                }
            }
            catch (SecurityException e)
            {
                Log.Error(TAG, "Security exception: " + e);
            }
            return false;
        }

        /// <summary>
        /// Checks if in-app billing is supported. </summary>
        /// <returns> true if supported; false otherwise </returns>
        public virtual bool checkBillingSupported()
        {
            return (new CheckBillingSupported()).runRequest();
        }

        /// <summary>
        /// Requests that the given item be offered to the user for purchase. When
        /// the purchase succeeds (or is canceled) the <seealso cref="BillingReceiver"/>
        /// receives an intent with the action <seealso cref="Consts#ACTION_NOTIFY"/>.
        /// Returns false if there was an error trying to connect to Android Market. </summary>
        /// <param name="productId"> an identifier for the item being offered for purchase </param>
        /// <param name="developerPayload"> a payload that is associated with a given
        /// purchase, if null, no payload is sent </param>
        /// <returns> false if there was an error connecting to Android Market </returns>
        public virtual bool requestPurchase(string productId, string developerPayload)
        {
            return (new RequestPurchase(productId, developerPayload)).runRequest();
        }

        /// <summary>
        /// Requests transaction information for all managed items. Call this only when the
        /// application is first installed or after a database wipe. Do NOT call this
        /// every time the application starts up. </summary>
        /// <returns> false if there was an error connecting to Android Market </returns>
        public virtual bool restoreTransactions()
        {
            return (new RestoreTransactions()).runRequest();
        }
        /**
         * Confirms receipt of a purchase state change. Each {@code notifyId} is
         * an opaque identifier that came from the server. This method sends those
         * identifiers back to the MarketBillingService, which ACKs them to the
         * server. Returns false if there was an error trying to connect to the
         * MarketBillingService.
         * @param startId an identifier for the invocation instance of this service
         * @param notifyIds a list of opaque identifiers associated with purchase
         * state changes.
         * @return false if there was an error connecting to Market
         */
        private bool confirmNotifications(int startId, string[] notifyIds)
        {
            return new ConfirmNotifications(startId, notifyIds).runRequest();
        }


        /// <summary>
        /// Gets the purchase information. This message includes a list of
        /// notification IDs sent to us by Android Market, which we include in
        /// our request. The server responds with the purchase information,
        /// encoded as a JSON string, and sends that to the <seealso cref="BillingReceiver"/>
        /// in an intent with the action <seealso cref="Consts#ACTION_PURCHASE_STATE_CHANGED"/>.
        /// Returns false if there was an error trying to connect to the MarketBillingService.
        /// </summary>
        /// <param name="startId"> an identifier for the invocation instance of this service </param>
        /// <param name="notifyIds"> a list of opaque identifiers associated with purchase
        /// state changes </param>
        /// <returns> false if there was an error connecting to Android Market </returns>
        private bool getPurchaseInformation(int startId, string[] notifyIds)
        {
            return (new GetPurchaseInformation(startId, notifyIds)).runRequest();
        }

        /// <summary>
        /// Verifies that the data was signed with the given signature, and calls
        /// <seealso cref="ResponseHandler#purchaseResponse(Context, PurchaseState, String, String, long)"/>
        /// for each verified purchase. </summary>
        /// <param name="startId"> an identifier for the invocation instance of this service </param>
        /// <param name="signedData"> the signed JSON string (signed, not encrypted) </param>
        /// <param name="signature"> the signature for the data, signed with the private key </param>
        private void purchaseStateChanged(int startId, string signedData, string signature)
        {
            List<Security.VerifiedPurchase> purchases;
            purchases = Security.verifyPurchase(signedData, signature);
            if (purchases == null)
            {
                return;
            }

            List<string> notifyList = new List<string>();
            foreach (com.example.dungeons.Security.VerifiedPurchase vp in purchases)
            {
                if (vp.notificationId != null)
                {
                    notifyList.Add(vp.notificationId);
                }
                ResponseHandler.purchaseResponse(this, vp.purchaseState, vp.productId, vp.orderId, vp.purchaseTime, vp.developerPayload);
            }
            if (notifyList.Count > 0)
            {
                string[] notifyIds = notifyList.ToArray();
                confirmNotifications(startId, notifyIds);
            }
        }

        /// <summary>
        /// This is called when we receive a response code from Android Market for a request
        /// that we made. This is used for reporting various errors and for
        /// acknowledging that an order was sent to the server. This is NOT used
        /// for any purchase state changes.  All purchase state changes are received
        /// in the <seealso cref="BillingReceiver"/> and passed to this service, where they are
        /// handled in <seealso cref="#purchaseStateChanged(int, String, String)"/>. </summary>
        /// <param name="requestId"> a number that identifies a request, assigned at the
        /// time the request was made to Android Market </param>
        /// <param name="responseCode"> a response code from Android Market to indicate the state
        /// of the request </param>
        private void checkResponseCode(long requestId, Consts.ResponseCode responseCode)
        {
            BillingRequest request = mSentRequests.get(requestId);
            if (request != null)
            {
                if (Consts.DEBUG)
                {
                    Log.Debug(TAG, typeof(BillingRequest).FullName + ": " + responseCode);
                }
                request.responseCodeReceived(responseCode);
            }
            mSentRequests.Remove(requestId);
        }

        /// <summary>
        /// Runs any pending requests that are waiting for a connection to the
        /// service to be established.  This runs in the main UI thread.
        /// </summary>
        private void runPendingRequests()
        {
            int maxStartId = -1;
            BillingRequest request;
            while ((request = mPendingRequests.peek()) != null)
            {
                if (request.runIfConnected())
                {
                    // Remove the request
                    mPendingRequests.remove();

                    // Remember the largest startId, which is the most recent
                    // request to start this service.
                    if (maxStartId < request.startId)
                    {
                        maxStartId = request.startId;
                    }
                }
                else
                {
                    // The service crashed, so restart it. Note that this leaves
                    // the current request on the queue.
                    bindToMarketBillingService();
                    return;
                }
            }

            // If we get here then all the requests ran successfully.  If maxStartId
            // is not -1, then one of the requests started the service, so we can
            // stop it now.
            if (maxStartId >= 0)
            {
                if (Consts.DEBUG)
                {
                    Log.Info(TAG, "stopping service, startId: " + maxStartId);
                }

                StopSelf(maxStartId);
            }
        }

        /// <summary>
        /// This is called when we are connected to the MarketBillingService.
        /// This runs in the main UI thread.
        /// </summary>
        public virtual void onServiceConnected(ComponentName name, IBinder service)
        {
            if (Consts.DEBUG)
            {
                Log.Debug(TAG, "Billing service connected");
            }
            mService = new BillingService() as IMarketBillingService;
            //mService = IMarketBillingService.Stub.asInterface(service);
            runPendingRequests();
        }

        /// <summary>
        /// This is called when we are disconnected from the MarketBillingService.
        /// </summary>
        public virtual void onServiceDisconnected(ComponentName name)
        {
            Log.Warn(TAG, "Billing service disconnected");
            mService = null;
        }

        /// <summary>
        /// Unbinds from the MarketBillingService. Call this when the application
        /// terminates to avoid leaking a ServiceConnection.
        /// </summary>
        public virtual void unbind()
        {
            try
            {
                UnbindService(this);
            }
            catch (System.ArgumentException e)
            {
                // This might happen if the service was disconnected
            }
        }
    }
}