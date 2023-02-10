using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace aviUtlDropper
{
    public class Dropper
    {
        public static void Drop()
        {
            Mutex mutex = null;
            // mutexによる排他制御ができる場合、mutexによるロックを取得する
            try
            {
                Mutex.TryOpenExisting(@"GCMZDropsMutex", out mutex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("mutex開き損ねた" + ex);
            }
            var data = new COPYDATASTRUCT();
            var lparam = IntPtr.Zero;
            bool isMutexLocked = false;
            try
            {
                // ごちゃまぜドロップスのバージョン情報を読取る
                // var result = ReadAndValidateGcmzInfo(out var gcmzInfo, mutex != null);
                // copyDataStructを作る
                // lparamを作る
                lparam = Marshal.AllocHGlobal(Marshal.SizeOf(data));
            }
            catch (Exception ex)
            {
                // 
            }
            finally
            {
                // mutexによるロックの解除
                if (mutex != null)
                {
                    if (isMutexLocked)
                    {
                        mutex.ReleaseMutex();
                    }
                    mutex.Dispose();
                }
            }


        }

        #region Win32 API import
        [StructLayout(LayoutKind.Sequential)]
        struct COPYDATASTRUCT
        {
            public UIntPtr Param;
            public int size;
            public IntPtr intPtr;
        }

        private const uint WM_COPYDATA = 0x004A;
        #endregion
    }
}
