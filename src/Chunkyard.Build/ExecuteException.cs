using System;
using System.Runtime.Serialization;

namespace Chunkyard
{
    /// <summary>
    /// A custom exception type.
    /// </summary>
    [Serializable]
    public class ExecuteException : Exception
    {
        public ExecuteException()
        {
        }

        public ExecuteException(string message)
            : base(message)
        {
        }

        public ExecuteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ExecuteException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
