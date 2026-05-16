using WorkflowCore.AI.AzureFoundry.Models;
using Xunit;
using FluentAssertions;

namespace WorkflowCore.AI.AzureFoundry.Tests
{
    public class ConversationThreadTests
    {
        [Fact]
        public void AddMessage_ShouldUpdateTimestamp()
        {
            // Arrange
            var thread = new ConversationThread();
            var originalUpdatedAt = thread.UpdatedAt;

            // Act
            System.Threading.Thread.Sleep(10);
            thread.AddUserMessage("Hello");

            // Assert
            thread.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public void AddSystemMessage_ShouldAddCorrectRole()
        {
            // Arrange
            var thread = new ConversationThread();

            // Act
            thread.AddSystemMessage("You are helpful");

            // Assert
            thread.Messages.Should().ContainSingle(m => 
                m.Role == MessageRole.System && 
                m.Content == "You are helpful");
        }

        [Fact]
        public void AddUserMessage_ShouldAddCorrectRole()
        {
            // Arrange
            var thread = new ConversationThread();

            // Act
            thread.AddUserMessage("Hello");

            // Assert
            thread.Messages.Should().ContainSingle(m => 
                m.Role == MessageRole.User && 
                m.Content == "Hello");
        }

        [Fact]
        public void AddAssistantMessage_ShouldAddCorrectRole()
        {
            // Arrange
            var thread = new ConversationThread();

            // Act
            thread.AddAssistantMessage("Hi there!");

            // Assert
            thread.Messages.Should().ContainSingle(m => 
                m.Role == MessageRole.Assistant && 
                m.Content == "Hi there!");
        }

        [Fact]
        public void AddToolMessage_ShouldAddCorrectRoleAndMetadata()
        {
            // Arrange
            var thread = new ConversationThread();

            // Act
            thread.AddToolMessage("call-123", "search_tool", "results here");

            // Assert
            thread.Messages.Should().ContainSingle(m => 
                m.Role == MessageRole.Tool && 
                m.ToolCallId == "call-123" && 
                m.ToolName == "search_tool" &&
                m.Content == "results here");
        }

        [Fact]
        public void AddMessage_WithTokenCount_ShouldUpdateTotalTokens()
        {
            // Arrange
            var thread = new ConversationThread();

            // Act
            thread.AddMessage(new ConversationMessage
            {
                Role = MessageRole.User,
                Content = "Hello",
                TokenCount = 5
            });
            thread.AddMessage(new ConversationMessage
            {
                Role = MessageRole.Assistant,
                Content = "Hi there!",
                TokenCount = 10
            });

            // Assert
            thread.TotalTokens.Should().Be(15);
        }
    }
}
