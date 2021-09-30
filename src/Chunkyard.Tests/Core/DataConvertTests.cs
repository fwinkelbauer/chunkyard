using System;
using Chunkyard.Core;
using Xunit;

namespace Chunkyard.Tests.Core
{
    public static class DataConvertTests
    {
        [Fact]
        public static void ObjectToBytes_Converts_Objects_To_Bytes()
        {
            var blob = new Blob("some blob", DateTime.UtcNow);

            var bytes = DataConvert.ObjectToBytes(blob);

            Assert.Equal(
                blob,
                DataConvert.BytesToObject<Blob>(bytes));
        }

        [Fact]
        public static void TextToBytes_Converts_Text_To_Bytes()
        {
            var text = "Hello World!";

            var bytes = DataConvert.TextToBytes(text);

            Assert.Equal(
                text,
                DataConvert.BytesToText(bytes));
        }
    }
}
