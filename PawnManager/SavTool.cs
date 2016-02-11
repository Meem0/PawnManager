using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Xml.Linq;

namespace PawnManager
{
    public static class SavTool
    {
        const int AllocSize = 25 * 1024 * 1024;
        const string DLLName = "DDsavelib.dll";

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Unpack([MarshalAs(UnmanagedType.LPStr)] string path, IntPtr output);
        
        static Dictionary<int, string> Errors = new Dictionary<int, string>
        {
            { 1, "Unable to read file" },
            { 2, "Unable to write to file" },
            { 3, "Invalid format" },
            { 4, "Unpacking error" },
            { -2, "EZ stream error" },
            { -3, "EZ data error" },
            { -4, "EZ memory error" },
            { -5, "EZ buffer error" }
        };

        private static string CodeToMessage(int err)
        {
            string ret = "";
            if (!Errors.TryGetValue(err, out ret))
                ret = "Unknown error";
            return ret;
        }

        public static XElement UnpackSav(string savPath)
        {
            bool isError = false;
            int code = 0;
            XElement unpackedSav = null;

            {
                IntPtr output = Marshal.AllocHGlobal(AllocSize);
                try
                {
                    code = Unpack(savPath, output);
                    if (code == 0)
                    {
                        string unpackedText = Marshal.PtrToStringAnsi(output);
                        unpackedSav = XElement.Parse(unpackedText);
                    }
                }
                catch (System.Xml.XmlException ex)
                {
                    isError = true;
                    MessageBox.Show(
                        string.Format("Error while parsing unpacked .sav as XML:\n{0}", ex.Message),
                        "XML error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    isError = true;
                    MessageBox.Show(
                        string.Format("Error while trying to unpack using DDsavetool:\n{0}", ex.Message),
                        "DDsavetool error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    Marshal.FreeHGlobal(output);
                }
            }

            if (!isError && code != 0)
            {
                MessageBox.Show(
                    CodeToMessage(code),
                    "Error unpacking .sav",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                isError = true;
            }

            return unpackedSav;
        }
    }
}
