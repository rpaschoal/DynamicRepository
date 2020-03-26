using DynamicRepository.AddOn.Resiliency;
using System;

namespace DynamicRepository.AddOn
{
    /// <summary>
    /// Add on builder class for concrete implementations of <see cref="IRepository{Key, Entity}"/>
    /// </summary>
    /// <typeparam name="Key">The key type of the entity being interfaced with through the current repository instance.</typeparam>
    /// <typeparam name="Entity">The entity type for the current repository instance.</typeparam>
    internal class RepositoryAddOnBuilder<Key, Entity> where Entity : class
    {
        private IRepository<Key, Entity> _repositoryBuild;

        internal RepositoryAddOnBuilder(IRepository<Key, Entity> providerRepositoryInstance)
        {
            _repositoryBuild = providerRepositoryInstance;
        }

        /// <summary>
        /// Adds resiliency to the current repository being built by wrapping up repository calls on retry mechanisms.
        /// </summary>
        /// <typeparam name="ExceptionType">The exception type that is going to be caught by the retry mechanism.</typeparam>
        /// <returns>Current repository instance wrapped up on a <see cref="ResilientRepositoryDecorator{Key, Entity, ExceptionType}"/>.</returns>
        internal RepositoryAddOnBuilder<Key, Entity> AddResiliency<ExceptionType>() where ExceptionType : Exception
        {
            if (_repositoryBuild == null)
            {
                throw new Exception("No repository instance to add resiliency for the current DynamicRepository being built.");
            }

            _repositoryBuild = new ResilientRepositoryDecorator<Key, Entity, ExceptionType>(_repositoryBuild);
            return this;
        }

        /// <summary>
        /// Builds final version of constructed <see cref="IRepository{Key, Entity}"/>. 
        /// </summary>
        /// <returns>Built repository instance.</returns>
        internal IRepository<Key, Entity> Build()
        {
            return _repositoryBuild;
        }
    }
}
