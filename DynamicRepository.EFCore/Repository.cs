using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using Microsoft.EntityFrameworkCore;
using DynamicRepository.Filter;
using DynamicRepository.Extensions;
using System.Threading.Tasks;
using System.Threading;

namespace DynamicRepository.EFCore
{
    /// <summary>
    /// Base repository for persistency model CRUD and advanced filtering operations.
    /// </summary>
    /// <typeparam name="Key">The key type of current entity type. For composed primary keys use a new class type definition or an <see cref="object[]"/> array.</typeparam>
    /// <typeparam name="Entity">The type of the entity being persisted or retrieved.</typeparam>
    public abstract class Repository<Key, Entity> : RepositoryProxy<Key, Entity>, IRepository<Key, Entity> where Entity : class, new()
    {
        /// <summary>
        /// Current EF DBContext instance.
        /// </summary>
        protected DbContext Context { get; set; }

        /// <summary>
        /// DBSet of <see cref="Entity"/> extracted from <see cref="Context"/>.
        /// </summary>
        protected internal DbSet<Entity> DbSet;


        /// <summary>
        /// Default constructor of main repository. 
        /// Required dependencies are injected.
        /// </summary>
        /// <param name="context">Current EF context.</param>
        public Repository(DbContext context): base(BuildInternals(context))
        {
            Context = context;

            // Configures current entity DB Set which is being manipulated
            DbSet = context.Set<Entity>();
        }

        private static IRepository<Key, Entity> BuildInternals(DbContext context)
        {
            return new RepositoryAddOnBuilder<Key, Entity>(new EFCoreRepository<Key, Entity>(context))
                .AddResiliency()
                .Build();
        }
    }
}
