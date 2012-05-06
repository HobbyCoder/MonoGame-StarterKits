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
    public class CatalogAdapter : ArrayAdapter<string>
    {
        public CatalogAdapter(Context context)
            : base(context, 0)
        {

        }
    }
}