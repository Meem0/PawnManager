using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace DDsavelibTest
{
    class Program
    {
        const int ALLOC_SIZE = 30 * 1024 * 1024;

        [DllImport("DDsavelib.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Unpack([MarshalAs(UnmanagedType.LPStr)]string savPath, IntPtr unpackedSavPtr);
        
        static void Main(string[] args)
        {
            int result = 0;
            IntPtr unpackedSavPtr = Marshal.AllocHGlobal(ALLOC_SIZE);
            result = UnpackTest("Input.sav", unpackedSavPtr);

            string unpackedSav = Marshal.PtrToStringAnsi(unpackedSavPtr);

            Console.WriteLine(unpackedSav);

            /*XElement root = XElement.Parse(unpackedSav);
            Console.WriteLine(root.Name);*/

            Console.ReadKey();
        }
    }
}
