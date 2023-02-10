﻿using System;
using Speech;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ttscontroller;
using System.Threading;

namespace SpeechGrpcServer
{

    public class TTSControllerImpl : TTSService.TTSServiceBase
    {
        public override Task<SpeechEngineList> getSpeechEngineDetail(SpeechEngineRequest request, ServerCallContext context)
        {
            return Task.FromResult(GetLibraryList());
        }

        public override Task<ttsResult> record(ttsRequest request, ServerCallContext context)
        {
            return RecordTask(request.LibraryName, request.EngineName, request.Body, request.OutputPath);
        }

        public override Task<ttsResult> talk(ttsRequest request, ServerCallContext context)
        {
            return TalkTask(request.LibraryName, request.EngineName, request.Body, request.OutputPath);
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

        private static Task<ttsResult> TalkTask(String libraryName, String engineName, String body, String outputPath)
        {
            Console.WriteLine("talk called,Library Name:" + libraryName + "\nengine:" + engineName + "\nbody:" + body);
            // engine.finishedイベントが呼ばれてから結果を返すようにするためTaskCompletionSourceを使う
            var tcs = new TaskCompletionSource<ttsResult>();

            ISpeechController engine = getInstance(libraryName, engineName);
            if (engine == null)
            {
                Console.WriteLine($"{libraryName} を起動できませんでした。");
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
                    LibraryName = libraryName,
                    EngineName = engineName,
                    Body = body,
                    OutputPath = outputPath
                });
            };
            engine.Play(body);
            return tcs.Task;
        }

        private static Task<ttsResult> RecordTask(String libraryName, String engineName, String body, String outputPath)
        {
            Console.WriteLine("Record called,Library Name:" + libraryName + "\nengine:" + engineName + "\nbody:" + body);
            // engine.finishedイベントが呼ばれてから結果を返すようにするためTaskCompletionSourceを使う
            var tcs = new TaskCompletionSource<ttsResult>();

            SoundRecorder recorder = new SoundRecorder(outputPath);
            recorder.PostWait = 300;

            ISpeechController engine = getInstance(libraryName, engineName);
            if (engine == null)
            {
                Console.WriteLine($"{libraryName} を起動できませんでした。");
                return Task.FromResult(new ttsResult
                {
                    IsSuccess = false
                });
            }
            engine.Activate();
            engine.Finished += (s, a) =>
            {
                Task t = recorder.Stop();
                t.Wait();
                engine.Dispose();
                Console.WriteLine("record completed");
                WavResampler.resampleTo16bit(outputPath);
                tcs.TrySetResult(new ttsResult
                {
                    IsSuccess = true,
                    LibraryName = libraryName,
                    EngineName = engineName,
                    Body = body,
                    OutputPath = outputPath
                });
            };
            // recorderの起動後に音声を再生する
            recorder.Start();
            engine.Play(body);
            return tcs.Task;
        }

        private static Task<ttsResult> RecordViaTtsController(ISpeechController engine, SoundRecorder recorder, String libraryName, String engineName, String body, String outputPath)
        {
            var tcs = new TaskCompletionSource<ttsResult>();
            engine.Activate();
            engine.Finished += (s, a) =>
            {
                Task t = recorder.Stop();
                t.Wait();
                engine.Dispose();
                Console.WriteLine("record completed");
                WavResampler.resampleTo16bit(outputPath);
                tcs.TrySetResult(new ttsResult
                {
                    IsSuccess = true,
                    LibraryName = libraryName,
                    EngineName = engineName,
                    Body = body,
                    OutputPath = outputPath
                });
            };
            // recorderの起動後に音声を再生する
            recorder.Start();
            engine.Play(body);
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
