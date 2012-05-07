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

namespace InAppBilling
{
    public class CatalogAdapter : ArrayAdapter<string>
    {
        private CatalogEntry[] mCatalog;
        private List<string> mOwnedItems = new List<string>();

        public CatalogAdapter(Context context, CatalogEntry[] catalog)
            : base(context, Android.Resource.Layout.SimpleSpinnerItem)
        {
            mCatalog = catalog;
            foreach (CatalogEntry element in catalog)
            {
                Add(context.GetString(element.nameId));
            }
            SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        }

        public override bool AreAllItemsEnabled()
        {
            return false;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            View view = base.GetDropDownView(position, convertView, parent);
            view.Enabled = IsEnabled(position);
            return view;
        }




        private bool isPurchased(string sku)
        {
            for (int i = 0; i < mOwnedItems.Count(); i++)
            {
                if (sku.Equals(mOwnedItems[i]))
                {
                    return true;
                }
            }
            return false;
        }


        public override bool IsEnabled(int position)
        {
            // If the item at the given list position is not purchasable,
            // then prevent the list item from being selected.
            CatalogEntry entry = mCatalog[position];
            if (entry.managed == InAppBilling.CatalogEntry.Managed.MANAGED && isPurchased(entry.sku))
            {
                return false;
            }
            return true;
        }

        public void setOwnedItems(List<String> ownedItems)
        {
            mOwnedItems = ownedItems;
            NotifyDataSetChanged();
        }

    }
}