using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace TouchenceSample
{
    class CommonFunctions
    {
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // UNIXエポックを表すDateTimeオブジェクトを取得
        private static DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public static long GetUnixTimeWithMillisecond(DateTime targetTime)
        {
            // UTC時間に変換
            targetTime = targetTime.ToUniversalTime();

            // UNIXエポックからの経過時間を取得
            TimeSpan elapsedTime = targetTime - UNIX_EPOCH;

            // 経過秒数に変換
            return (long)elapsedTime.TotalMilliseconds;
        }

        public static long GetUnixTimeWithSecond(DateTime targetTime)
        {
            // UTC時間に変換
            targetTime = targetTime.ToUniversalTime();

            // UNIXエポックからの経過時間を取得
            TimeSpan elapsedTime = targetTime - UNIX_EPOCH;

            // 経過秒数に変換
            return (long)(elapsedTime.TotalSeconds) * 1000;
        }

        public static bool IsNumeric(string targetString)
        {
            double dNullable;

            return double.TryParse(
                targetString,
                System.Globalization.NumberStyles.Any,
                null,
                out dNullable
            );
        }

        public static bool IsNumericInt(object oTarget)
        {
            return IsNumeric(oTarget.ToString());
        }


        public static bool IsNumericInt(string targetString)
        {
            int dNullable;

            return int.TryParse(
                targetString,
                System.Globalization.NumberStyles.Any,
                null,
                out dNullable
            );
        }

        public static bool IsNumeric(object oTarget)
        {
            return IsNumeric(oTarget.ToString());
        }

        public static void SendWpfWindowBack(System.Windows.Window window)
        {
            var hWnd = new WindowInteropHelper(window).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
        }
        public static void SendWpfWindowForwad(System.Windows.Window window)
        {
            SetForegroundWindow(new WindowInteropHelper(window).Handle);
        }
    }

}
