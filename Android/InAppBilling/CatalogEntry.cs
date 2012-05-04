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
    public class CatalogEntry
    {

        /**
         * Each product in the catalog is either MANAGED or UNMANAGED. MANAGED means
         * that the product can be purchased only once per user (such as a new level
         * in a game). The purchase is remembered by Android Market and can be
         * restored if this application is uninstalled and then re-installed.
         * UNMANAGED is used for products that can be used up and purchased multiple
         * times (such as poker chips). It is up to the application to keep track of
         * UNMANAGED products for the user.
         */
        public enum Managed
        {
            MANAGED, UNMANAGED
        }

        public String sku;
        public int nameId;
        public Managed managed;

        public CatalogEntry(String sku, int nameId, Managed managed)
        {
            this.sku = sku;
            this.nameId = nameId;
            this.managed = managed;
        }

        /** An array of product list entries for the products that can be purchased. */
        public CatalogEntry[] CATALOG = new CatalogEntry[] {
            new CatalogEntry("sword_001", 1, Managed.MANAGED)
            //new CatalogEntry("sword_001", Android.Resource.String.two_handed_sword, Managed.MANAGED),
            //new CatalogEntry("potion_001", Android.Resource.String.potions, Managed.UNMANAGED),
            //new CatalogEntry("android.test.purchased", Android.Resource.String.android_test_purchased, Managed.UNMANAGED),
            //new CatalogEntry("android.test.canceled", Android.Resource.String.android_test_canceled, Managed.UNMANAGED),
            //new CatalogEntry("android.test.refunded", Android.Resource.String.android_test_refunded, Managed.UNMANAGED),
            //new CatalogEntry("android.test.item_unavailable", Android.Resource.String.android_test_item_unavailable, Managed.UNMANAGED), 
    };
    }
}