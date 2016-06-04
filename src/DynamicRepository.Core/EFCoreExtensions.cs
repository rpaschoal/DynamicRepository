using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DynamicRepository.Core
{
    /// <summary>
    /// Extension methods to add behavios which EF Core still haven't implemented on its official API.
    /// </summary>
    public static class EFCoreExtensions
    {
        /// <summary>
        /// Finds an entity by its ID.
        /// 
        /// </summary>
        /// <remarks>
        /// Credits: http://www.c-sharpcorner.com/blogs/dbsetfind-api-missing-in-entity-framework-core-final-rc1-version
        /// </remarks>
        public static TEntity Find<TEntity>(this DbSet<TEntity> set, params object[] keyValues) where TEntity : class
        {
            var context = ((IInfrastructure<IServiceProvider>)set).GetService<DbContext>();

            var entityType = context.Model.FindEntityType(typeof(TEntity));
            var key = entityType.FindPrimaryKey();

            var entries = context.ChangeTracker.Entries<TEntity>();

            var i = 0;
            foreach (var property in key.Properties)
            {
                entries = Enumerable.Where(entries, e => e.Property(property.Name).CurrentValue == keyValues[i]);
                i++;
            }

            var entry = entries.FirstOrDefault();
            if (entry != null)
            {
                // Return the local object if it exists.  
                return entry.Entity;
            }

            // TODO: Build the real LINQ Expression  
            // set.Where(x => x.Id == keyValues[0]);  
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var query = Queryable.Where(set, (Expression<Func<TEntity, bool>>)
                Expression.Lambda(
                    Expression.Equal(
                        Expression.Property(parameter, "Id"),
                        Expression.Constant(keyValues[0])),
                    parameter));

            // Look in the database  
            return query.FirstOrDefault();
        }
    }
}
