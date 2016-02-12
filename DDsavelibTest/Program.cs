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

        static XElement TestUnpack(string path)
        {
            IntPtr unpackedSavPtr = Marshal.AllocHGlobal(AllocSize);
            int result = Unpack(path, unpackedSavPtr);

            Console.WriteLine("Unpack result: {0}", result);

            string unpackedSav = Marshal.PtrToStringAnsi(unpackedSavPtr);


            XElement root = XElement.Parse(unpackedSav, LoadOptions.PreserveWhitespace);
            /*int itrs = 0;
            int maxItrs = 50;
            foreach (XElement current in root.Descendants())
            {
                Console.Write(current.Name);
                foreach (XAttribute attribute in current.Attributes())
                {
                    Console.Write("{0}={1} ", attribute.Name, attribute.Value);
                    ++itrs;
                }
                Console.WriteLine();

                if (itrs > maxItrs)
                    break;
            }*/
            
            return root;
        }

        static void TestRepack(string path, XElement savRoot)
        {
            string str = savRoot.ToString(SaveOptions.DisableFormatting);
            int result = Repack(path, str, (uint)str.Length);
            Console.WriteLine("Repack result: {0}", result);
        }

        static void Main(string[] args)
        {
            XElement unpacked = TestUnpack(Path);
            string outputPath = Path + "_unpacked.xml";
            File.WriteAllText(outputPath, unpacked.ToString(SaveOptions.DisableFormatting).Replace("\r", ""));

            //Console.ReadKey();

            if (unpacked != null)
            {
                string repackPath = Path + "_repacked.sav";
                TestRepack(repackPath, unpacked);
                //Console.ReadKey();
                XElement unpacked2 = TestUnpack(repackPath);
                unpacked2.Save(repackPath + "_unpacked.xml", SaveOptions.DisableFormatting);
            }

            //Console.ReadKey();
        }
    }
}
