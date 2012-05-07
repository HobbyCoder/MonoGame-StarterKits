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
using Android.Preferences;


namespace InAppBilling
{
    /**
 * Abstract subclass of IBillingObserver that provides default implementations
 * for {@link IBillingObserver#onPurchaseIntent(String, PendingIntent)} and
 * {@link IBillingObserver#onTransactionsRestored()}.
 * 
 */
    public abstract class AbstractBillingObserver : IBillingObserver
    {

        //protected const String KEY_TRANSACTIONS_RESTORED = "net.robotmedia.billing.transactionsRestored";

        //protected Activity activity;

        //public AbstractBillingObserver(Activity activity)
        //{
        //    this.activity = activity;
        //}

        //public bool isTransactionsRestored()
        //{
        //    ISharedPreferences preferences = PreferenceManager.GetDefaultSharedPreferences(activity);
        //    return preferences.GetBoolean(KEY_TRANSACTIONS_RESTORED, false);
        //}

        ///**
        // * Called after requesting the purchase of the specified item. The default
        // * implementation simply starts the pending intent.
        // * 
        // * @param itemId
        // *            id of the item whose purchase was requested.
        // * @param purchaseIntent
        // *            a purchase pending intent for the specified item.
        // */

        //public override void onPurchaseIntent(String itemId, PendingIntent purchaseIntent)
        //{
        //    BillingController.startPurchaseIntent(activity, purchaseIntent, null);
        //}

        //public override void onTransactionsRestored()
        //{
        //    ISharedPreferences preferences = PreferenceManager.GetDefaultSharedPreferences(activity);
        //    ISharedPreferencesEditor editor = preferences.Edit();
        //    editor.PutBoolean(KEY_TRANSACTIONS_RESTORED, true);
        //    editor.Commit();
        //}


        //public void onBillingChecked(bool supported)
        //{
        //    throw new NotImplementedException();
        //}

        //public void onPurchaseStateChanged(string itemId, Consts.PurchaseState state)
        //{
        //    throw new NotImplementedException();
        //}

        //public void onRequestPurchaseResponse(string itemId, Consts.ResponseCode response)
        //{
        //    throw new NotImplementedException();
        //}
        public void onBillingChecked(bool supported)
        {
            throw new NotImplementedException();
        }

        public void onPurchaseIntent(string itemId, PendingIntent purchaseIntent)
        {
            throw new NotImplementedException();
        }

        public void onPurchaseStateChanged(string itemId, Consts.PurchaseState state)
        {
            throw new NotImplementedException();
        }

        public void onRequestPurchaseResponse(string itemId, Consts.ResponseCode response)
        {
            throw new NotImplementedException();
        }

        public void onTransactionsRestored()
        {
            throw new NotImplementedException();
        }
    }
}