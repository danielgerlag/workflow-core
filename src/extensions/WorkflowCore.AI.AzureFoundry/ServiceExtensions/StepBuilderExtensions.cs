using System;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.AI.AzureFoundry.Services;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    /// <summary>
    /// Extension methods for adding AI steps to workflows
    /// </summary>
    public static class AzureFoundryStepBuilderExtensions
    {
        /// <summary>
        /// Add a chat completion step
        /// </summary>
        public static IChatCompletionBuilder<TData> ChatCompletion<TData, TStepBody>(
            this IStepBuilder<TData, TStepBody> builder,
            Action<IChatCompletionBuilder<TData>> configure = null)
            where TStepBody : IStepBody
        {
            var newStep = new ChatCompletionStep();
            builder.WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new ChatCompletionBuilder<TData>(builder.WorkflowBuilder, newStep);

            configure?.Invoke(stepBuilder);

            newStep.Name = newStep.Name ?? nameof(ChatCompletion);
            builder.Step.Outcomes.Add(new ValueOutcome { NextStep = newStep.Id });

            return stepBuilder;
        }

        /// <summary>
        /// Add an agent loop step
        /// </summary>
        public static IAgentLoopBuilder<TData> AgentLoop<TData, TStepBody>(
            this IStepBuilder<TData, TStepBody> builder,
            Action<IAgentLoopBuilder<TData>> configure = null)
            where TStepBody : IStepBody
        {
            var newStep = new AgentLoopStep();
            builder.WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new AgentLoopBuilder<TData>(builder.WorkflowBuilder, newStep);

            configure?.Invoke(stepBuilder);

            newStep.Name = newStep.Name ?? nameof(AgentLoop);
            builder.Step.Outcomes.Add(new ValueOutcome { NextStep = newStep.Id });

            return stepBuilder;
        }

        /// <summary>
        /// Add a human review step
        /// </summary>
        public static IHumanReviewBuilder<TData> HumanReview<TData, TStepBody>(
            this IStepBuilder<TData, TStepBody> builder,
            Action<IHumanReviewBuilder<TData>> configure = null)
            where TStepBody : IStepBody
        {
            var newStep = new HumanReviewStep();
            builder.WorkflowBuilder.AddStep(newStep);
            var stepBuilder = new HumanReviewBuilder<TData>(builder.WorkflowBuilder, newStep);

            configure?.Invoke(stepBuilder);

            newStep.Name = newStep.Name ?? nameof(HumanReview);
            builder.Step.Outcomes.Add(new ValueOutcome { NextStep = newStep.Id });

            return stepBuilder;
        }

        /// <summary>
        /// Add an embedding generation step
        /// </summary>
        public static IStepBuilder<TData, GenerateEmbedding> GenerateEmbedding<TData, TStepBody>(
            this IStepBuilder<TData, TStepBody> builder,
            Action<IStepBuilder<TData, GenerateEmbedding>> configure = null)
            where TStepBody : IStepBody
        {
            var stepBuilder = builder.Then<GenerateEmbedding>();
            configure?.Invoke(stepBuilder);
            return stepBuilder;
        }

        /// <summary>
        /// Add a vector search step
        /// </summary>
        public static IStepBuilder<TData, VectorSearch> VectorSearch<TData, TStepBody>(
            this IStepBuilder<TData, TStepBody> builder,
            Action<IStepBuilder<TData, VectorSearch>> configure = null)
            where TStepBody : IStepBody
        {
            var stepBuilder = builder.Then<VectorSearch>();
            configure?.Invoke(stepBuilder);
            return stepBuilder;
        }

        /// <summary>
        /// Add a tool execution step
        /// </summary>
        public static IStepBuilder<TData, ExecuteTool> ExecuteTool<TData, TStepBody>(
            this IStepBuilder<TData, TStepBody> builder,
            Action<IStepBuilder<TData, ExecuteTool>> configure = null)
            where TStepBody : IStepBody
        {
            var stepBuilder = builder.Then<ExecuteTool>();
            configure?.Invoke(stepBuilder);
            return stepBuilder;
        }
    }
}
