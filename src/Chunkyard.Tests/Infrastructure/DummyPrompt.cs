﻿using Chunkyard.Core;

namespace Chunkyard.Tests.Infrastructure
{
    internal class DummyPrompt : IPrompt
    {
        private readonly string _password;

        public DummyPrompt(string password)
        {
            _password = password;
        }

        public string NewPassword()
        {
            return _password;
        }

        public string ExistingPassword()
        {
            return _password;
        }
    }
}
