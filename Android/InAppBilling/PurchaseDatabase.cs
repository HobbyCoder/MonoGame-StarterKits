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
using Android.Database.Sqlite;
using Android.Database;
using Android.Util;


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


    using PurchaseState = InAppBilling.Consts.PurchaseState;
    using ResponseCode = InAppBilling.Consts.ResponseCode;

    //using ContentValues = android.content.ContentValues;
    //using Context = android.content.Context;
    //using ICursor = android.database.ICursor;
    //using SQLiteDatabase = android.database.sqlite.SQLiteDatabase;
    //using SQLiteOpenHelper = android.database.sqlite.SQLiteOpenHelper;
    //using Log = android.util.Log;

    /// <summary>
    /// An example database that records the state of each purchase. You should use
    /// an obfuscator before storing any information to persistent storage. The
    /// obfuscator should use a key that is specific to the device and/or user.
    /// Otherwise an attacker could copy a database full of valid purchases and
    /// distribute it to others.
    /// </summary>
    public class PurchaseDatabase
    {
        private const string TAG = "PurchaseDatabase";
        private const string DATABASE_NAME = "purchase.db";
        private const int DATABASE_VERSION = 1;
        private const string PURCHASE_HISTORY_TABLE_NAME = "history";
        private const string PURCHASED_ITEMS_TABLE_NAME = "purchased";

        // These are the column names for the purchase history table. We need a
        // column named "_id" if we want to use a ICursorAdapter. The primary key is
        // the orderId so that we can be robust against getting multiple messages
        // from the server for the same purchase.
        internal const string HISTORY_ORDER_ID_COL = "_id";
        internal const string HISTORY_STATE_COL = "state";
        internal const string HISTORY_PRODUCT_ID_COL = "productId";
        internal const string HISTORY_PURCHASE_TIME_COL = "purchaseTime";
        internal const string HISTORY_DEVELOPER_PAYLOAD_COL = "developerPayload";

        private static readonly string[] HISTORY_COLUMNS = { HISTORY_ORDER_ID_COL, HISTORY_PRODUCT_ID_COL, HISTORY_STATE_COL, HISTORY_PURCHASE_TIME_COL, HISTORY_DEVELOPER_PAYLOAD_COL };

        // These are the column names for the "purchased items" table.
        internal const string PURCHASED_PRODUCT_ID_COL = "_id";
        internal const string PURCHASED_QUANTITY_COL = "quantity";

        private static readonly string[] PURCHASED_COLUMNS = { PURCHASED_PRODUCT_ID_COL, PURCHASED_QUANTITY_COL };

        private SQLiteDatabase mDb;
        private DatabaseHelper mDatabaseHelper;

        public PurchaseDatabase(Context context)
        {
            mDatabaseHelper = new DatabaseHelper(context);
            mDb = mDatabaseHelper.WritableDatabase;
        }

        public virtual void close()
        {
            mDatabaseHelper.Close();
        }


        /// <summary>
        /// Inserts a purchased product into the database. There may be multiple
        /// rows in the table for the same product if it was purchased multiple times
        /// or if it was refunded. </summary>
        /// <param name="orderId"> the order ID (matches the value in the product list) </param>
        /// <param name="productId"> the product ID (sku) </param>
        /// <param name="state"> the state of the purchase </param>
        /// <param name="purchaseTime"> the purchase time (in milliseconds since the epoch) </param>
        /// <param name="developerPayload"> the developer provided "payload" associated with
        ///     the order. </param>
        private void insertOrder(string orderId, string productId, PurchaseState state, long purchaseTime, string developerPayload)
        {
            ContentValues values = new ContentValues();
            values.Put(HISTORY_ORDER_ID_COL, orderId);
            values.Put(HISTORY_PRODUCT_ID_COL, productId);
            values.Put(HISTORY_STATE_COL, state.ToString());
            values.Put(HISTORY_PURCHASE_TIME_COL, purchaseTime);
            values.Put(HISTORY_DEVELOPER_PAYLOAD_COL, developerPayload);
            mDb.Replace(PURCHASE_HISTORY_TABLE_NAME, null, values); // nullColumnHack
        }

        /// <summary>
        /// Updates the quantity of the given product to the given value. If the
        /// given value is zero, then the product is removed from the table. </summary>
        /// <param name="productId"> the product to update </param>
        /// <param name="quantity"> the number of times the product has been purchased </param>
        private void updatePurchasedItem(string productId, int quantity)
        {
            if (quantity == 0)
            {
                mDb.Delete(PURCHASED_ITEMS_TABLE_NAME, PURCHASED_PRODUCT_ID_COL + "=?", new string[] { productId });
                return;
            }
            ContentValues values = new ContentValues();
            values.Put(PURCHASED_PRODUCT_ID_COL, productId);
            values.Put(PURCHASED_QUANTITY_COL, quantity);
            mDb.Replace(PURCHASED_ITEMS_TABLE_NAME, null, values); // nullColumnHack
        }

        /// <summary>
        /// Adds the given purchase information to the database and returns the total
        /// number of times that the given product has been purchased. </summary>
        /// <param name="orderId"> a string identifying the order </param>
        /// <param name="productId"> the product ID (sku) </param>
        /// <param name="purchaseState"> the purchase state of the product </param>
        /// <param name="purchaseTime"> the time the product was purchased, in milliseconds
        /// since the epoch (Jan 1, 1970) </param>
        /// <param name="developerPayload"> the developer provided "payload" associated with
        ///     the order </param>
        /// <returns> the number of times the given product has been purchased. </returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual int updatePurchase(string orderId, string productId, PurchaseState purchaseState, long purchaseTime, string developerPayload)
        {
            insertOrder(orderId, productId, purchaseState, purchaseTime, developerPayload);
            ICursor ICursor = mDb.Query(PURCHASE_HISTORY_TABLE_NAME, HISTORY_COLUMNS, HISTORY_PRODUCT_ID_COL + "=?", new string[] { productId }, null, null, null, null);
            if (ICursor == null)
            {
                return 0;
            }
            int quantity = 0;
            try
            {
                // Count the number of times the product was purchased
                while (ICursor.MoveToNext())
                {
                    int stateIndex = ICursor.GetInt(2);
                    PurchaseState state = (PurchaseState)stateIndex;
                    // Note that a refunded purchase is treated as a purchase. Such
                    // a friendly refund policy is nice for the user.
                    if (state == PurchaseState.PURCHASED || state == PurchaseState.REFUNDED)
                    {
                        quantity += 1;
                    }
                }

                // Update the "purchased items" table
                updatePurchasedItem(productId, quantity);
            }
            catch
            {
                if (ICursor != null)
                {
                    ICursor.Close();
                }
            }
            return quantity;
        }

        /// <summary>
        /// Returns a ICursor that can be used to read all the rows and columns of
        /// the "purchased items" table.
        /// </summary>
        public virtual ICursor QueryAllPurchasedItems()
        {
            return mDb.Query(PURCHASED_ITEMS_TABLE_NAME, PURCHASED_COLUMNS, null, null, null, null, null);
        }

        /// <summary>
        /// This is a standard helper class for constructing the database.
        /// </summary>
        private class DatabaseHelper : SQLiteOpenHelper
        {
            public DatabaseHelper(Context context)
                : base(context, DATABASE_NAME, null, DATABASE_VERSION)
            {
            }

            public override void OnCreate(SQLiteDatabase db)
            {
                createPurchaseTable(db);
            }

            public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
            {
                // Production-quality upgrade code should modify the tables when
                // the database version changes instead of dropping the tables and
                // re-creating them.
                if (newVersion != DATABASE_VERSION)
                {
                    Log.Warn(TAG, "Database upgrade from old: " + oldVersion + " to: " + newVersion);
                    db.ExecSQL("DROP TABLE IF EXISTS " + PURCHASE_HISTORY_TABLE_NAME);
                    db.ExecSQL("DROP TABLE IF EXISTS " + PURCHASED_ITEMS_TABLE_NAME);
                    createPurchaseTable(db);
                    return;
                }
            }

            private void createPurchaseTable(SQLiteDatabase db)
            {
                db.ExecSQL("CREATE TABLE " + PURCHASE_HISTORY_TABLE_NAME + "(" + HISTORY_ORDER_ID_COL + " TEXT PRIMARY KEY, " + HISTORY_STATE_COL + " INTEGER, " + HISTORY_PRODUCT_ID_COL + " TEXT, " + HISTORY_DEVELOPER_PAYLOAD_COL + " TEXT, " + HISTORY_PURCHASE_TIME_COL + " INTEGER)");
                db.ExecSQL("CREATE TABLE " + PURCHASED_ITEMS_TABLE_NAME + "(" + PURCHASED_PRODUCT_ID_COL + " TEXT PRIMARY KEY, " + PURCHASED_QUANTITY_COL + " INTEGER)");
            }
        }

    }
}