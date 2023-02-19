using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aviUtlDropper
{
    class GcmzDropsData
    {
        // ごちゃまぜドロップスv0.3.11以前
        internal struct LegacyLayout
        {
            public int WindowHandle;
            public int Width;
            public int Height;
            public int VideoRate;
            public int VideoScale;
            public int AudioRate;
            public int AudioChannel;
            public int ApiVersion;
        }
        // ごちゃまぜドロップスv0.3.12以降での構造体
        internal struct CurrentLayout
        {
            public int WindowHandle;
            public int Width;
            public int Height;
            public int VideoRate;
            public int VideoScale;
            public int AudioRate;
            public int AudioChannel;
            public int ApiVersion;
        }
        internal GcmzDropsData(ref CurrentLayout data)
        {
            this.WindowHandle = new IntPtr(data.WindowHandle);
            this.Width = data.Width;
            this.Height = data.Height;
            this.FrameRateBase = data.VideoRate;
            this.FrameRateScale = data.VideoScale;
            this.FrameRate =
                (data.VideoScale > 0) ? ((decimal)data.VideoRate / data.VideoScale) : 0;
            this.AudioSampleRate = data.AudioRate;
            this.AudioChannelCount = data.AudioChannel;
            this.ApiVersion = data.ApiVersion;
        }

        /// <summary>
        /// WM_COPYDATA メッセージ送信先ウィンドウハンドルを取得する。
        /// </summary>
        public IntPtr WindowHandle { get; }

        /// <summary>
        /// WM_COPYDATA 送信先ウィンドウが開かれているか否かを取得する。
        /// </summary>
        public bool IsWindowOpened => this.WindowHandle != IntPtr.Zero;

        /// <summary>
        /// AviUtl拡張編集プロジェクトの横幅設定値を取得する。
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトの縦幅設定値を取得する。
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトのフレームレート基準値を取得する。
        /// </summary>
        public int FrameRateBase { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトのフレームレートスケール値を取得する。
        /// </summary>
        public int FrameRateScale { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトのフレームレートを取得する。
        /// </summary>
        public decimal FrameRate { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトの音声サンプリングレートを取得する。
        /// </summary>
        public int AudioSampleRate { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトの音声チャンネル数を取得する。
        /// </summary>
        public int AudioChannelCount { get; }

        /// <summary>
        /// 『ごちゃまぜドロップス』の外部連携APIバージョンを取得する。
        /// </summary>
        /// <remarks>
        /// <see cref="GcmzInfo(ref Data)"/> コンストラクタで作成した場合は 0 を返す。
        /// </remarks>
        public int ApiVersion { get; }

    }
}
