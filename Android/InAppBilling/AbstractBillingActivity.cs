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
    using PurchaseState = Consts.PurchaseState;
    using ResponseCode = Consts.ResponseCode;


    public abstract class AbstractBillingActivity : Activity, BillingController.IConfiguration
    {
        protected AbstractBillingObserver mBillingObserver;

        public class BillingObserver : AbstractBillingObserver
        {

            public override void onBillingChecked(bool supported)
            {
                this.onBillingChecked(supported);
            }

            public override void onPurchaseStateChanged(String itemId, PurchaseState state)
            {
                this.onPurchaseStateChanged(itemId, state);
            }

            public override void onRequestPurchaseResponse(String itemId, ResponseCode response)
            {
                this.onRequestPurchaseResponse(itemId, response);
            }

        }


        /**
         * Returns the billing status. If it's currently unknown, requests to check
         * if billing is supported and
         * {@link AbstractBillingActivity#onBillingChecked(bool)} should be
         * called later with the result.
         * 
         * @return the current billing status (unknown, supported or unsupported).
         * @see AbstractBillingActivity#onBillingChecked(bool)
         */
        public BillingController.BillingStatus checkBillingSupported()
        {
            return BillingController.checkBillingSupported(this);
        }

        public abstract void onBillingChecked(bool supported);

        public override void onCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            BillingController.registerObserver(mBillingObserver);
            BillingController.setConfiguration(this); // This activity will provide
            // the public key and salt
            this.checkBillingSupported();
            if (!mBillingObserver.isTransactionsRestored())
            {
                BillingController.restoreTransactions(this);
            }
        }



        protected override void onDestroy()
        {
            base.OnDestroy();
            BillingController.unregisterObserver(mBillingObserver); // Avoid
            // receiving
            // notifications after
            // destroy
            BillingController.setConfiguration(null);
        }

        public abstract void onPurchaseStateChanged(String itemId, PurchaseState state);

        public abstract void onRequestPurchaseResponse(String itemId, ResponseCode response);

        /**
         * Requests the purchase of the specified item. The transaction will not be
         * confirmed automatically; such confirmation could be handled in
         * {@link AbstractBillingActivity#onPurchaseExecuted(String)}. If automatic
         * confirmation is preferred use
         * {@link BillingController#requestPurchase(android.content.Context, String, bool)}
         * instead.
         * 
         * @param itemId
         *            id of the item to be purchased.
         */
        public void requestPurchase(String itemId)
        {
            BillingController.requestPurchase(this, itemId);
        }

        /**
         * Requests to restore all transactions.
         */
        public void restoreTransactions()
        {
            BillingController.restoreTransactions(this);
        }

    }
}