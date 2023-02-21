using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;


namespace aviUtlDropper.utilities
{
    class Audio
    {
        // 引数のファイルパスに対する、wavファイルのミリ秒単位での長さを返す
        internal static double getAudioFileMilliSecondsLength(String filePath)
        {
            double result = 0;
            using(var reader = new WaveFileReader(filePath))
            {
                result = reader.TotalTime.TotalMilliseconds;
            }
            return result;
        }
    }
}
