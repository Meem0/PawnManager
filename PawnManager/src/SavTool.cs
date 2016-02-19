using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PawnManager
{
    public static class SavTool
    {
        const int AllocSize = 25 * 1024 * 1024;
        const string DLLName = "DDsavelib.dll";

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Unpack([MarshalAs(UnmanagedType.LPStr)]string savPath, IntPtr unpackedSavPtr);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Repack([MarshalAs(UnmanagedType.LPStr)]string outputPath,
                                            [MarshalAs(UnmanagedType.LPStr)]string xmlData,
                                            uint dataSize);

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Validate([MarshalAs(UnmanagedType.LPStr)]string savPath);

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

        /// <summary>
        /// Writes a packed .sav file, given the unpacked XML text.
        /// May throw an exception from accessing the DLL, or if repacking failed.
        /// </summary>
        /// <param name="savPath">The path to the file to write</param>
        /// <param name="savText">The unpacked XML</param>
        public static void RepackSav(string savPath, string savText)
        {
            int code = 0;
            try
            {
                code = Repack(savPath, savText, (uint)savText.Length);
            }
            catch (Exception ex)
            {
                ThrowDDsavelibException(ex);
            }
            if (code != 0)
            {
                throw new Exception(CodeToMessage(code));
            }
        }

        /// <summary>
        /// Gets the unpacked XML from a packed .sav file.
        /// May throw an exception from accessing the DLL, or if unpacking failed.
        /// </summary>
        /// <param name="savPath">The path to the .sav file</param>
        /// <returns>The unpacked XML of the .sav file</returns>
        public static string UnpackSav(string savPath)
        {
            int code = 0;
            string unpackedText = "";

            {
                IntPtr output = Marshal.AllocHGlobal(AllocSize);
                try
                {
                    code = Unpack(savPath, output);
                    if (code == 0)
                    {
                        unpackedText = Marshal.PtrToStringAnsi(output);
                    }
                }
                catch (Exception ex)
                {
                    ThrowDDsavelibException(ex);
                }
                finally
                {
                    Marshal.FreeHGlobal(output);
                }
            }

            if (code != 0)
            {
                throw new Exception(CodeToMessage(code));
            }
            
            return unpackedText;
        }

        /// <summary>
        /// Checks if a file is a valid packed DDDA .sav file.
        /// May throw an exception from accessing the DLL.
        /// </summary>
        /// <param name="savPath">The path to the .sav file</param>
        /// <returns>True if the file is a valid packed DDDA .sav file</returns>
        public static bool ValidateSav(string savPath)
        {
            int errorCode = 0;
            try
            {
                errorCode = Validate(savPath);
            }
            catch (Exception ex)
            {
                ThrowDDsavelibException(ex);
            }
            return errorCode == 0;
        }

        private static void ThrowDDsavelibException(Exception ex)
        {
            throw new Exception(string.Format(
                "DDsavelib error:\n{0}\n" +
                "Ensure {1} is in the same folder as the executable.",
                ex.Message,
                DLLName),
                ex);
        }
    }
}
