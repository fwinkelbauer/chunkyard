using System;

namespace Chunkyard.Core
{
    /// <summary>
    /// An abstract implementation of <see cref="IRepository"/> which can be
    /// used to implement decorators.
    /// </summary>
    public abstract class DecoratorRepository : IRepository
    {
        protected DecoratorRepository(IRepository repository)
        {
            Repository = repository;
        }

        public virtual Uri RepositoryUri => Repository.RepositoryUri;

        protected IRepository Repository { get; }

        public virtual void StoreValue(Uri contentUri, byte[] value)
        {
            Repository.StoreValue(contentUri, value);
        }

        public virtual byte[] RetrieveValue(Uri contentUri)
        {
            return Repository.RetrieveValue(contentUri);
        }

        public virtual bool ValueExists(Uri contentUri)
        {
            return Repository.ValueExists(contentUri);
        }

        public virtual Uri[] ListUris()
        {
            return Repository.ListUris();
        }

        public virtual void RemoveValue(Uri contentUri)
        {
            Repository.RemoveValue(contentUri);
        }

        public virtual int AppendToLog(int newLogPosition, byte[] value)
        {
            return Repository.AppendToLog(newLogPosition, value);
        }

        public virtual byte[] RetrieveFromLog(int logPosition)
        {
            return Repository.RetrieveFromLog(logPosition);
        }

        public virtual void RemoveFromLog(int logPosition)
        {
            Repository.RemoveFromLog(logPosition);
        }

        public virtual int[] ListLogPositions()
        {
            return Repository.ListLogPositions();
        }
    }
}
