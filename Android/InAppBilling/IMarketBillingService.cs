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


    public interface IMarketBillingService
    {
        /// <summary>
        /// Given the arguments in bundle form, returns a bundle for results. </summary>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: Bundle sendBillingRequest(Bundle bundle) throws android.os.RemoteException;
        Bundle sendBillingRequest(Bundle bundle);
    }
}