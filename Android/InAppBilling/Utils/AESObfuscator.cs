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
using Java.Lang;
using Java.IO;
using Javax.Crypto;
using Javax.Crypto.Spec;

/**
 * An obfuscator that uses AES to encrypt data.
 */
public class AESObfuscator {
    private static  string UTF8 = "UTF-8";
    private static  string KEYGEN_ALGORITHM = "PBEWITHSHAAND256BITAES-CBC-BC";
    private static  string CIPHER_ALGORITHM = "AES/CBC/PKCS5Padding";
    private static  byte[] IV =
        { 16, 74, 71, 80, 32, 101, 47, 72, 117, 14, 0, 29, 70, 65, 12, 74 };
    private static  string header = "net.robotmedia.billing.utils.AESObfuscator-1|";

    private Cipher mEncryptor;
    private Cipher mDecryptor;

    public AESObfuscator(byte[] salt, string password) {
        try {
            SecretKeyFactory factory = SecretKeyFactory.GetInstance(KEYGEN_ALGORITHM);
            PBEKeySpec keySpec =
                new PBEKeySpec(password.ToCharArray(), salt, 1024, 256);
            ISecretKey tmp = factory.GenerateSecret(keySpec);
            ISecretKey secret = new SecretKeySpec(tmp.GetEncoded(), "AES");
            mEncryptor = Cipher.GetInstance(CIPHER_ALGORITHM);
            mEncryptor.Init(Cipher.EncryptMode, secret, new IvParameterSpec(IV));
            mDecryptor = Cipher.GetInstance(CIPHER_ALGORITHM);
            mDecryptor.Init(Cipher.DecryptMode, secret, new IvParameterSpec(IV));
        } catch (GeneralSecurityException e) {
            // This can't happen on a compatible Android device.
            throw new RuntimeException("Invalid environment", e);
        }
    }

    public string obfuscate(string original) {
        //if (original == null)
        //{
        //    return null;
        //}
        //try
        //{
        //     Header is appended as an integrity check
        //    return Base64.encode(mEncryptor.DoFinal((header + original).GetBytes(UTF8)));
        //}
        //catch (UnsupportedEncodingException e)
        //{
        //    throw new RuntimeException("Invalid environment", e);
        //}
        //catch (GeneralSecurityException e)
        //{
        //    throw new RuntimeException("Invalid environment", e);
        //}
        //TODO: implement
        return original;
    }

    public string unobfuscate(string obfuscated) {
        return obfuscated;
        //if (obfuscated == null)
        //{
        //    return null;
        //}
        //try
        //{
        //    string result = new string(mDecryptor.DoFinal(Base64.decode(obfuscated)), UTF8);
        //    // Check for presence of header. This serves as a final integrity check, for cases
        //    // where the block size is correct during decryption.
        //    int headerIndex = result.IndexOf(header);
        //    if (headerIndex != 0)
        //    {
        //        throw new ValidationException("Header not found (invalid data or key)" + ":" +
        //                obfuscated);
        //    }
        //    return result.Substring(header.Length, result.Length);
        //}
        //catch (Base64DecoderException e)
        //{
        //    throw new ValidationException(e.Message + ":" + obfuscated);
        //}
        //catch (IllegalBlockSizeException e)
        //{
        //    throw new ValidationException(e.Message + ":" + obfuscated);
        //}
        //catch (BadPaddingException e)
        //{
        //    throw new ValidationException(e.Message + ":" + obfuscated);
        //}
        //catch (UnsupportedEncodingException e)
        //{
        //    throw new RuntimeException("Invalid environment", e);
        //}
    }
    
    public class ValidationException : Java.Lang.Exception {
        public ValidationException() {
            
          //super();
        }

        public ValidationException(string s) {
          //super(s);
           
        }

        private static long serialVersionUID = 1L;
    }
    
}