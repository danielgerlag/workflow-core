using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WorkflowCore.Interface;
using WorkflowCore.Services;
using Xunit;

namespace WorkflowCore.UnitTests.Services
{
    public class ScopeProviderTests
    {
        private readonly ScopeProvider _sut;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;

        public ScopeProviderTests()
        {
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();

            _sut = new ScopeProvider(_scopeFactoryMock.Object);
        }

        [Fact(DisplayName = "Should return IServiceScope")]
        public void ReturnsServiceScope_CreateScopeCalled()
        {
            var scope = new Mock<IServiceScope>().Object;
            _scopeFactoryMock.Setup(x => x.CreateScope())
                .Returns(scope);
            
            var result = _sut.CreateScope(new Mock<IStepExecutionContext>().Object);

            result.Should().NotBeNull().And.BeSameAs(scope);
            _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        }
    }
}