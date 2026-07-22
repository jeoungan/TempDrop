using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace TempDrop.Core
{
    internal static class JsonStore
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static T Read<T>(string path) where T : class
        {
            if (!File.Exists(path))
            {
                return null;
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            return Serializer.Deserialize<T>(json);
        }

        public static void WriteAtomic<T>(string path, T value)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("The JSON path has no parent directory.");
            }

            Directory.CreateDirectory(directory);
            string temporaryPath = path + ".tmp";
            string json = Serializer.Serialize(value);
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }

        public static string Serialize(object value)
        {
            return Serializer.Serialize(value);
        }
    }
}
