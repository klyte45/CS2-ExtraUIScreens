using Belzont.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Belzont.Utils
{
    public static class KResourceLoader
    {
        public static Assembly RefAssemblyMod => BasicIMod.Instance.GetType().Assembly;
        private static string NamespaceMod => $"{BasicIMod.Instance.SafeName}.";
        public static Assembly RefAssemblyBelzont => typeof(KResourceLoader).Assembly;

        public static byte[] LoadResourceDataMod(string name) => LoadResourceData(NamespaceMod + name, RefAssemblyMod);
        public static byte[] LoadResourceDataBelzont(string name) => LoadResourceData("Belzont." + name, RefAssemblyBelzont);
        private static byte[] LoadResourceData(string name, Assembly refAssembly)
        {
            var stream = (UnmanagedMemoryStream)refAssembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                LogUtils.DoLog("Could not find resource: " + name);
                return null;
            }

            var read = new BinaryReader(stream);
            return read.ReadBytes((int)stream.Length);
        }

        public static string LoadResourceStringMod(string name) => LoadResourceString(NamespaceMod + name, RefAssemblyMod);
        public static string LoadResourceStringBelzont(string name) => LoadResourceString("Belzont." + name, RefAssemblyBelzont);
        private static string LoadResourceString(string name, Assembly refAssembly)
        {
            var stream = (UnmanagedMemoryStream)refAssembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                LogUtils.DoLog("Could not find resource: " + name);
                return null;
            }

            var read = new StreamReader(stream);
            return read.ReadToEnd();
        }
        public static IEnumerable<string> LoadResourceStringLinesMod(string name) => LoadResourceStringLines(NamespaceMod + name, RefAssemblyMod);
        public static IEnumerable<string> LoadResourceStringLinesBelzont(string name) => LoadResourceStringLines("Belzont." + name, RefAssemblyBelzont);
        private static IEnumerable<string> LoadResourceStringLines(string name, Assembly refAssembly)
        {
            using (var stream = (UnmanagedMemoryStream)refAssembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    LogUtils.DoLog("Could not find resource: " + name);
                    yield break;
                }

                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line;
                    }
                }
            }
        }
    }
}
