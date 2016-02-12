using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace DDsavelibTest
{
    class Program
    {
        const int AllocSize = 30 * 1024 * 1024;
        const string Path = "../notes/input.sav";

        [DllImport("DDsavelib.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Unpack([MarshalAs(UnmanagedType.LPStr)]string savPath, IntPtr unpackedSavPtr);

        [DllImport("DDsavelib.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Repack([MarshalAs(UnmanagedType.LPStr)]string outputPath,
                                         [MarshalAs(UnmanagedType.LPStr)]string xmlData,
                                         uint dataSize);

        [DllImport("DDsavelib.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Validate([MarshalAs(UnmanagedType.LPStr)]string savPath);

        static void TestValidate(string path)
        {
            int result = Validate(path);
            Console.WriteLine("Validate result: {0}", result);
        }

        static XElement TestUnpack(string path, out string xmlText)
        {
            IntPtr unpackedSavPtr = Marshal.AllocHGlobal(AllocSize);
            int result = Unpack(path, unpackedSavPtr);

            Console.WriteLine("Unpack result: {0}", result);

            xmlText = Marshal.PtrToStringAnsi(unpackedSavPtr);
            
            XElement root = XElement.Parse(xmlText, LoadOptions.PreserveWhitespace);
            
            return root;
        }

        static void TestRepack(string path, string savText)
        {
            int result = Repack(path, savText, (uint)savText.Length);
            Console.WriteLine("Repack result: {0}", result);
        }

        static void Main(string[] args)
        {
            string xmlText;
            XElement unpacked = TestUnpack(Path, out xmlText);
            string outputPath = Path + "_unpacked.xml";
            File.WriteAllText(outputPath, xmlText);
            
            if (unpacked != null)
            {
                string repackPath = Path + "_repacked.sav";
                TestRepack(repackPath, xmlText);
                XElement unpacked2 = TestUnpack(repackPath, out xmlText);
                outputPath = repackPath + "_unpackedFromXML.xml";
                unpacked2.Save(outputPath, SaveOptions.DisableFormatting);
                File.WriteAllText(outputPath, xmlText);
            }

            /*string dllPath = Path + "_dll.xml";
            string savDLL = File.ReadAllText(dllPath);
            string xmlPath = Path + "_repacked.sav_unpackedFromXML.xml";
            string savFromXML = File.ReadAllText(xmlPath);
            Console.WriteLine("DLL: {0}\nXML: {1}", savDLL.GetHashCode(), savFromXML.GetHashCode());

            TestRepack(dllPath + "_repacked.sav", savDLL);
            TestRepack(xmlPath + "_repacked.sav", savFromXML);
            
            outputs were identical

            */
        }
    }
}
