using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace DDsavelibTest
{
    class Program
    {
        const int AllocSize = 30 * 1024 * 1024;
        const string Path = "../test/";

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
            char flag = '\0';
            string file = "";
            while (flag != 'x')
            {
                Console.Write("(u)npack / (r)epack, then file: ");
                flag = (char)Console.Read();
                Console.WriteLine();
                file = Console.ReadLine().Trim();
                string filePath = Path + file;

                switch (flag)
                {
                    case 'u':
                        {
                            string unpackedText;
                            XElement unpackedXml = TestUnpack(filePath, out unpackedText);
                            unpackedXml.Save(filePath + ".xml");
                            File.WriteAllText(filePath + "_txt.xml", unpackedText);
                        }
                        break;
                    case 'r':
                        {
                            string packedText = File.ReadAllText(filePath);
                            TestRepack(filePath + ".sav", packedText);
                        }
                        break;
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }
}
