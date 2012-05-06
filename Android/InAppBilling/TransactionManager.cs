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
using Android.Database;

namespace com.example.dungeons
{
    using PurchaseState = Consts.PurchaseState;
    
    public class TransactionManager
    {

        public static void addTransaction(Context context, Transaction transaction)
        {
            BillingDB db = new BillingDB(context);
            db.insert(transaction);
            db.close();
        }

        public static bool isPurchased(Context context, String itemId)
        {
            return countPurchases(context, itemId) > 0;
        }

        public static int countPurchases(Context context, String itemId)
        {
            BillingDB db = new BillingDB(context);
            ICursor c = db.queryTransactions(itemId, PurchaseState.PURCHASED);
            int count = 0;
            if (c != null)
            {
                count = c.Count;
                c.Close();
            }
            db.close();
            return count;
        }

        public static List<Transaction> getTransactions(Context context)
        {
            BillingDB db = new BillingDB(context);
            ICursor c = db.queryTransactions();
            List<Transaction> transactions = cursorToList(c);
            db.close();
            return transactions;
        }

        private static List<Transaction> cursorToList(ICursor c)
        {
            List<Transaction> transactions = new List<Transaction>();
            if (c != null)
            {
                while (c.MoveToNext())
                {
                    Transaction purchase = BillingDB.createTransaction(c);
                    transactions.Add(purchase);
                }
                c.Close();
            }
            return transactions;
        }

        public static List<Transaction> getTransactions(Context context, String itemId)
        {
            BillingDB db = new BillingDB(context);
            ICursor c = db.queryTransactions(itemId);
            List<Transaction> transactions = cursorToList(c);
            db.close();
            return transactions;
        }

    }
}