using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;
using WorkflowCore.Models.DefinitionStorage;
using WorkflowCore.Models.DefinitionStorage.v1;
using WorkflowCore.Exceptions;

namespace WorkflowCore.Services.DefinitionStorage
{
    public class DefinitionLoader : IDefinitionLoader
    {
        private readonly IWorkflowRegistry _registry;

        public DefinitionLoader(IWorkflowRegistry registry)
        {
            _registry = registry;
        }

        public WorkflowDefinition LoadDefinition(string source, Func<string, DefinitionSourceV1> deserializer)
        {
            var sourceObj = deserializer(source);
            var def = Convert(sourceObj);
            _registry.RegisterWorkflow(def);
            return def;
        }

        private WorkflowDefinition Convert(DefinitionSourceV1 source)
        {
            var dataType = typeof(object);
            if (!string.IsNullOrEmpty(source.DataType))
                dataType = FindType(source.DataType);

            var result = new WorkflowDefinition
            {
                Id = source.Id,
                Version = source.Version,
                Steps = ConvertSteps(source.Steps, dataType),
                DefaultErrorBehavior = source.DefaultErrorBehavior,
                DefaultErrorRetryInterval = source.DefaultErrorRetryInterval,
                Description = source.Description,
                DataType = dataType
            };

            return result;
        }


        private WorkflowStepCollection ConvertSteps(ICollection<StepSourceV1> source, Type dataType)
        {
            var result = new WorkflowStepCollection();
            int i = 0;
            var stack = new Stack<StepSourceV1>(source.Reverse<StepSourceV1>());
            var parents = new List<StepSourceV1>();
            var compensatables = new List<StepSourceV1>();

            while (stack.Count > 0)
            {
                var nextStep = stack.Pop();

                var stepType = FindType(nextStep.StepType);
                var containerType = typeof(WorkflowStep<>).MakeGenericType(stepType);
                var targetStep = (containerType.GetConstructor(new Type[] { }).Invoke(null) as WorkflowStep);

                if (nextStep.Saga)
                {
                    containerType = typeof(SagaContainer<>).MakeGenericType(stepType);
                    targetStep = (containerType.GetConstructor(new Type[] { }).Invoke(null) as WorkflowStep);
                }

                if (!string.IsNullOrEmpty(nextStep.CancelCondition))
                {
                    var cancelExprType = typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(dataType, typeof(bool)));
                    var dataParameter = Expression.Parameter(dataType, "data");
                    var cancelExpr = DynamicExpressionParser.ParseLambda(new[] { dataParameter }, typeof(bool), nextStep.CancelCondition);
                    targetStep.CancelCondition = cancelExpr;
                }

                targetStep.Id = i;
                targetStep.Name = nextStep.Name;
                targetStep.ErrorBehavior = nextStep.ErrorBehavior;
                targetStep.RetryInterval = nextStep.RetryInterval;
                targetStep.ExternalId = $"{nextStep.Id}";

                AttachInputs(nextStep, dataType, stepType, targetStep);
                AttachOutputs(nextStep, dataType, stepType, targetStep);

                if (nextStep.Do != null)
                {
                    foreach (var branch in nextStep.Do)
                    {
                        foreach (var child in branch.Reverse<StepSourceV1>())
                            stack.Push(child);
                    }

                    if (nextStep.Do.Count > 0)
                        parents.Add(nextStep);
                }

                if (nextStep.CompensateWith != null)
                {
                    foreach (var compChild in nextStep.CompensateWith.Reverse<StepSourceV1>())
                        stack.Push(compChild);

                    if (nextStep.CompensateWith.Count > 0)
                        compensatables.Add(nextStep);
                }

                if (!string.IsNullOrEmpty(nextStep.NextStepId))
                    targetStep.Outcomes.Add(new StepOutcome() { ExternalNextStepId = $"{nextStep.NextStepId}" });

                result.Add(targetStep);

                i++;
            }

            foreach (var step in result)
            {
                if (result.Any(x => x.ExternalId == step.ExternalId && x.Id != step.Id))
                    throw new WorkflowDefinitionLoadException($"Duplicate step Id {step.ExternalId}");

                foreach (var outcome in step.Outcomes)
                {
                    if (result.All(x => x.ExternalId != outcome.ExternalNextStepId))
                        throw new WorkflowDefinitionLoadException($"Cannot find step id {outcome.ExternalNextStepId}");

                    outcome.NextStep = result.Single(x => x.ExternalId == outcome.ExternalNextStepId).Id;
                }
            }

            foreach (var parent in parents)
            {
                var target = result.Single(x => x.ExternalId == parent.Id);
                foreach (var branch in parent.Do)
                {
                    var childTags = branch.Select(x => x.Id).ToList();
                    target.Children.AddRange(result
                        .Where(x => childTags.Contains(x.ExternalId))
                        .OrderBy(x => x.Id)
                        .Select(x => x.Id)
                        .Take(1)
                        .ToList());
                }
            }

            foreach (var item in compensatables)
            {
                var target = result.Single(x => x.ExternalId == item.Id);
                var tag = item.CompensateWith.Select(x => x.Id).FirstOrDefault();
                if (tag != null)
                {
                    var compStep = result.FirstOrDefault(x => x.ExternalId == tag);
                    if (compStep != null)
                        target.CompensationStepId = compStep.Id;
                }
            }

