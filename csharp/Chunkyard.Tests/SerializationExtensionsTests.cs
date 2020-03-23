using System;
using Xunit;

namespace Chunkyard.Tests
{
    public static class SerializationExtensionsTests
    {
        [Fact]
        public static void Converts_ToBytes_And_ToObject()
        {
            var expectedObject = new SomeData("Hello World!");

            var bytes = expectedObject.ToBytes();
            var actualObject = bytes.ToObject<SomeData>();

            Assert.Equal(expectedObject, actualObject);
        }

        private class SomeData
        {
            public SomeData(string importantMessage)
            {
                ImportantMessage = importantMessage;
            }

            public string ImportantMessage { get; }

            public override bool Equals(object? obj)
            {
                return obj is SomeData someData &&
                    ImportantMessage == someData.ImportantMessage;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ImportantMessage);
            }
        }
    }
}
