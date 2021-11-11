using System;
using System.Runtime.Serialization;

namespace Chunkyard.Build
{
    /// <summary>
    /// A custom exception type.
    /// </summary>
    [Serializable]
    public class BuildException : Exception
    {
        public BuildException()
        {
        }

        public BuildException(string message)
            : base(message)
        {
        }

        public BuildException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BuildException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
