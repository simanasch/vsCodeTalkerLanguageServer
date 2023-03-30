using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Text;
using Codeplex.Data;
using aviUtlDropper.utilities;
using System.Collections.Generic;

namespace aviUtlConnector
{
    public class AviutlConnector
    {
        private const string FileMapName = @"GCMZDrops";
        private const string SUBTITLE_TEXT_PREFIX = @"<?s=[==[";
        private const string SUBTITLE_TEXT_SUFFIX = "]==];require(\"PSDToolKit\").prep.init({ls_mgl=0,ls_mgr=0,st_mgl=0,st_mgr=0,sl_mgl=0,sl_mgr=0,},obj,s)?>";

        private const string SEND_FILE_TYPE_SOUND_ONLY =  "音声のみ";
        private const string SEND_FILE_TYPE_SOUND_WITH_SIMPLE_LIP_SYNC= "音声+テキスト";
        private const string SEND_FILE_TYPE_SOUND_WITH_COMPLEX_LIP_SYNC = "音声+テキスト+音素";

        public static void SendFile(
            IntPtr fromWindowHandle,
            string filePath,
            int layer = 0,
            string sendFileType = "",
            string body="")
        {
            // TODO:mutexによるロック状態はクラス変数にする
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
            bool isMutexLocked = false;
            List<string> filePathes = new List<string> {};
            try
            {
                GcmzDropsProjectConfig gcmzDropsData = null;
                var data = new COPYDATASTRUCT();
                var dataAddress = IntPtr.Zero;
                var lparam = IntPtr.Zero;
                // ごちゃまぜドロップスのバージョン情報を読取る
                var handle = OpenFileMapping(FILE_MAP_READ, false, FileMapName);
                dataAddress = MapViewOfFile(handle, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
                var GcmzDropsData =
                    (GcmzDropsProjectConfig.CurrentLayout)Marshal.PtrToStructure(
                        dataAddress,
                        typeof(GcmzDropsProjectConfig.CurrentLayout));
                gcmzDropsData = new GcmzDropsProjectConfig(ref GcmzDropsData);
                if (dataAddress != IntPtr.Zero)
                {
                    UnmapViewOfFile(dataAddress);
                }
                if (handle != IntPtr.Zero)
                {
                    CloseHandle(handle);
                }
                // wavファイルの長さ取得
                int wavFileLength = getWaveFileLength(filePath, gcmzDropsData);
                // 送りつけるファイルのリスト作成
                filePathes.Add(filePath);
                if(sendFileType != SEND_FILE_TYPE_SOUND_ONLY)
                {
                    string subtitlePath = generateSubtitleObject(gcmzDropsData, wavFileLength, filePath, body);
                    filePathes.Add(subtitlePath);
                }
                // labファイルは生成できている場合だけ追加する
                if(sendFileType == SEND_FILE_TYPE_SOUND_WITH_COMPLEX_LIP_SYNC)
                {
                    string labPath = filePath.Replace(".wav", ".lab");
                    if(File.Exists(labPath))
                    {
                        filePathes.Add(labPath);
                    }
                }
                // copyDataStructを作る
                data = CreateCopyDataStruct(filePathes, layer, wavFileLength);
                // lparamを作る
                lparam = Marshal.AllocHGlobal(Marshal.SizeOf(data));
                Marshal.StructureToPtr(data, lparam, false);
                // sendMessageの呼び出し
                SendMessage(gcmzDropsData.WindowHandle, WM_COPYDATA, fromWindowHandle, lparam);
            }
            catch (Exception ex)
            {
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
        /// <param name="files">連携するファイルパスのリスト</param>
        /// <param name="layer">レイヤー位置指定</param>
        /// <param name="frameAdvance">ドロップ後に進めるフレーム数</param>
        /// <returns>
        /// COPYDATASTRUCT 値。
        /// DataAddress フィールドは利用後に Marshal.FreeHGlobal で解放する必要がある。
        /// </returns>
        private static COPYDATASTRUCT CreateCopyDataStruct(
            List<string> files,
            int layer,
            int frameAdvance = 0)
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

        private static int getWaveFileLength(string filePath, GcmzDropsProjectConfig config) {
            int frameAdvance = 0;
            // 何フレーム進めるかの取得処理
            String fileExt = Path.GetExtension(filePath);
            if (fileExt.ToLower() == ".wav" && frameAdvance <= 0)
            {
                // 返却値がミリ秒なので/1000*フレームレートが進めるべきフレーム数
                decimal rawFrameCount = (decimal) Audio.getAudioFileMilliSecondsLength(filePath);
                frameAdvance = (int)Math.Round(rawFrameCount / 1000 * config.FrameRate);
            }
            return frameAdvance;
        }
        private const string TEMPLATE_EXO_PATH = "aviUtlDropper.resources.simpleLipsync.txt";
        private static string generateSubtitleObject(GcmzDropsProjectConfig config, int wavLength, string filePath, string body="")
        {
            // resourceにあるテンプレートファイルを読み込む
            string template = "";
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (var rStream = assembly.GetManifestResourceStream(TEMPLATE_EXO_PATH))
            {

                using (StreamReader sr = new StreamReader(rStream, Encoding.GetEncoding("shift-jis")))
                {
                    template = sr.ReadToEnd();
                }
            }
            String content = "";
            // 字幕にする文字列はprefix,suffixをつけた上で1文字づつutf-16LE、固定長4文字のバイト文字列に変換する

            foreach(char c in String.Join("\r\n", new string[] { SUBTITLE_TEXT_PREFIX , body ,SUBTITLE_TEXT_SUFFIX })
                .ToCharArray())
            {
                content += BitConverter.ToString(
                    Encoding.GetEncoding("UTF-16LE")
                        .GetBytes(c.ToString() )
                    )
                .Replace("-", "")
                .ToLower()
                .PadRight(4, '0');
            }
            content = content.PadRight(4096, '0');
            string result = string.Format(
                template,
                config.Width,
                config.Height,
                config.FrameRate,
                wavLength - 1,
                content,
                filePath.Replace(@"\", @"\\")

            );
            string resultFilePath = filePath.Replace(".wav", ".exo");
            using (StreamWriter wStream = new StreamWriter(resultFilePath, true, Encoding.GetEncoding("shift-jis"))) { wStream.Write(result); }
            return resultFilePath;
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
