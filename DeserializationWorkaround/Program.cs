using System;
using MassTransit;
using Newtonsoft.Json;

namespace DeserializationWorkaround
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = Bus.Factory.CreateUsingInMemory(configurator =>
            {
                configurator.ConfigureJsonSerializer(settings =>
                {
                    settings.Converters.Add(new TypeNameHandlingConverter(TypeNameHandling.Auto));

                    return settings;
                });

                configurator.ConfigureJsonDeserializer(settings =>
                {
                    settings.Converters.Add(new TypeNameHandlingConverter(TypeNameHandling.Auto));

                    return settings;
                });

                configurator.ReceiveEndpoint("test",
                    endpointConfigurator =>
                    {
                        endpointConfigurator.Handler<ComplexMessage>(async context =>
                        {
                            await Console.Out.WriteLineAsync(
                                $"CommandOne Text property is : {((CommandOne)context.Message.Commands[0]).TextOne}");
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
  
            Console.ReadLine();
        }
    }

    internal class TypeNameHandlingConverter : JsonConverter
    {
        private readonly TypeNameHandling typeNameHandling;

        public TypeNameHandlingConverter(TypeNameHandling typeNameHandling)
        {
            this.typeNameHandling = typeNameHandling;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Set TypeNameHandling for serializing objects with $type
            new JsonSerializer { TypeNameHandling = typeNameHandling }.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Set TypeNameHandling for deserializing objects with $type
            var obj = new JsonSerializer { TypeNameHandling = typeNameHandling }.Deserialize(reader, objectType);
            return obj;
        }

        public override bool CanConvert(Type objectType)
        {
            return !IsMassTransitOrSystemType(objectType);
        }

        private static bool IsMassTransitOrSystemType(Type objectType)
        {
            return objectType.Assembly == typeof(MassTransit.IConsumer).Assembly ||
                   objectType.Assembly.IsDynamic ||
                   objectType.Assembly == typeof(object).Assembly;
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
