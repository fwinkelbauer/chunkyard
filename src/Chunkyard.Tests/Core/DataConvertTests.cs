using System;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class DataConvertTests
    {
        [Fact]
        public static void Converts_Objects()
        {
            var blob = new Blob("some blob", DateTime.UtcNow);

            var bytes = DataConvert.ObjectToBytes(blob);

            Assert.Equal(
                blob,
                DataConvert.BytesToObject<Blob>(bytes));
        }

        [Fact]
        public static void Converts_Text()
        {
            var text = "Hello World!";

            var bytes = DataConvert.TextToBytes(text);

            Assert.Equal(
                text,
                DataConvert.BytesToText(bytes));
        }
    }
}
