using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;

namespace WorkflowCore.UnitTests
{
    public class MemberMapParameterTests
    {

        [Fact]
        public void should_assign_input()
        {
            Expression<Func<MyStep, int>> memberExpr = (x => x.Value1);
            Expression<Func<MyData, int>> valueExpr = (x => x.Value1);
            var subject = new MemberMapParameter(valueExpr, memberExpr);
            var data = new MyData
            {
                Value1 = 5
            };
            var step = new MyStep();

            subject.AssignInput(data, step, new StepExecutionContext());

            step.Value1.Should().Be(data.Value1);
        }

        [Fact]
        public void should_assign_output()
        {
            Expression<Func<MyData, int>> memberExpr = (x => x.Value1);
            Expression<Func<MyStep, int>> valueExpr = (x => x.Value1);
            var subject = new MemberMapParameter(valueExpr, memberExpr);
            var data = new MyData();
            var step = new MyStep
            {
                Value1 = 5
            };

            subject.AssignOutput(data, step, new StepExecutionContext());

            data.Value1.Should().Be(step.Value1);
        }
        [Fact]
        public void should_assign_output_with_context()
        {
            Expression<Func<MyData, object>> memberExpr = (x => x.Value2);
            Expression<Func<MyStep, StepExecutionContext, object>> valueExpr = ((step, context) => ((string[])step.Value2)[(int)context.Item]);
            var subject = new MemberMapParameter(valueExpr, memberExpr);
            var data = new MyData();
            var step = new MyStep {
                Value2 = new []{"A", "B", "C", "D"}
            };

            var context = new StepExecutionContext {Item = 2};

            subject.AssignOutput(data, step, context);

            data.Value2.Should().Be("C");
        }

        [Fact]
        public void should_convert_input()
        {
            Expression<Func<MyStep, object>> memberExpr = (x => x.Value2);
            Expression<Func<MyData, int>> valueExpr = (x => x.Value1);
            var subject = new MemberMapParameter(valueExpr, memberExpr);

            var data = new MyData
            {
                Value1 = 5
            };

            var step = new MyStep();

            subject.AssignInput(data, step, new StepExecutionContext());

            step.Value2.Should().Be(data.Value1);
        }

        [Fact]
        public void should_convert_output()
        {
            Expression<Func<MyData, object>> memberExpr = (x => x.Value2);
            Expression<Func<MyStep, int>> valueExpr = (x => x.Value1);
            var subject = new MemberMapParameter(valueExpr, memberExpr);

            var data = new MyData
            {
                Value1 = 5
            };

            var step = new MyStep();

            subject.AssignOutput(data, step, new StepExecutionContext());

            data.Value2.Should().Be(step.Value1);
        }


        class MyData
        {
            public int Value1 { get; set; }
            public object Value2 { get; set; }
        }

        class MyStep : IStepBody
        {
            public int Value1 { get; set; }
            public object Value2 { get; set; }

            public Task<ExecutionResult> RunAsync(IStepExecutionContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
