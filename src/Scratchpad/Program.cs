﻿using System;
using System.Threading;
using REstate;
using Grpc.Core;
using System.Threading.Tasks;
using Grpc.Core.Logging;
using REstate.Engine;
using REstate.Remote;

namespace Scratchpad
{
    class Program
    {
        private static async Task ClientImpl()
        {
            var visor = new InMemoryStateVisor();

            REstateHost.Agent.Configuration
                .RegisterComponent(new InMemoryStateVisorComponent(visor));

            REstateHost.Agent.Configuration
                .RegisterComponent(
                    new GrpcRemoteHostComponent(
                        new GrpcHostOptions
                        {
                            Channel = new Channel("localhost", 12345, ChannelCredentials.Insecure),
                            UseAsDefaultEngine = true
                        }));

            var stateEngine = REstateHost.Agent
                .GetStateEngine<string, string>();

            var schematic = REstateHost.Agent
                .CreateSchematic<string, string>("EchoMachine")

                .WithState("Ready", state => state
                    .AsInitialState()
                    .WithOnEntry("Console", onEntry => onEntry
                        .DescribedAs("Echoes the payload to the console.")
                        .WithSetting("Format", "{2}")
                        .OnFailureSend("EchoFailure"))
                    .WithReentrance("Echo", transition => transition
                        .WithGuard("Console", guard => guard
                            .DescribedAs("Verfies action OK to take with y/n from console.")
                            .WithSetting("Prompt", "Are you sure you want to echo \"{3}\"? (y/n)"))))

                .WithState("EchoFailure", state => state
                    .AsSubstateOf("Ready")
                    .DescribedAs("An echo command failed to execute.")
                    .WithTransitionFrom("Ready", "EchoFailure")).Copy();
            
            var newSchematic = await stateEngine.StoreSchematicAsync(schematic, CancellationToken.None);

            var echoMachine = await stateEngine.CreateMachineAsync("EchoMachine", null, CancellationToken.None);

            await stateEngine.DeleteMachineAsync(echoMachine.MachineId, CancellationToken.None);

            echoMachine = await stateEngine.CreateMachineAsync(schematic, null, CancellationToken.None);

            echoMachine = await stateEngine.GetMachineAsync(echoMachine.MachineId, CancellationToken.None);

            var machineSchematic = await echoMachine.GetSchematicAsync(CancellationToken.None);

            var status = await echoMachine.SendAsync("Echo", "Hello!", CancellationToken.None);

            var commitTag = status.CommitTag;

            status = await echoMachine.SendAsync("Echo", CancellationToken.None);

            try
            {
                status = await echoMachine.SendAsync("Echo", "Fail", commitTag, CancellationToken.None);
            }
            catch(StateConflictException ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine($"EchoMachine ended with state: { visor.GetStatus(echoMachine).State }.");

            await stateEngine.DeleteMachineAsync(echoMachine.MachineId, CancellationToken.None);
        }

        private static void Main(string[] args)
        {
            GrpcEnvironment.SetLogger(new ConsoleLogger());

            using (var server = REstateHost.Agent
                .AsRemote()
                .CreateGrpcServer(new ServerPort("localhost", 12345, ServerCredentials.Insecure)))
            {
                server.Start();

                // sample, launch server/client in same app.
                Task.Run(async () =>
                {
                    await ClientImpl();
                }).Wait();

                Console.ReadLine();
            }
        }
    }
}