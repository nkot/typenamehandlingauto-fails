using System;
using MassTransit;
using Newtonsoft.Json;

namespace DeserializationFails
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = Bus.Factory.CreateUsingInMemory(configurator =>
            {
                configurator.ConfigureJsonSerializer(settings =>
                {
                    // include $type into json so that we can deserialize CommandOne and CommandTwo
                    settings.TypeNameHandling = TypeNameHandling.Auto; 
                    return settings;
                });

                configurator.ConfigureJsonDeserializer(settings =>
                {
                    settings.TypeNameHandling = TypeNameHandling.Auto; 
                    return settings;
                });

                configurator.ReceiveEndpoint("test",
                    endpointConfigurator =>
                    {
                        endpointConfigurator.Handler<ComplexMessage>(async context =>
                        {
                            await Console.Out.WriteLineAsync(
                                $"CommandOne Text property is : {((CommandOne) context.Message.Commands[0]).TextOne}");
                            await Console.Out.WriteLineAsync(
                                $"CommandTwo Text property is : {((CommandTwo)context.Message.Commands[1]).TextTwo}");
                            await Console.Out.WriteLineAsync("Deserialization succeded!");
                        });
                    });
            });
            bus.Start();
            bus.Publish<ComplexMessage>(new ComplexMessage
            {
                Commands = new IMyCommand[]
                {
                    new CommandOne{TextOne = "One"},
                    new CommandTwo{TextTwo = "Two"}
                }
            }).Wait();
            /*
             * The exception we receive in Console:
             * MOVE loopback://localhost/test 8f5a0000-3ede-dc4a-31fd-08d5155e736c loopback://localhost/test_error Fault: 
             * Type specified in JSON 'MassTransit.Serialization.JsonMessageEnvelope, MassTransit, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b8e0e9f2f1e657fa' 
             * is not compatible with 'GreenPipes.DynamicInternal.MassTransit.Serialization.MessageEnvelope, MassTransit.SerializationGreenPipes.DynamicInternale259301d179d416dbde48e08153bbd64, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. 
             * Path '$type', line 2, position 71.
            */
            Console.ReadLine();
        }
    }

    public class ComplexMessage
    {
        public IMyCommand[] Commands { get; set; }
    }

    public interface IMyCommand
    {
    }

    public class CommandOne : IMyCommand
    {
        public string TextOne { get; set; }
    }

    public class CommandTwo : IMyCommand
    {
        public string TextTwo { get; set; }
    }
}
