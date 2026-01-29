using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.AI.AzureFoundry.Services;
using Xunit;
using FluentAssertions;

namespace WorkflowCore.AI.AzureFoundry.Tests
{
    public class InMemoryConversationStoreTests
    {
        private readonly InMemoryConversationStore _store;

        public InMemoryConversationStoreTests()
        {
            _store = new InMemoryConversationStore();
        }

        [Fact]
        public async Task GetOrCreateThreadAsync_ShouldCreateNewThread()
        {
            // Act
            var thread = await _store.GetOrCreateThreadAsync("workflow-1", "pointer-1");

            // Assert
            thread.Should().NotBeNull();
            thread.WorkflowInstanceId.Should().Be("workflow-1");
            thread.ExecutionPointerId.Should().Be("pointer-1");
            thread.Messages.Should().BeEmpty();
        }

        [Fact]
        public async Task GetOrCreateThreadAsync_ShouldReturnExistingThread()
        {
            // Arrange
            var firstThread = await _store.GetOrCreateThreadAsync("workflow-1", "pointer-1");
            firstThread.AddUserMessage("Hello");
            await _store.SaveThreadAsync(firstThread);

            // Act
            var secondThread = await _store.GetOrCreateThreadAsync("workflow-1", "pointer-1");

            // Assert
            secondThread.Id.Should().Be(firstThread.Id);
            secondThread.Messages.Should().HaveCount(1);
        }

        [Fact]
        public async Task SaveAndGetThread_ShouldPersistMessages()
        {
            // Arrange
            var thread = new ConversationThread
            {
                WorkflowInstanceId = "workflow-2",
                ExecutionPointerId = "pointer-2"
            };
            thread.AddSystemMessage("You are a helpful assistant");
            thread.AddUserMessage("Hello");
            thread.AddAssistantMessage("Hi there!");

            // Act
            await _store.SaveThreadAsync(thread);
            var retrieved = await _store.GetThreadAsync(thread.Id);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved.Messages.Should().HaveCount(3);
            retrieved.Messages[0].Role.Should().Be(MessageRole.System);
            retrieved.Messages[1].Role.Should().Be(MessageRole.User);
            retrieved.Messages[2].Role.Should().Be(MessageRole.Assistant);
        }

        [Fact]
        public async Task DeleteThreadAsync_ShouldRemoveThread()
        {
            // Arrange
            var thread = await _store.GetOrCreateThreadAsync("workflow-3", "pointer-3");
            await _store.SaveThreadAsync(thread);

            // Act
            await _store.DeleteThreadAsync(thread.Id);
            var retrieved = await _store.GetThreadAsync(thread.Id);

            // Assert
            retrieved.Should().BeNull();
        }
    }
}
