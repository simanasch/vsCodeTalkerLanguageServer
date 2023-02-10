using NAudio.Wave;
using System;
using System.IO;


namespace SpeechGrpcServer
{
    public class WavResampler
    {
        /**
         * @param リサンプリングする.wavファイルのパス
         * @description wasAPIで作成した録音ファイルを16bit,48000kHzの.wavファイルに変換する
         */
        public static String resampleTo16bit(String OutputPath)
        {
            String origFileName = Path.GetDirectoryName(OutputPath) + @"/orig_" + Path.GetFileName(OutputPath);
            if(File.Exists(origFileName))
            {
                File.Delete(origFileName);
            }
            System.IO.File.Move(OutputPath, origFileName);
            using (var reader = new WaveFileReader(origFileName))
            {
                var convertWaveFormat = new WaveFormat(48000, 16, 2);
                var wavProvider = new WaveFloatTo16Provider(reader);
                wavProvider.Volume = 1.3f;
                using (var resampler = new MediaFoundationResampler(wavProvider, convertWaveFormat))
                {
                    WaveFileWriter.CreateWaveFile(OutputPath, resampler);
                }

            }

            File.Delete(origFileName);
            return OutputPath;
        }
    }
}