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
        struct LegacyLayout
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
        struct CurrentLayout
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
    }
}
