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
    using GetPurchaseInformation = com.example.dungeons.BillingRequest.GetPurchaseInformation;
    using CheckBillingSupported = com.example.dungeons.BillingRequest.CheckBillingSupported;
    using ConfirmNotifications = com.example.dungeons.BillingRequest.ConfirmNotifications;
    using RequestPurchase = com.example.dungeons.BillingRequest.RequestPurchase;
    using RestoreTransactions = com.example.dungeons.BillingRequest.RestoreTransactions;

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
        private enum Action
        {
            CHECK_BILLING_SUPPORTED, CONFIRM_NOTIFICATIONS, GET_PURCHASE_INFORMATION, REQUEST_PURCHASE, RESTORE_TRANSACTIONS, NULL
        }

        static string ACTION_MARKET_BILLING_SERVICE = "com.android.vending.billing.MarketBillingService.BIND";
        static string EXTRA_DEVELOPER_PAYLOAD = "DEVELOPER_PAYLOAD";

        static string EXTRA_ITEM_ID = "ITEM_ID";
        static string EXTRA_NONCE = "EXTRA_NONCE";
        static string EXTRA_NOTIFY_IDS = "NOTIFY_IDS";

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


        public static void checkBillingSupported(Context context)
        {
            Intent intent = createIntent(context, Action.CHECK_BILLING_SUPPORTED);
            context.StartService(intent);
        }

        private static Intent createIntent(Context context, Action action)
        {
            string actionString = getActionForIntent(context, action);
            Intent intent = new Intent(actionString);
            intent.SetClass(context, typeof(BillingService));
            return intent;
        }

        private static string getActionForIntent(Context context, Action action)
        {
            return context.PackageName + "." + action.ToString();
        }

        public static void getPurchaseInformation(Context context, string[] notifyIds, long nonce)
        {
            Intent intent = createIntent(context, Action.GET_PURCHASE_INFORMATION);
            intent.PutExtra(EXTRA_NOTIFY_IDS, notifyIds);
            intent.PutExtra(EXTRA_NONCE, nonce);
            context.StartService(intent);
        }

        public static void requestPurchase(Context context, string itemId, string developerPayload)
        {
            Intent intent = createIntent(context, Action.REQUEST_PURCHASE);
            intent.PutExtra(EXTRA_ITEM_ID, itemId);
            intent.PutExtra(EXTRA_DEVELOPER_PAYLOAD, developerPayload);
            context.StartService(intent);
        }

        public static void restoreTransations(Context context, long nonce)
        {
            Intent intent = createIntent(context, Action.RESTORE_TRANSACTIONS);
            intent.SetClass(context, typeof(BillingService));
            intent.PutExtra(EXTRA_NONCE, nonce);
            context.StartService(intent);
        }

        private void bindMarketBillingService()
        {
            try
            {
                bool bindResult = BindService(new Intent(ACTION_MARKET_BILLING_SERVICE), this, Bind.AutoCreate);
                if (!bindResult)
                {
                    Log.Error(typeof(BillingService).FullName, "Could not bind to MarketBillingService");
                }
            }
            catch (SecurityException e)
            {
                Log.Error(typeof(BillingService).FullName, "Could not bind to MarketBillingService", e);
            }
        }

        private void checkBillingSupported(int startId)
        {
            string packageName = PackageName;
            CheckBillingSupported request = new CheckBillingSupported(packageName, startId);
            runRequestOrQueue(request);
        }

        private void confirmNotifications(Intent intent, int startId)
        {
            string packageName = PackageName;
            string[] notifyIds = intent.GetStringArrayExtra(EXTRA_NOTIFY_IDS);
            ConfirmNotifications request = new ConfirmNotifications(packageName, startId, notifyIds);
            runRequestOrQueue(request);
        }

        private Action getActionFromIntent(Intent intent)
        {
            string actionString = intent.Action;
            if (actionString == null)
            {
                return Action.NULL;
            }
            string[] split = actionString.Split('.');
            if (split.Length <= 0)
            {
                return Action.NULL;
            }
            return (Action)System.Enum.Parse(typeof(Action), split[split.Length - 1]);
        }

        private void getPurchaseInformation(Intent intent, int startId)
        {
            string packageName = PackageName;
            long nonce = intent.GetLongExtra(EXTRA_NONCE, 0);
            string[] notifyIds = intent.GetStringArrayExtra(EXTRA_NOTIFY_IDS);
            GetPurchaseInformation request = new GetPurchaseInformation(packageName, startId, notifyIds);
            request.setNonce(nonce);
            runRequestOrQueue(request);
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            mService = new BillingService() as IMarketBillingService;
            runPendingRequests();
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            mService = null;
        }

        // This is the old onStart method that will be called on the pre-2.0
        // platform.  On 2.0 or later we override onStartCommand() so this
        // method will not be called.
        public override void OnStart(Intent intent, int startId)
        {
            handleCommand(intent, startId);
        }

        // @Override // Avoid compile errors on pre-2.0
        public int onStartCommand(Intent intent, int flags, int startId)
        {
            handleCommand(intent, startId);
            return Compatibility.START_NOT_STICKY;
        }

        private void handleCommand(Intent intent, int startId)
        {
            Action action = getActionFromIntent(intent);
            if (action == null)
            {
                return;
            }
            switch (action)
            {
                case Action.CHECK_BILLING_SUPPORTED:
                    checkBillingSupported(startId);
                    break;
                case Action.REQUEST_PURCHASE:
                    requestPurchase(intent, startId);
                    break;
                case Action.GET_PURCHASE_INFORMATION:
                    getPurchaseInformation(intent, startId);
                    break;
                case Action.CONFIRM_NOTIFICATIONS:
                    confirmNotifications(intent, startId);
                    break;
                case Action.RESTORE_TRANSACTIONS:
                    restoreTransactions(intent, startId);
                    break;
            }
        }

        private void requestPurchase(Intent intent, int startId)
        {
            string packageName = PackageName;
            string itemId = intent.GetStringExtra(EXTRA_ITEM_ID);
            string developerPayload = intent.GetStringExtra(EXTRA_DEVELOPER_PAYLOAD);
            RequestPurchase request = new RequestPurchase(packageName, startId, itemId, developerPayload);
            runRequestOrQueue(request);
        }

        private void restoreTransactions(Intent intent, int startId)
        {
            string packageName = PackageName;
            long nonce = intent.GetLongExtra(EXTRA_NONCE, 0);
            RestoreTransactions request = new RestoreTransactions(packageName, startId);
            request.setNonce(nonce);
            runRequestOrQueue(request);
        }

        private void runPendingRequests()
        {
            BillingRequest request;
            int maxStartId = -1;
            while ((request = mPendingRequests.Peek()) != null)
            {
                if (mService != null)
                {
                    runRequest(request);
                    mPendingRequests.remove();
                    if (maxStartId < request.getStartId())
                    {
                        maxStartId = request.getStartId();
                    }
                }
                else
                {
                    bindMarketBillingService();
                    return;
                }
            }
            if (maxStartId >= 0)
            {
                StopSelf(maxStartId);
            }
        }

        private void runRequest(BillingRequest request)
        {
            try
            {
                long requestId = request.run(mService);
                BillingController.onRequestSent(requestId, request);
            }
            catch (RemoteException e)
            {
                Log.Warn(typeof(BillingService).FullName, "Remote billing service crashed");
                // TODO: Retry?
            }
        }

        private void runRequestOrQueue(BillingRequest request)
        {
            mPendingRequests.AddLast(request);
            if (mService == null)
            {
                bindMarketBillingService();
            }
            else
            {
                runPendingRequests();
            }
        }

        public void onDestroy()
        {
            base.OnDestroy();
            // Ensure we're not leaking Android Market billing service
            if (mService != null)
            {
                try
                {
                    UnbindService(this);
                }
                catch (IllegalArgumentException)
                {
                    // This might happen if the service was disconnected
                }
            }
        }
    }

}