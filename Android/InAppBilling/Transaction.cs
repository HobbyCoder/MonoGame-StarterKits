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
using System.Json;


namespace InAppBilling
{
    using PurchaseState = Consts.PurchaseState;

    public class Transaction
    {

        static string DEVELOPER_PAYLOAD = "developerPayload";
        static string NOTIFICATION_ID = "notificationId";
        static string ORDER_ID = "orderId";
        static string PACKAGE_NAME = "packageName";
        static string PRODUCT_ID = "productId";
        static string PURCHASE_STATE = "purchaseState";

        static string PURCHASE_TIME = "purchaseTime";



        public static Transaction parse(JsonObject json)
        {
            Transaction transaction = new Transaction();
            int response = int.Parse(json[PURCHASE_STATE]);
            transaction.purchaseState = (PurchaseState)Enum.Parse(typeof(PurchaseState), json[response]);
            transaction.productId = json[PRODUCT_ID];
            transaction.packageName = json[PACKAGE_NAME];
            transaction.purchaseTime = json[PURCHASE_TIME];
            transaction.orderId = json[ORDER_ID];
            transaction.notificationId = json[NOTIFICATION_ID];
            transaction.developerPayload = json[DEVELOPER_PAYLOAD];
            return transaction;
        }

        public String developerPayload;
        public String notificationId;
        public String orderId;
        public String packageName;
        public String productId;
        public PurchaseState purchaseState;
        public long purchaseTime;

        public Transaction() { }

        public Transaction(String orderId, String productId, String packageName, PurchaseState purchaseState,
                String notificationId, long purchaseTime, String developerPayload)
        {
            this.orderId = orderId;
            this.productId = productId;
            this.packageName = packageName;
            this.purchaseState = purchaseState;
            this.notificationId = notificationId;
            this.purchaseTime = purchaseTime;
            this.developerPayload = developerPayload;
        }

        public Transaction clone()
        {
            return new Transaction(orderId, productId, packageName, purchaseState, notificationId, purchaseTime, developerPayload);
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            //TO DO: what is the c# for this?
            //if (getClass() != obj.getClass())
            //    return false;
            Transaction other = (Transaction)obj;
            if (developerPayload == null)
            {
                if (other.developerPayload != null)
                    return false;
            }
            else if (!developerPayload.Equals(other.developerPayload))
                return false;
            if (notificationId == null)
            {
                if (other.notificationId != null)
                    return false;
            }
            else if (!notificationId.Equals(other.notificationId))
                return false;
            if (orderId == null)
            {
                if (other.orderId != null)
                    return false;
            }
            else if (!orderId.Equals(other.orderId))
                return false;
            if (packageName == null)
            {
                if (other.packageName != null)
                    return false;
            }
            else if (!packageName.Equals(other.packageName))
                return false;
            if (productId == null)
            {
                if (other.productId != null)
                    return false;
            }
            else if (!productId.Equals(other.productId))
                return false;
            if (purchaseState != other.purchaseState)
                return false;
            if (purchaseTime != other.purchaseTime)
                return false;
            return true;
        }

        public override String ToString()
        {
            return orderId.ToString();
        }

    }
}
