﻿using System;
using System.Collections.Generic;

namespace Chunkyard
{
    public abstract class DecoratorRepository : IRepository
    {
        public DecoratorRepository(IRepository repository)
        {
            Repository = repository;
        }

        public virtual Uri RepositoryUri => Repository.RepositoryUri;

        protected IRepository Repository { get; }

        public virtual bool StoreValue(Uri contentUri, byte[] value)
        {
            return Repository.StoreValue(contentUri, value);
        }

        public virtual byte[] RetrieveValue(Uri contentUri)
        {
            return Repository.RetrieveValue(contentUri);
        }

        public virtual bool ValueExists(Uri contentUri)
        {
            return Repository.ValueExists(contentUri);
        }

        public virtual IEnumerable<Uri> ListUris()
        {
            return Repository.ListUris();
        }

        public virtual void RemoveValue(Uri contentUri)
        {
            Repository.RemoveValue(contentUri);
        }

        public virtual int AppendToLog(byte[] value, int newLogPosition)
        {
            return Repository.AppendToLog(value, newLogPosition);
        }

        public virtual byte[] RetrieveFromLog(int logPosition)
        {
            return Repository.RetrieveFromLog(logPosition);
        }

        public virtual void RemoveFromLog(int logPosition)
        {
            Repository.RemoveFromLog(logPosition);
        }

        public virtual IEnumerable<int> ListLogPositions()
        {
            return Repository.ListLogPositions();
        }
    }
}