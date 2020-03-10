﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Chunkyard
{
    public interface IRepository
    {
        public Uri RepositoryUri { get; }

        Uri StoreContent(HashAlgorithmName algorithm, byte[] value);

        byte[] RetrieveContent(Uri contentUri);

        bool ContentExists(Uri contentUri);

        int AppendToLog(byte[] value, string logName, int? currentLogPosition);

        byte[] RetrieveFromLog(string logName, int logPosition);

        int? FetchLogPosition(string logName);

        IEnumerable<int> ListLogPositions(string logName);

        IEnumerable<string> ListLogNames();
    }
}