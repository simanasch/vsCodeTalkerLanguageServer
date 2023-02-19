using System;
using CommandLine;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ttscontroller;
using System.Threading;

namespace SpeechGrpcServer
{
    class Server
    {
        static Grpc.Core.Server server = null;

        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('p', "port", Required = false, HelpText = "リクエスト待ちをするポート番号", Default = 5001)]
            public int Port { get; set; }
        }

        static void Main(string[] args)
        {
            
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o => RunServer(o));
        }

        static void RunServer(Options o)
        {
            server = new Grpc.Core.Server
            {
                Services = {
                    TTSService.BindService(new TTSControllerImpl())
                },
                Ports = { new ServerPort("localhost", o.Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("localhost:" + o.Port + "で接続待機中");
            while (true)
            {
                Thread.Sleep(500);
            }
        }
    }
}
