using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace WorkflowCore.Services.FluentBuilders
{
    internal static class ExpressionHelpers
    {
        public static Expression<Action<TObject, TParam>> CreateSetter<TObject, TParam>(
            Expression<Func<TObject, TParam>> getterExpression)
        {
            var valueParameter = Expression.Parameter(typeof(TParam), "value");
            Expression assignment;
            if (getterExpression.Body is MethodCallExpression callExpression && callExpression.Method.Name == "get_Item")
            {
                //Get Matching setter method for the indexer
                var parameterTypes = callExpression.Method.GetParameters()
                    .Select(p => p.ParameterType)
                    .ToArray();
                var itemProperty = callExpression.Method.DeclaringType.GetProperty("Item", typeof(TParam), parameterTypes);

                assignment = Expression.Call(callExpression.Object, itemProperty.SetMethod, callExpression.Arguments.Concat(new[] { valueParameter }));
            }
            else
            {
                assignment = Expression.Assign(getterExpression.Body, valueParameter);
            }
            return Expression.Lambda<Action<TObject, TParam>>(assignment, valueParameter);
        }

        public static LambdaExpression CreateSetter(LambdaExpression getterExpression)
        {
            var valueParameter = Expression.Parameter(getterExpression.ReturnType, "value");
            Expression assignment;
            if (getterExpression.Body is MethodCallExpression callExpression && callExpression.Method.Name == "get_Item")
            {
                //Get Matching setter method for the indexer
                var parameterTypes = callExpression.Method.GetParameters()
                    .Select(p => p.ParameterType)
                    .ToArray();
                var itemProperty = callExpression.Method.DeclaringType.GetProperty("Item", valueParameter.Type, parameterTypes);

                assignment = Expression.Call(callExpression.Object, itemProperty.SetMethod, callExpression.Arguments.Concat(new[] { valueParameter }));
            }
            else
            {
                assignment = Expression.Assign(getterExpression.Body, valueParameter);
            }

            var parameters = getterExpression.Parameters.Concat(new[] {valueParameter}).ToArray();
            return Expression.Lambda(assignment, parameters);
        }
    }
}
