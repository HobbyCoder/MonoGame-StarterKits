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
    public interface ISignatureValidator
    {

        /**
         * Validates that the specified signature matches the computed signature on
         * the specified signed data. Returns true if the data is correctly signed.
         * 
         * @param signedData
         *            signed data
         * @param signature
         *            signature
         * @return true if the data and signature match, false otherwise.
         */
        bool validate(string signedData, string signature);

    }
}