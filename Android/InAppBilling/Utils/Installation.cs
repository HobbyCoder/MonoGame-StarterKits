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
using Java.IO;
using Java.Lang;
using Java.Util;

namespace com.example.dungeons
{
    public class Installation
    {
        private static string INSTALLATION = "INSTALLATION";
	    private static string sID = null;

        public static string id(Context context)
        {
            if (sID == null)
            {
                File installation = new File(context.FilesDir, INSTALLATION);
                try
                {
                    if (!installation.Exists())
                    {
                        writeInstallationFile(installation);
                    }
                    sID = readInstallationFile(installation);
                }
                catch (Java.Lang.Exception e)
                {
                    throw new RuntimeException(e);
                }
            }
            return sID;
        }

    private static string readInstallationFile(File installation) {
        RandomAccessFile f = new RandomAccessFile(installation, "r");
        byte[] bytes = new byte[(int) f.Length()];
        f.ReadFully(bytes);
        f.Close();
        System.Text.UTF8Encoding en = new UTF8Encoding();
        return en.GetString(bytes); 
    }

    private static void writeInstallationFile(File installation)  {
        FileOutputStream outt = new FileOutputStream(installation);
        string id = UUID.RandomUUID().ToString();
        System.Text.UTF8Encoding en = new UTF8Encoding();
        outt.Write(en.GetBytes(id));
        outt.Close();
    }
    }
}