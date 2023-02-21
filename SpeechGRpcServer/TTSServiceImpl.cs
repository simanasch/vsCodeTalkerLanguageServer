using System;
using Speech;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ttscontroller;
using System.Threading;
using System.Diagnostics;
using aviUtlConnector;

namespace SpeechGrpcServer
{

    public class TTSControllerImpl : TTSService.TTSServiceBase
    {
        internal static IntPtr Handle;
        public TTSControllerImpl(IntPtr intPtr)
        {
            Handle = intPtr;
        }
        public override Task<SpeechEngineList> getSpeechEngineDetail(SpeechEngineRequest request, ServerCallContext context)
        {
            return Task.FromResult(GetLibraryList());
        }

        public override Task<ttsResult> record(ttsRequest request, ServerCallContext context)
        {
            return RecordTask(request);
        }

        public override Task<ttsResult> talk(ttsRequest request, ServerCallContext context)
        {
            return TalkTask(request);
        }

        private static SpeechEngineList GetLibraryList()
        {
            var results = new SpeechEngineList();
            var engines = SpeechController.GetAllSpeechEngine();
            foreach (SpeechEngineInfo engineInfo in engines)
            {
                results.DetailItem.Add(new SpeechEngineList.Types.SpeechEngineDetail
                {
                    EngineName = engineInfo.EngineName,
                    LibraryName = engineInfo.LibraryName,
                    EnginePath = String.IsNullOrWhiteSpace(engineInfo.EnginePath) ? "" : engineInfo.EnginePath,
                    Is64BitProcess = engineInfo.Is64BitProcess
                });
                Console.WriteLine(engineInfo);
            }
            return results;
        }

        private static Task<ttsResult> TalkTask(ttsRequest request)
        {
            Console.WriteLine("talk called,Library Name:" + request.LibraryName + "\nengine:" + request.EngineName + "\nbody:" + request.Body);
            // engine.finishedイベントが呼ばれてから結果を返すようにするためTaskCompletionSourceを使う
            var tcs = new TaskCompletionSource<ttsResult>();

            ISpeechController engine = getInstance(request.LibraryName, request.EngineName);
            if (engine == null)
            {
                Console.WriteLine($"{request.LibraryName} を起動できませんでした。");
                return Task.FromResult(new ttsResult
                {
                    IsSuccess = false,
                    OutputPath = ""
                });
            }

            engine.Activate();
            engine.Finished += (s, a) =>
            {
                engine.Dispose();
                Console.WriteLine("talk completed");
                tcs.TrySetResult(new ttsResult
                {
                    IsSuccess = true,
                    LibraryName = request.LibraryName,
                    EngineName = request.EngineName,
                    Body = request.Body,
                    OutputPath = request.OutputPath
                });
            };
            engine.Play(request.Body);
            return tcs.Task;
        }

        private static Task<ttsResult> RecordTask(ttsRequest request)
        {
            Console.WriteLine("Record called,Library Name:" + request.LibraryName + "\nengine:" + request.EngineName + "\nbody:" + request.Body);
            // engine.finishedイベントが呼ばれてから結果を返すようにするためTaskCompletionSourceを使う
            //var tcs = new TaskCompletionSource<ttsResult>();

            SoundRecorder recorder = new SoundRecorder(request.OutputPath);
            recorder.PostWait = 300;

            ISpeechController engine = getInstance(request.LibraryName, request.EngineName);
            if (engine == null)
            {
                Console.WriteLine($"{request.LibraryName} を起動できませんでした。");
                return Task.FromResult(new ttsResult
                {
                    IsSuccess = false
                });
            }
            // TODO: 録音機能を自前実装してるかで処理分岐
            return RecordViaTtsController(engine, recorder, request);
        }

        private static Task<ttsResult> RecordViaTtsController(ISpeechController engine, SoundRecorder recorder, ttsRequest request)
        {
            var tcs = new TaskCompletionSource<ttsResult>();
            engine.Activate();
            engine.Finished += (s, a) =>
            {
                Task t = recorder.Stop();
                t.Wait();
                engine.Dispose();
                Console.WriteLine("record completed");
                WavResampler.resampleTo16bit(request.OutputPath);
                tcs.TrySetResult(new ttsResult
                {
                    IsSuccess = true,
                    LibraryName = request.LibraryName,
                    EngineName = request.EngineName,
                    Body = request.Body,
                    OutputPath = request.OutputPath
                });
                // 保存したファイルをaviutlに送りつける
                AviutlConnector.SendFile(TTSControllerImpl.Handle, request.OutputPath);
            };
            // recorderの起動後に音声を再生する
            recorder.Start();
            engine.Play(request.Body);
            return tcs.Task;
        }

        private static ISpeechController getInstance(String libraryName, String engineName)
        {
            if (String.IsNullOrWhiteSpace(engineName))
            {
                return SpeechController.GetInstance(libraryName);
            }
            else
            {
                return SpeechController.GetInstance(libraryName, engineName);
            }

        }
    }
}
