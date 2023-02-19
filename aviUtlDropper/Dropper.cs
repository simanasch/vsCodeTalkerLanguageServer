using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using System.IO;
using System.Text;
using Codeplex.Data;

namespace aviUtlDropper
{
    public class Dropper
    {
        private const string FileMapName = @"GCMZDrops";
        public static void Drop(
            IntPtr ownWindowHandle,
            IEnumerable<string> filePathes,
            int stepFrameCount = 0,
            int layer = 0,
            int timeoutMilliseconds = -1)
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
            var handle = IntPtr.Zero;
            bool isMutexLocked = false;
            var dataAddress = IntPtr.Zero;
            GcmzDropsData gcmzDropsData = null;
            try
            {
                // ごちゃまぜドロップスのバージョン情報を読取る
                // var result = ReadAndValidateGcmzInfo(out var gcmzInfo, mutex != null);
                handle = OpenFileMapping(FILE_MAP_READ, false, FileMapName);
                dataAddress = MapViewOfFile(handle, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
                var GcmzDropsData =
                    (GcmzDropsData.CurrentLayout)Marshal.PtrToStructure(
                        dataAddress,
                        typeof(GcmzDropsData.CurrentLayout));
                gcmzDropsData = new GcmzDropsData(ref GcmzDropsData);
                if (dataAddress != IntPtr.Zero)
                {
                    UnmapViewOfFile(dataAddress);
                }
                if (handle != IntPtr.Zero)
                {
                    CloseHandle(handle);
                }
                // copyDataStructを作る
                data = CreateCopyDataStruct(layer, stepFrameCount, filePathes);
                // lparamを作る
                lparam = Marshal.AllocHGlobal(Marshal.SizeOf(data));
                Marshal.StructureToPtr(data, lparam, false);
                // sendMessageの呼び出し
                 SendMessage(gcmzDropsData.WindowHandle, WM_COPYDATA, ownWindowHandle, lparam);
            }
            catch (Exception ex)
            {
                // 
                Console.WriteLine("なんか落ちた"+ex);
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
        /// <summary>
        /// 『ごちゃまぜドロップス』 v0.3.12 以降用の COPYDATASTRUCT 値を作成する。
        /// </summary>
        /// <param name="layer">レイヤー位置指定。</param>
        /// <param name="frameAdvance">ドロップ後に進めるフレーム数。</param>
        /// <param name="files">ファイルパス列挙。</param>
        /// <returns>
        /// COPYDATASTRUCT 値。
        /// DataAddress フィールドは利用後に Marshal.FreeHGlobal で解放する必要がある。
        /// </returns>
        private static COPYDATASTRUCT CreateCopyDataStruct(
            int layer,
            int frameAdvance,
            IEnumerable<string> files)
        {
            // 送信JSON文字列作成
            var json = DynamicJson.Serialize(new { layer, frameAdvance, files });
            var data = Encoding.UTF8.GetBytes(json);

            // COPYDATASTRUCT 作成
            var cds = new COPYDATASTRUCT();
            try
            {
                cds.Param = new UIntPtr(1);
                cds.size = data.Length;
                cds.intPtr = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, cds.intPtr, data.Length);
            }
            catch
            {
                if (cds.intPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(cds.intPtr);
                }
                throw;
            }

            return cds;
        }


        #region Win32 API import
        [StructLayout(LayoutKind.Sequential)]
        struct COPYDATASTRUCT
        {
            public UIntPtr Param;
            public int size;
            public IntPtr intPtr;
        }
        // ごちゃまぜドロップスのファイルマッピング読み込み関係
        private const uint FILE_MAP_READ = 0x0004;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenFileMapping(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr fileMapHandle,
            uint desiredAccess,
            uint fileOffsetHigh,
            uint fileOffsetLow,
            UIntPtr numberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr address);

        // デスクトップのウィンドウ取得

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr windowHandle, uint flags);
        // ごちゃまぜドロップスのAPI(sendMessage)関係
        private const uint WM_COPYDATA = 0x004A;
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            string lparam);

        #endregion
    }
}
