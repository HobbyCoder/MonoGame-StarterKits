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

namespace com.example.dungeons
{

    using RequestPurchase = com.example.dungeons.BillingService.RequestPurchase;
    using RestoreTransactions = com.example.dungeons.BillingService.RestoreTransactions;
    using PurchaseState = com.example.dungeons.Consts.PurchaseState;
    using ResponseCode = com.example.dungeons.Consts.ResponseCode;

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
    using Resource = Android.Resource;


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
            //builder.setTitle(titleId).setIcon(android.R.drawable.stat_sys_warning).setMessage(messageId).setCancelable(
            //        false).setPositiveButton(android.R.string.ok, null);
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

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            mBillingObserver = new BillingObserver() as AbstractBillingObserver;

            SetContentView(Resource.Layout.main);

            setupWidgets();
            BillingController.registerObserver(mBillingObserver);
            BillingController.checkBillingSupported(this);
            updateOwnedItems();
        }

        //protected Dialog onCreateDialog(int id) {
        //    switch (id) {
        //    case DIALOG_BILLING_NOT_SUPPORTED_ID:
        //        return createDialog(Android.Resource.String.billing_not_supported_title, Android.Resource.String.billing_not_supported_message);
        //    default:
        //        return null;
        //    }
        //}

        protected override void OnDestroy()
        {
            //BillingController.unregisterObserver(mBillingObserver);
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
            //    BillingController.restoreTransactions(this);
            //    Toast.MakeText(this, Android.Resource.String.restoring_transactions, ToastLength.Long).show();
            //}
        }

        private void setupWidgets()
        {
            mBuyButton = (Button)FindViewById(Android.Resource.Id.buy_button);
            mBuyButton.Enabled = false;
            //mBuyButton.setOnClickListener(new OnClickListener() {

            //    @Override
            //    public void onClick(View v) {
            //        BillingController.requestPurchase(Dungeons.this, mSku, true /* confirm */);
            //    }
            //});

            mSelectItemSpinner = (Spinner)FindViewById(Android.Resource.Id.item_choices);
            //mCatalogAdapter = new CatalogAdapter(this, CatalogEntry.CATALOG);
            mSelectItemSpinner.Adapter = mCatalogAdapter;
            //mSelectItemSpinner.setOnItemSelectedListener(new OnItemSelectedListener() {

            //    public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {
            //        mSku = CatalogEntry.CATALOG[position].sku;
            //    }

            //    public void onNothingSelected(AdapterView<?> arg0) {
            //    }

            //});

            mOwnedItemsTable = (ListView)FindViewById(Android.Resource.Id.owned_items);
        }

        private void updateOwnedItems()
        {
            //List<Transaction> transactions = BillingController.getTransactions(this);
            //final ArrayList<String> ownedItems = new ArrayList<String>();
            //for (Transaction t : transactions) {
            //    if (t.purchaseState == PurchaseState.PURCHASED) {
            //        ownedItems.add(t.productId);
            //    }
            //}

            //mCatalogAdapter.setOwnedItems(ownedItems);
            //final ArrayAdapter<String> adapter = new ArrayAdapter<String>(this, R.layout.item_row, R.id.item_name,
            //        ownedItems);
            //mOwnedItemsTable.setAdapter(adapter);
        }

        public class BillingObserver : AbstractBillingObserver
        {

            public override void onBillingChecked(bool supported) {
                onBillingChecked(supported);
            }

            public override void onPurchaseStateChanged(string itemId, PurchaseState state) {
                onPurchaseStateChanged(itemId, state);
            }

            public override void onRequestPurchaseResponse(string itemId, ResponseCode response) {
                onRequestPurchaseResponse(itemId, response);
            }
        };


    }
}

