using System.Runtime.InteropServices;
using System.Windows;

namespace PawnManager
{
    public static class SavTool
    {
        const string DLLName = "DDsavelib.dll";

        static string[] Errors =
        {
            null,
            "Unable to read file",
            "Unable to write to file",
            "Invalid format",
            "Unpacking error",
            "Unknown error"
        };

        private static string CodeToMessage(int err)
        {
            if (err < 0 || err > 5)
                err = 5;
            return Errors[err];
        }

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Unpack([MarshalAs(UnmanagedType.LPStr)] string path);

        public static bool UnpackSav(string savPath)
        {
            int code = 0;
            try
            {
                code = Unpack(savPath);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    string.Format("Error while trying to unpack using DDsavetool:\n{0}", ex.Message),
                    "DDsavetool error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            if (code != 0)
            {
                MessageBox.Show(
                    CodeToMessage(code),
                    "Error unpacking .sav",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            return code == 0;
        }

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Repack([MarshalAs(UnmanagedType.LPStr)] string path);

        public static bool RepackSav(string savPath)
        {
            int code = 0;
            try
            {
                code = Repack(savPath);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    string.Format("Error while trying to repack using DDsavetool:\n{0}", ex.Message),
                    "DDsavetool error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            if (code != 0)
            {
                MessageBox.Show(
                    CodeToMessage(code),
                    "Error repacking .sav",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            return code == 0;
        }

        [DllImport(DLLName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int Validate([MarshalAs(UnmanagedType.LPStr)] string path);

        public static string ValidateSav(string savPath)
        {
            int code = 0;
            try
            {
                code = Validate(savPath);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    string.Format("Error while trying to validate a .sav using DDsavetool:\n{0}", ex.Message),
                    "DDsavetool error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return "DLL error";
            }
            return CodeToMessage(code);
        }
    }
}
