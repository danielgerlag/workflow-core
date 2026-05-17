using System;
using System.Linq;
using System.Linq.Expressions;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class MemberMapParameter : IStepParameter
    {
        private readonly LambdaExpression _source;
        private readonly LambdaExpression _target;
        private readonly Delegate _compiledSource;
     
        public MemberMapParameter(LambdaExpression source, LambdaExpression target)
        {
            if (target.Body.NodeType != ExpressionType.MemberAccess)
                throw new NotSupportedException();

            _source = source;
            _target = target;
            _compiledSource = source.Compile();
        }

        private void Assign(object sourceObject, object targetObject, IStepExecutionContext context)
        {
            object resolvedValue = null;

            switch (_source.Parameters.Count)
            {
                case 1:
                    resolvedValue = _compiledSource.DynamicInvoke(sourceObject);
                    break;
                case 2:
                    resolvedValue = _compiledSource.DynamicInvoke(sourceObject, context);
                    break;
                default:
                    throw new ArgumentException();
            }

            if (resolvedValue == null)
            {
                var defaultAssign = Expression.Lambda(Expression.Assign(_target.Body, Expression.Default(_target.ReturnType)), _target.Parameters.Single());
                defaultAssign.Compile().DynamicInvoke(targetObject);
                return;
            }

            var valueExpr = Expression.Convert(Expression.Constant(resolvedValue), _target.ReturnType);
            var assign = Expression.Lambda(Expression.Assign(_target.Body, valueExpr), _target.Parameters.Single());
            assign.Compile().DynamicInvoke(targetObject);
        }

        public void AssignInput(object data, IStepBody body, IStepExecutionContext context)
        {
            Assign(data, body, context);
        }

        public void AssignOutput(object data, IStepBody body, IStepExecutionContext context)
        {
            Assign(body, data, context);
        }
    }
}
