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
using Android.Database.Sqlite;
using Android.Database;

namespace com.example.dungeons
{
    using PurchaseState = Consts.PurchaseState;

    public class BillingDB
    {

        static string DATABASE_NAME = "billing.db";
        static int DATABASE_VERSION = 1;
        static string TABLE_TRANSACTIONS = "purchases";

        public static string COLUMN__ID = "_id";
        public static string COLUMN_STATE = "state";
        public static string COLUMN_PRODUCT_ID = "productId";
        public static string COLUMN_PURCHASE_TIME = "purchaseTime";
        public static string COLUMN_DEVELOPER_PAYLOAD = "developerPayload";

        private static String[] TABLE_TRANSACTIONS_COLUMNS = {
    	COLUMN__ID, COLUMN_PRODUCT_ID, COLUMN_STATE,
    	COLUMN_PURCHASE_TIME, COLUMN_DEVELOPER_PAYLOAD
    };

        SQLiteDatabase mDb;
        private DatabaseHelper mDatabaseHelper;

        public BillingDB(Context context)
        {
            mDatabaseHelper = new DatabaseHelper(context);
            mDb = mDatabaseHelper.WritableDatabase;
        }

        public void close()
        {
            mDatabaseHelper.Close();
        }

        public void insert(Transaction transaction)
        {
            ContentValues values = new ContentValues();
            values.Put(COLUMN__ID, transaction.orderId);
            values.Put(COLUMN_PRODUCT_ID, transaction.productId);
            values.Put(COLUMN_STATE, transaction.purchaseState.ToString());
            values.Put(COLUMN_PURCHASE_TIME, transaction.purchaseTime);
            values.Put(COLUMN_DEVELOPER_PAYLOAD, transaction.developerPayload);
            mDb.Replace(TABLE_TRANSACTIONS, null /* nullColumnHack */, values);
        }

        public ICursor queryTransactions()
        {
            return mDb.Query(TABLE_TRANSACTIONS, TABLE_TRANSACTIONS_COLUMNS, null,
                    null, null, null, null);
        }

        public ICursor queryTransactions(String productId)
        {
            return mDb.Query(TABLE_TRANSACTIONS, TABLE_TRANSACTIONS_COLUMNS, COLUMN_PRODUCT_ID + " = ?",
                    new String[] { productId }, null, null, null);
        }

        public ICursor queryTransactions(String productId, PurchaseState state)
        {
            return mDb.Query(TABLE_TRANSACTIONS, TABLE_TRANSACTIONS_COLUMNS, COLUMN_PRODUCT_ID + " = ? AND " + COLUMN_STATE + " = ?",
                    new String[] { productId, Convert.ToString(state.ToString()) }, null, null, null);
        }

        public static Transaction createTransaction(ICursor cursor)
        {
            Transaction purchase = new Transaction();
            purchase.orderId = cursor.GetString(0);
            purchase.productId = cursor.GetString(1);
            purchase.purchaseState = (PurchaseState)Enum.Parse(typeof(PurchaseState), cursor.GetInt(2).ToString());
            purchase.purchaseTime = cursor.GetLong(3);
            purchase.developerPayload = cursor.GetString(4);
            return purchase;
        }

        private class DatabaseHelper : SQLiteOpenHelper
        {
            public DatabaseHelper(Context context) :
                base(context, DATABASE_NAME, null, DATABASE_VERSION)
            {
            }

            public override void OnCreate(SQLiteDatabase db)
            {
                createTransactionsTable(db);
            }

            private void createTransactionsTable(SQLiteDatabase db)
            {
                db.ExecSQL("CREATE TABLE " + TABLE_TRANSACTIONS + "(" +
                        COLUMN__ID + " TEXT PRIMARY KEY, " +
                        COLUMN_PRODUCT_ID + " INTEGER, " +
                        COLUMN_STATE + " TEXT, " +
                        COLUMN_PURCHASE_TIME + " TEXT, " +
                        COLUMN_DEVELOPER_PAYLOAD + " INTEGER)");
            }


            public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) { }
        }
    }
}