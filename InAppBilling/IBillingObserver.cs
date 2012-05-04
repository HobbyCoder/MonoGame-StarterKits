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
    public interface IBillingObserver
    {

        /**
         * Called only once after determining if in-app billing is supported or not.
         * 
         * @param supported
         *            if true, in-app billing is supported. Otherwise, it isn't.
         * @see BillingController#checkBillingSupported(android.content.Context)
         */
        public void onBillingChecked(bool supported);

        /**
         * Called after requesting the purchase of the specified item.
         * 
         * @param itemId
         *            id of the item whose purchase was requested.
         * @param purchaseIntent
         *            a purchase pending intent for the specified item.
         * @see BillingController#requestPurchase(android.content.Context, String,
         *      bool)
         */
        void onPurchaseIntent(String itemId, PendingIntent purchaseIntent);

        /**
         * Called when the specified item is purchased, cancelled or refunded.
         * 
         * @param itemId
         *            id of the item whose purchase state has changed.
         * @param state
         *            purchase state of the specified item.
         */
        void onPurchaseStateChanged(String itemId, Consts.PurchaseState state);

        /**
         * Called with the response for the purchase request of the specified item.
         * This is used for reporting various errors, or if the user backed out and
         * didn't purchase the item.
         * 
         * @param itemId
         *            id of the item whose purchase was requested
         * @param response
         *            response of the purchase request
         */
        void onRequestPurchaseResponse(String itemId, Consts.ResponseCode response);

        /**
         * Called when a restore transactions request has been successfully
         * received by the server.
         */
        void onTransactionsRestored();

    }
}