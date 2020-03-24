﻿using DynamicRepository.AddOn.Resiliency;
using System;

namespace DynamicRepository.AddOn
{
    public class RepositoryAddOnBuilder<Key, Entity> where Entity : class
    {
        private IRepository<Key, Entity> _repositoryBuild;

        public RepositoryAddOnBuilder(IRepository<Key, Entity> providerRepositoryInstance)
        {
            _repositoryBuild = providerRepositoryInstance;
        }

        public RepositoryAddOnBuilder<Key, Entity> AddResiliency<ExceptionType>() where ExceptionType : Exception
        {
            if (_repositoryBuild == null)
            {
                throw new Exception("No repository instance to add resiliency for the current DynamicRepository being built.");
            }

            _repositoryBuild = new ResilientRepositoryDecorator<Key, Entity, ExceptionType>(_repositoryBuild);
            return this;
        }

        public IRepository<Key, Entity> Build()
        {
            return _repositoryBuild;
        }
    }
}
