using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSDTP.Utils
{
    internal class CompiledMethod
    {
        private ConcurrentDictionary<Type[], Func<object[], object>> Methods = new ConcurrentDictionary<Type[], Func<object[], object>>(new ArrayEqualityComparer());

        private MethodInfo Method;
        public CompiledMethod(MethodInfo method)
        {
            Method = method;
        }
        public object Invoke(object obj, Type genericType, params object[] args)
        {
            var type = new Type[] { genericType };
            if (!Methods.TryGetValue(type, out var result))
            {
                result = GetMethodInvoke(obj, Method.MakeGenericMethod(genericType));

                Methods.TryAdd(type, result);
            }
            return result(args);
        }
        public object Invoke(object obj, Type[] genericTypes, params object[] args)
        {
            if (!Methods.TryGetValue(genericTypes, out var result))
            {
                result = GetMethodInvoke(obj, Method.MakeGenericMethod(genericTypes));

                Methods.TryAdd(genericTypes, result);
            }
            return result(args);
        }

        private static Func<object[], object> GetMethodInvoke(object instance, MethodInfo methodInfo)
        {
            // Получаем параметры метода
            ParameterInfo[] methodParameters = methodInfo.GetParameters();

            // Создаем параметры выражения
            ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");

            // Создаем выражения для преобразования аргументов к нужным типам
            Expression[] argExpressions = new Expression[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type parameterType = methodParameters[i].ParameterType;
                Expression argValue = Expression.ArrayIndex(argsParam, index);
                Expression argConvert = Expression.Convert(argValue, parameterType);
                argExpressions[i] = argConvert;
            }

            // Создаем выражение вызова метода
            Expression callExpression;
            if (instance != null)
            {
                Expression instanceExpression = Expression.Constant(instance);
                callExpression = Expression.Call(instanceExpression, methodInfo, argExpressions);
            }
            else
            {
                callExpression = Expression.Call(methodInfo, argExpressions);
            }

            // Создаем выражение преобразования результата к object
            UnaryExpression resultConvert = Expression.Convert(callExpression, typeof(object));

            // Создаем лямбда-выражение
            Expression<Func<object[], object>> lambdaExpression = Expression.Lambda<Func<object[], object>>(
                resultConvert,
                argsParam
            );

            // Компилируем лямбда-выражение и возвращаем результат
            return lambdaExpression.Compile();
        }


        private class ArrayEqualityComparer : IEqualityComparer<Type[]>
        {
            public bool Equals(Type[]? x, Type[]? y)
            {
                if(x==null || y==null)
                    return false;

                if (x.Length != y.Length)
                    return false;
     
                for (int i = 0; i < x.Length; i++)
                    if (x[i] != y[i])
                        return false;

                return true;
            }

            public int GetHashCode(Type[] obj)
            {
                int result = 17;
                for (int i = 0; i < obj.Length; i++)
                {
                    unchecked
                    {
                        result = result * 23 + obj[i].GetHashCode();
                    }
                }
                return result;
            }

        }
    }
}
