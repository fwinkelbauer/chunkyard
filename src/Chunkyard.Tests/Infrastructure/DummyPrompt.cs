﻿using Chunkyard.Core;

namespace Chunkyard.Tests.Infrastructure
{
    internal class DummyPrompt : IPrompt
    {
        public string NewPassword()
        {
            return "secret";
        }

        public string ExistingPassword()
        {
            return "secret";
        }
    }
}