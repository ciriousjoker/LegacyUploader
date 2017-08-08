using SharpConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyManagerUploader
{
    static class Ini
    {
        static string INI_FILENAME = Path.Combine(Environment.CurrentDirectory, "settings.ini");
        static string INI_SECTION = "Settings";

        // KEYS

        // Local folders
        public static string KEY_LOCAL_FOLDER = "LocalFolder";
        public static string KEY_LOCAL_TEMP_FOLDER = "LocalTempFolder";
        public static string KEY_ENCRYPTION_PASSWORD = "EncryptionPassword";

        // Google Drive Ids
        public static string KEY_DRIVE_ROOT = "DriveRoot";              //  The application's main folder
        public static string KEY_DRIVE_ID_JSON = "DriveIdJson";         //  The JsonTable
        public static string KEY_DRIVE_ID_PACKAGES = "DriveIdPackages"; //  An array of all the package

        // Messagebox Flags
        public static string KEY_FLAG_TEMP_DATA_NOTICE = "flagTempData";//  A notification shown when the user chooses a temp folder for the first time.


        static Configuration cfg;
        static Section Settings;

        public static void create()
        {
            cfg = new Configuration();

            // Set default values
            cfg[INI_SECTION][KEY_DRIVE_ROOT].StringValue = "";

            // Overwrite with existing .ini
            if (File.Exists(INI_FILENAME))
            {
                cfg = Configuration.LoadFromFile(INI_FILENAME);
            }

            Settings = cfg[INI_SECTION];
            cfg.SaveToFile(INI_FILENAME);
        }

        // Writing
        public static void put(string key, string value)
        {
            Settings[key].StringValue = value;
        }

        public static void write(string key, string value)
        {
            Settings[key].StringValue = value;
            apply();
        }

        public static void writeArray(string key, string[] value_array)
        {
            Settings[key].StringValueArray = value_array;
            apply();
        }

        public static void setFlag(string key)
        {
            Settings[key].StringValue = "set";
            apply();
        }

        // Reading
        public static string read(string key)
        {
            return Settings[key].StringValue;
        }

        public static bool hasFlag(string key)
        {
            if(String.IsNullOrEmpty(Settings[key].StringValue))
            {
                return false;
            }
            return true;
        }

        // TODO: Remove this at some point, it's kind of useless
        public static string read(string key, string default_string)
        {
            string result = read(key);
            if(String.IsNullOrEmpty(result))
            {
                return default_string;
            }
            return result;
        }

        public static string[] readArray(string key)
        {
            if(Settings[key].ArraySize == -1)
            {
                return null;
            }
            return Settings[key].StringValueArray;
        }

        public static void apply()
        {
            cfg.SaveToFile(INI_FILENAME);
        }
    }
}
