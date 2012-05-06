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
using Java.Lang;

namespace com.example.dungeons
{
    using ResponseCode = Consts.ResponseCode;
    public abstract class BillingRequest
    {

        private static string KEY_BILLING_REQUEST = "BILLING_REQUEST";
        private static string KEY_API_VERSION = "API_VERSION";
        private static string KEY_PACKAGE_NAME = "PACKAGE_NAME";
        private static string KEY_RESPONSE_CODE = "RESPONSE_CODE";

        protected static string KEY_REQUEST_ID = "REQUEST_ID";

        private static string KEY_NONCE = "NONCE";

        public static long IGNORE_REQUEST_ID = -1;
        private string packageName;

        private int startId;
        private bool success;
        private long nonce;

        public long getNonce()
        {
            return nonce;
        }


        protected Bundle makeRequestBundle()
        {
            Bundle request = new Bundle();
            request.PutString(KEY_BILLING_REQUEST, getRequestType());
            request.PutInt(KEY_API_VERSION, 1);
            request.PutString(KEY_PACKAGE_NAME, packageName);
            if (hasNonce())
            {
                request.PutLong(KEY_NONCE, nonce);
            }
            return request;
        }

        public virtual void onResponseCode(ResponseCode responde)
        {
            // Do nothing by default
        }

        protected virtual void processOkResponse(Bundle response)
        {
            // Do nothing by default
        }

        public long run(IMarketBillingService mService)
        {
            Bundle request = makeRequestBundle();
            addParams(request);
            Bundle response = new Bundle();
            try
            {
                response = mService.sendBillingRequest(request);
            }
            catch (NullPointerException e)
            {
                Log.Error(typeof(BillingRequest).FullName, "Known IAB bug. See: http://code.google.com/p/marketbilling/issues/detail?id=25", e);
                return IGNORE_REQUEST_ID;
            }

            if (validateResponse(response))
            {
                processOkResponse(response);
                return response.GetLong(KEY_REQUEST_ID, IGNORE_REQUEST_ID);
            }
            else
            {
                return IGNORE_REQUEST_ID;
            }
        }

        public void setNonce(long nonce)
        {
            this.nonce = nonce;
        }

        protected bool validateResponse(Bundle response)
        {
            int responseCode = response.GetInt(KEY_RESPONSE_CODE);
            if (responseCode == 0)
                success = true;
            if (!success)
            {
                Log.Warn(typeof(BillingRequest).FullName, "Error with response code " + (ResponseCode)responseCode);
            }
            return success;
        }

        public int getStartId()
        {
            return startId;
        }


        public BillingRequest(string packageName, int startId)
        {
            this.packageName = packageName;
            this.startId = startId;
        }

        protected virtual void addParams(Bundle request)
        {
            // Do nothing by default
        }



        public virtual string getRequestType() { return string.Empty; }

        public virtual bool hasNonce()
        {
            return false;
        }

        public bool isSuccess()
        {
            return success;
        }

        /// <summary>
        /// Wrapper class that checks if in-app billing is supported.
        /// </summary>
        public class CheckBillingSupported : BillingRequest
        {
            public CheckBillingSupported(string packageName, int startId)
            :base(packageName, startId)
            {
            }

            public override string getRequestType()
            {
                return "CHECK_BILLING_SUPPORTED";
            }

            protected override void processOkResponse(Bundle response)
            {
                bool supported = this.isSuccess();
                BillingController.onBillingChecked(supported);
            }
        }

        /// <summary>
        /// Wrapper class that confirms a list of notifications to the server.
        /// </summary>
        public class ConfirmNotifications : BillingRequest
        {
            private string[] notifyIds;

            private static string KEY_NOTIFY_IDS = "NOTIFY_IDS";

            public ConfirmNotifications(string packageName, int startId, string[] notifyIds) : base(packageName, startId)
            {
                this.notifyIds = notifyIds;
            }

            protected override void addParams(Bundle request)
            {
                request.PutStringArray(KEY_NOTIFY_IDS, notifyIds);
            }

            public override string getRequestType()
            {
                return "CONFIRM_NOTIFICATIONS";
            }
        }

        /// <summary>
        /// Wrapper class that requests a purchase.
        /// </summary>
        public class RequestPurchase : BillingRequest
        {
            private string itemId;
            private string developerPayload;

            private static string KEY_ITEM_ID = "ITEM_ID";
            private static string KEY_DEVELOPER_PAYLOAD = "DEVELOPER_PAYLOAD";
            private static string KEY_PURCHASE_INTENT = "PURCHASE_INTENT";

            public RequestPurchase(string packageName, int startId, string itemId, string developerPayload)
            : base(packageName, startId)
            {
                this.itemId = itemId;
                this.developerPayload = developerPayload;
            }

            protected override void addParams(Bundle request)
            {
                request.PutString(KEY_ITEM_ID, itemId);
                if (developerPayload != null)
                {
                    request.PutString(KEY_DEVELOPER_PAYLOAD, developerPayload);
                }
            }

            public override string getRequestType()
            {
                return "REQUEST_PURCHASE";
            }

            public override void onResponseCode(ResponseCode response)
            {
                base.onResponseCode(response);
                BillingController.onRequestPurchaseResponse(itemId, response);
            }

            protected override void processOkResponse(Bundle response)
            {
                PendingIntent purchaseIntent = (PendingIntent)response.GetParcelable(KEY_PURCHASE_INTENT);
                BillingController.onPurchaseIntent(itemId, purchaseIntent);
            }

        }

        /// <summary>
        /// Wrapper class that sends a GET_PURCHASE_INFORMATION message to the server.
        /// </summary>
        public class GetPurchaseInformation : BillingRequest
        {
            private string[] notifyIds;
    	
    	    private string KEY_NOTIFY_IDS = "NOTIFY_IDS";
    	
    	    public GetPurchaseInformation(string packageName, int startId, string[] notifyIds) 
    		    : base(packageName, startId)
            {
    		    this.notifyIds = notifyIds;
    	    }
    	
    	    protected override void addParams(Bundle request) {
    		    request.PutStringArray(KEY_NOTIFY_IDS, notifyIds);
    	    }

    	    public override string getRequestType() {
    		    return "GET_PURCHASE_INFORMATION";
    	    }

            public override bool hasNonce() { return true; }

        }

        /// <summary>
        /// Wrapper class that sends a RESTORE_TRANSACTIONS message to the server.
        /// </summary>
        public class RestoreTransactions : BillingRequest
        {
            public RestoreTransactions(string packageName, int startId) : base(packageName, startId)
            {
            }

            public string getRequestType()
            {
                return "RESTORE_TRANSACTIONS";
            }

            public override bool hasNonce() { return true; }

            public override void onResponseCode(ResponseCode response)
            {
                base.onResponseCode(response);
                if (response == ResponseCode.RESULT_OK)
                {
                    BillingController.onTransactionsRestored();
                }
            }
        }
    }
}