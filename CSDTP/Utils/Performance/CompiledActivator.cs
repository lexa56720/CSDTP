using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Utils.Performance
{
    internal class CompiledActivator
    {
        private ConcurrentDictionary<Type, Func<object>> Activators = new ConcurrentDictionary<Type, Func<object>>();
        private Func<object> CreateCtor(Type type)
        {
            ConstructorInfo ctor = type.GetConstructors().First(c => c.GetParameters().Length == 0);
            NewExpression newExp = Expression.New(ctor);
            LambdaExpression lambda = Expression.Lambda(typeof(Func<object>), newExp);
            return (Func<object>)lambda.Compile();
        }

        private Func<object> GetActivator(Type type)
        {
            if (!Activators.TryGetValue(type, out var activator))
            {
                activator = CreateCtor(type);
                Activators.TryAdd(type, activator);
            }
            return activator;
        }


        public object CreateInstance(Type type)
        {
            return GetActivator(type)();
        }
    }
}
