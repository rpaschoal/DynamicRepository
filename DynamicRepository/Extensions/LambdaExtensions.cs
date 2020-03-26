using System;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace DynamicRepository.Extensions
{
    /// <summary>
    /// C# Lambda helpers.
    /// </summary>
    internal static class LambdaExtensions
    {
        /// <summary>
        /// Parses a string expression to strongly typed response Expression Lambda.
        /// </summary>
        internal static Expression<Func<Entity, bool>> ParseLambda<Entity>(this string expression, object[] parameters) where Entity : class, new()
        {
            if (!String.IsNullOrEmpty(expression))
            {
                var lambdaExpression = DynamicExpressionParser.ParseLambda(false, typeof(Entity), null, expression, parameters);
                var body = lambdaExpression.Body;
                var p = lambdaExpression.Parameters;
                return Expression.Lambda<Func<Entity, bool>>(body, p);
            }

            return null;
        }
    }
}
