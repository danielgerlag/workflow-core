using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WorkflowCore;
using WorkflowCore.Interface;
using WorkflowCore.Models.DefinitionStorage.v1;

namespace WorkflowCore.Services.DefinitionStorage
{
    public class StoredWorkflowDefinition : IWorkflow
    {
        private DefinitionSourceV1 _source;

        private Type _dataType => FindType(_source.DataType);

        public string Id => _source.Id;

        public int Version => _source.Version;

        public StoredWorkflowDefinition(DefinitionSourceV1 source)
        {
            _source = source;
        }

        public void Build(IWorkflowBuilder<object> builder)
        {
            var sourceStep = _source.Steps.First();
            var step = StartWith(builder, sourceStep);

            sourceStep = _source.Steps.FirstOrDefault(x => x.Id == sourceStep.NextStepId);
            while (sourceStep != null)
            {
                step = Then(builder, step, sourceStep);

                sourceStep = _source.Steps.FirstOrDefault(x => x.Id == sourceStep.NextStepId);
            }

        }


        public dynamic StartWith(IWorkflowBuilder<object> builder, StepSourceV1 step)
        {
            var stepType = FindType(step.StepType);
            var stepBuilderType = typeof(IStepBuilder<,>).MakeGenericType(_dataType, stepType);
            var configActionType = typeof(Action<>).MakeGenericType(stepBuilderType);
            var m = builder.GetType().GetMethods();
            var genMethod = m.Single(x => x.Name == "StartWith" && x.ContainsGenericParameters);

            //var GenMethod = builder.GetType().GetMethod("StartWith", new Type[] { configActionType });
            var method = genMethod.MakeGenericMethod(stepType);
            var result = method.Invoke(builder, new object[] { null });

            result = AttachInputs(step, stepType, result);
            result = AttachOutputs(step, stepType, result);

            //var containerType = typeof(IContainerStepBuilder<,,>).MakeGenericType(_dataType, stepType, stepType);
            
            //if (containerType.IsInstanceOfType(result))
            //{
                
            //}

            //if (step.Do.Count > 0)
            //{

            //}
            

            return result;
        }

        public dynamic Then(IWorkflowBuilder<object> builder, dynamic previous, StepSourceV1 step)
        {
            var stepType = FindType(step.StepType);
            var stepBuilderType = typeof(IStepBuilder<,>).MakeGenericType(_dataType, stepType);
            var configActionType = typeof(Action<>).MakeGenericType(stepBuilderType);
            var genMethod = previous.GetType().GetMethod("Then", new Type[] { configActionType });
            var m = previous.GetType().GetMethods();
            //var genMethod = m.Single(x => x.Name == "Then" && x.ContainsGenericParameters);
            var method = genMethod.MakeGenericMethod(stepType);
            var result = method.Invoke(builder, new object[] { null });

            result = AttachInputs(step, stepType, result);
            result = AttachOutputs(step, stepType, result);

            return result;
        }

        private object AttachOutputs(StepSourceV1 step, Type stepType, object result)
        {
            foreach (var output in step.Outputs)
            {
                var sourceExpr = Expression.Property(Expression.Parameter(_dataType), output.Source);
                var targetExpr = Expression.Property(Expression.Parameter(stepType), output.Target);

                result = typeof(IStepBuilder<,>)
                    .MakeGenericType(_dataType, stepType)
                    .GetMethod("Output")
                    .Invoke(result, new[] { sourceExpr, targetExpr });
            }

            return result;
        }

        private object AttachInputs(StepSourceV1 step, Type stepType, object result)
        {
            foreach (var input in step.Inputs)
            {
                var sourceExpr = Expression.Property(Expression.Parameter(_dataType), input.Source);
                var targetExpr = Expression.Property(Expression.Parameter(stepType), input.Target);

                result = typeof(IStepBuilder<,>)
                    .MakeGenericType(_dataType, stepType)
                    .GetMethod("Input")
                    .Invoke(result, new[] { targetExpr, sourceExpr });
            }

            return result;
        }

        private Type FindType(string name)
        {
            return Type.GetType(name);
        }
    }
}
