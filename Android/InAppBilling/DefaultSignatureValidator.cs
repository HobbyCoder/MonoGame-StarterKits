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
using Java.Security;
using Android.Util;
using Java.Security.Spec;
using Java.Lang;
using Android.Text;

namespace InAppBilling
{

public class DefaultSignatureValidator : ISignatureValidator {

	protected static string KEY_FACTORY_ALGORITHM = "RSA";
	protected static string SIGNATURE_ALGORITHM = "SHA1withRSA";

	/**
	 * Generates a PublicKey instance from a string containing the
	 * Base64-encoded public key.
	 * 
	 * @param encodedPublicKey
	 *            Base64-encoded public key
	 * @throws IllegalArgumentException
	 *             if encodedPublicKey is invalid
	 */
	protected IPublicKey generatePublicKey(string encodedPublicKey) {
		try {
			//TO DO: base 64 decode
            byte[] decodedKey = Base64.Decode(encodedPublicKey, 0);
            //byte[] decodedKey = Base64.Decode(encodedPublicKey);
			KeyFactory keyFactory = KeyFactory.GetInstance(KEY_FACTORY_ALGORITHM);
			return keyFactory.GeneratePublic(new X509EncodedKeySpec(decodedKey));
		} catch (NoSuchAlgorithmException e) {
			throw new RuntimeException(e);
		} catch (InvalidKeySpecException e) {
			Log.Error(BillingController.LOG_TAG, "Invalid key specification.");
			throw new IllegalArgumentException(e);
		} catch (Base64DecoderException e) {
			Log.Error(BillingController.LOG_TAG, "Base64 decoding failed.");
			//throw new IllegalArgumentException(e);
            throw new System.Exception("Base 64 decoding has failed.");
		}
	}

	private BillingController.IConfiguration configuration;

	public DefaultSignatureValidator(BillingController.IConfiguration configuration) {
		this.configuration = configuration;
	}

	protected bool validate(IPublicKey publicKey, string signedData, string signature) {
		Signature sig;
		try {
			sig = Signature.GetInstance(SIGNATURE_ALGORITHM);
			sig.InitVerify(publicKey);
            System.Text.UTF8Encoding en = new UTF8Encoding();
			sig.Update(en.GetBytes(signedData));
			if (!sig.Verify(Base64.Decode(signature, 0))) {
				Log.Error(BillingController.LOG_TAG, "Signature verification failed.");
				return false;
			}
			return true;
		} catch (NoSuchAlgorithmException e) {
			Log.Error(BillingController.LOG_TAG, "NoSuchAlgorithmException");
		} catch (InvalidKeyException e) {
			Log.Error(BillingController.LOG_TAG, "Invalid key specification");
		} catch (SignatureException e) {
			Log.Error(BillingController.LOG_TAG, "Signature exception");
		} catch (Base64DecoderException e) {
			Log.Error(BillingController.LOG_TAG, "Base64 decoding failed");
		}
		return false;
	}


	public bool validate(string signedData, string signature) {
        string publicKey;
		if (configuration == null || TextUtils.IsEmpty(publicKey = configuration.getPublicKey())) {
			Log.Warn(BillingController.LOG_TAG, "Please set the public key or turn on debug mode");
			return false;
		}
		if (signedData == null) {
			Log.Error(BillingController.LOG_TAG, "Data is null");
			return false;
		}
		IPublicKey key = generatePublicKey(publicKey);
		return validate(key, signedData, signature);
	}

}
}