            return result;
        }

        private void AttachInputs(StepSourceV1 source, Type dataType, Type stepType, WorkflowStep step)
        {
            foreach (var input in source.Inputs)
            {
                var dataParameter = Expression.Parameter(dataType, "data");
                var contextParameter = Expression.Parameter(typeof(IStepExecutionContext), "context");
                var environmentVarsParameter = Expression.Parameter(typeof(IDictionary), "environment");
                var stepProperty = stepType.GetProperty(input.Key);

                if (stepProperty == null)
                {
                    throw new ArgumentException($"Unknown property for input {input.Key} on {source.Id}");
                }

                if (input.Value is string)
                {
                    var acn = BuildScalarInputAction(input, dataParameter, contextParameter, environmentVarsParameter, stepProperty);
                    step.Inputs.Add(new ActionParameter<IStepBody, object>(acn));
                    continue;
                }

                if ((input.Value is IDictionary<string, object>) || (input.Value is IDictionary<object, object>))
                {
                    var acn = BuildObjectInputAction(input, dataParameter, contextParameter, environmentVarsParameter, stepProperty);
                    step.Inputs.Add(new ActionParameter<IStepBody, object>(acn));
                    continue;
                }

                throw new ArgumentException($"Unknown type for input {input.Key} on {source.Id}");
            }
        }

        private void AttachOutputs(StepSourceV1 source, Type dataType, Type stepType, WorkflowStep step)
        {
            foreach (var output in source.Outputs)
            {
                var stepParameter = Expression.Parameter(stepType, "step");
                var sourceExpr = DynamicExpressionParser.ParseLambda(new[] { stepParameter }, typeof(object), output.Value);

                var dataParameter = Expression.Parameter(dataType, "data");
                Expression targetProperty;

                // Check if our datatype has a matching property
                var propertyInfo = dataType.GetProperty(output.Key);
                if (propertyInfo != null)
                {
                    targetProperty = Expression.Property(dataParameter, propertyInfo);
                    var targetExpr = Expression.Lambda(targetProperty, dataParameter);
                    step.Outputs.Add(new MemberMapParameter(sourceExpr, targetExpr));
                }
                else
                {
                    // If we did not find a matching property try to find a Indexer with string parameter
                    propertyInfo = dataType.GetProperty("Item");
                    targetProperty = Expression.Property(dataParameter, propertyInfo, Expression.Constant(output.Key));

                    Action<IStepBody, object> acn = (pStep, pData) =>
                    {
                        object resolvedValue = sourceExpr.Compile().DynamicInvoke(pStep); ;
                        propertyInfo.SetValue(pData, resolvedValue, new object[] { output.Key });
                    };

                    step.Outputs.Add(new ActionParameter<IStepBody, object>(acn));
                }
            }
        }

        private Type FindType(string name)
        {
            return Type.GetType(name, true, true);
        }

        private static Action<IStepBody, object, IStepExecutionContext> BuildScalarInputAction(KeyValuePair<string, object> input, ParameterExpression dataParameter, ParameterExpression contextParameter, ParameterExpression environmentVarsParameter, PropertyInfo stepProperty)
        {
            var expr = System.Convert.ToString(input.Value);
            var sourceExpr = DynamicExpressionParser.ParseLambda(new[] { dataParameter, contextParameter, environmentVarsParameter }, typeof(object), expr);

            void acn(IStepBody pStep, object pData, IStepExecutionContext pContext)
            {
                object resolvedValue = sourceExpr.Compile().DynamicInvoke(pData, pContext, Environment.GetEnvironmentVariables());
                if (stepProperty.PropertyType.IsEnum)
                    stepProperty.SetValue(pStep, Enum.Parse(stepProperty.PropertyType, (string)resolvedValue, true));
                else
                    stepProperty.SetValue(pStep, System.Convert.ChangeType(resolvedValue, stepProperty.PropertyType));
            }
            return acn;
        }

        private static Action<IStepBody, object, IStepExecutionContext> BuildObjectInputAction(KeyValuePair<string, object> input, ParameterExpression dataParameter, ParameterExpression contextParameter, ParameterExpression environmentVarsParameter, PropertyInfo stepProperty)
        {
            void acn(IStepBody pStep, object pData, IStepExecutionContext pContext)
            {
                var stack = new Stack<JObject>();
                var destObj = JObject.FromObject(input.Value);
                stack.Push(destObj);

                while (stack.Count > 0)
                {
                    var subobj = stack.Pop();
                    foreach (var prop in subobj.Properties().ToList())
                    {
                        if (prop.Name.StartsWith("@"))
                        {
                            var sourceExpr = DynamicExpressionParser.ParseLambda(new[] { dataParameter, contextParameter, environmentVarsParameter }, typeof(object), prop.Value.ToString());
                            object resolvedValue = sourceExpr.Compile().DynamicInvoke(pData, pContext, Environment.GetEnvironmentVariables());
                            subobj.Remove(prop.Name);
                            subobj.Add(prop.Name.TrimStart('@'), JToken.FromObject(resolvedValue));
                        }
                    }

                    foreach (var child in subobj.Children<JObject>())
                        stack.Push(child);
                }

                stepProperty.SetValue(pStep, destObj);
            }
            return acn;
        }

    }
}
