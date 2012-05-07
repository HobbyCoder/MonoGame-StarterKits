using System.Collections.Generic;

/*
 * Copyright (C) 2010 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace InAppBilling
{

    using RequestPurchase = InAppBilling.BillingRequest.RequestPurchase;
    using RestoreTransactions = InAppBilling.BillingRequest.RestoreTransactions;
    using PurchaseState = InAppBilling.Consts.PurchaseState;
    using ResponseCode = InAppBilling.Consts.ResponseCode;

    using Activity = Android.App.Activity;
    using AlertDialog = Android.App.AlertDialog;
    using Dialog = Android.App.Dialog;
    using Context = Android.Content.Context;
    using DialogInterface = Android.Content.DialogInterface;
    using Intent = Android.Content.Intent;
    using SharedPreferences = Android.Content.ISharedPreferences;
    using Cursor = Android.Database.ICursor;
    using Uri = Android.Net.Uri;
    using Bundle = Android.OS.Bundle;
    using Handler = Android.OS.Handler;
    using Html = Android.Text.Html;
    using Spanned = Android.Text.ISpanned;
    using SpannableStringBuilder = Android.Text.SpannableStringBuilder;
    using Log = Android.Util.Log;
    using View = Android.Views.View;
    using OnClickListener = Android.Views.View.IOnClickListener;
    using ViewGroup = Android.Views.ViewGroup;
    using AdapterView = Android.Widget.AdapterView;
    using OnItemSelectedListener = Android.Widget.AdapterView.IOnItemSelectedListener;
    using ArrayAdapter = Android.Widget.ArrayAdapter;
    using Button = Android.Widget.Button;
    using ListView = Android.Widget.ListView;
    using SimpleCursorAdapter = Android.Widget.SimpleCursorAdapter;
    using Spinner = Android.Widget.Spinner;
    using TextView = Android.Widget.TextView;
    using Toast = Android.Widget.Toast;
    using Android.Content;
    using Android.Runtime;
    using Android.Widget;
    using Java.Util;

    [Android.App.Activity(MainLauncher = true, NoHistory = true)]
    public class Dungeons : Activity
    {

        private const string TAG = "Dungeons";

        private Button mBuyButton;
        private Spinner mSelectItemSpinner;
        private ListView mOwnedItemsTable;

        private const int DIALOG_BILLING_NOT_SUPPORTED_ID = 2;

        private string mSku;

        private CatalogAdapter mCatalogAdapter;

        private AbstractBillingObserver mBillingObserver;

        private Dialog createDialog(int titleId, int messageId)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle(titleId)
                .SetIcon(Resource.Drawable.Icon)
                .SetMessage(messageId);
                //.SetPositiveButton(Resource.String.ok, null);
            return builder.Create();
        }

        public void onBillingChecked(bool supported)
        {
            if (supported)
            {
                restoreTransactions();
                mBuyButton.Enabled = true;
            }

            else
            {
                ShowDialog(DIALOG_BILLING_NOT_SUPPORTED_ID);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.main);
            setupWidgets();
            BillingController.registerObserver(mBillingObserver);
            BillingController.checkBillingSupported(this);
            updateOwnedItems();
            onBillingChecked(true);
        }

        protected Dialog onCreateDialog(int id)
        {
            switch (id)
            {
                case DIALOG_BILLING_NOT_SUPPORTED_ID:
                    return createDialog(Resource.String.billing_not_supported_title, Resource.String.billing_not_supported_message);
                default:
                    return null;
            }
        }


        protected override void OnDestroy()
        {
            BillingController.unregisterObserver(mBillingObserver);
            base.OnDestroy();
        }

        public void onPurchaseStateChanged(string itemId, PurchaseState state)
        {
            Log.Info(TAG, "onPurchaseStateChanged() itemId: " + itemId);
            updateOwnedItems();
        }

        public void onRequestPurchaseResponse(string itemId, ResponseCode response)
        {
        }

        /**
         * Restores previous transactions, if any. This happens if the application
         * has just been installed or the user wiped data. We do not want to do this
         * on every startup, rather, we want to do only when the database needs to
         * be initialized.
         */
        private void restoreTransactions()
        {
            //if (!mBillingObserver.isTransactionsRestored())
            //{
                BillingController.restoreTransactions(this);
                Toast.MakeText(this, Resource.String.restoring_transactions, ToastLength.Long).Show();
            //}
        }

        private void setupWidgets()
        {
            mBuyButton = (Button)FindViewById(Resource.Id.buy_button);
            mBuyButton.Enabled = false;
            mBuyButton.Click += delegate
            {
                    BillingController.requestPurchase(this, mSku, true /* confirm */);
            };

            mSelectItemSpinner = (Spinner)FindViewById(Resource.Id.item_choices);
            mCatalogAdapter = new CatalogAdapter(this, CatalogEntry.CATALOG);
            mSelectItemSpinner.Adapter = mCatalogAdapter;
            mSelectItemSpinner.ItemSelected += delegate(object sender, ItemEventArgs e)
            {
                mSku = CatalogEntry.CATALOG[e.Position].sku;
            };


             mOwnedItemsTable = (ListView)FindViewById(Resource.Id.owned_items);
        }

        private void updateOwnedItems()
        {
            List<Transaction> transactions = BillingController.getTransactions(this);
            List<string> ownedItems = new List<string>();
            foreach (Transaction t in transactions) {
                if (t.purchaseState == PurchaseState.PURCHASED) {
                    ownedItems.Add(t.productId);
                }
            }

            mCatalogAdapter.setOwnedItems(ownedItems);
            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Resource.Layout.item_row, Resource.Id.item_name,
                    ownedItems);
            mOwnedItemsTable.Adapter = adapter;
        }

        public class BillingObserver : AbstractBillingObserver
        {
      

            public void onBillingChecked(bool supported) {
                onBillingChecked(supported);
            }

            public void onPurchaseStateChanged(string itemId, PurchaseState state) {
                onPurchaseStateChanged(itemId, state);
            }

            public void onRequestPurchaseResponse(string itemId, ResponseCode response) {
                onRequestPurchaseResponse(itemId, response);
            }

        };


    }
}

