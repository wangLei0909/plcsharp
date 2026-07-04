

using System.Linq.Expressions;


namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// Copy
    /// </summary>
    public static class Copy
    {
        private static readonly Dictionary<Type, Delegate> _cache = [];
        /// <summary>
        /// DeepCopy
        /// </summary>
        /// <param name="obj">obj</param>
        /// <returns>返回结果</returns>
        public static T DeepCopy<T>(this T obj)
        {
            if (obj == null) return default;
            var type = typeof(T);
            if (!_cache.TryGetValue(type, out var func))
            {
                var parameter = Expression.Parameter(type, "p");
                var bindings = new List<MemberBinding>();
                foreach (var property in type.GetProperties())
                {
                    if (property.CanWrite)
                    {
                        var propertyAccess = Expression.Property(parameter, property);
                        var binding = Expression.Bind(property, propertyAccess);
                        bindings.Add(binding);
                    }

                }

                var memberInit = Expression.MemberInit(Expression.New(type), bindings);

                var lambda = Expression.Lambda<Func<T, T>>(memberInit, parameter);

                func = lambda.Compile();

                _cache[type] = func;

            }
            return ((Func<T, T>)func)(obj);
        }

    }

}
