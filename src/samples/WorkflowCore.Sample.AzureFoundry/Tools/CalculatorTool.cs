using System;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.Sample.AzureFoundry.Tools
{
    /// <summary>
    /// Sample tool that performs mathematical calculations
    /// </summary>
    public class CalculatorTool : IAgentTool
    {
        public string Name => "calculator";
        
        public string Description => "Perform mathematical calculations. Supports basic arithmetic (+, -, *, /), parentheses, and common math operations.";
        
        public string ParametersSchema => @"{
            ""type"": ""object"",
            ""properties"": {
                ""expression"": {
                    ""type"": ""string"",
                    ""description"": ""The mathematical expression to evaluate (e.g., '2 + 2', '(10 * 5) / 2', '3.14 * 2')""
                }
            },
            ""required"": [""expression""]
        }";

        public Task<ToolResult> ExecuteAsync(string toolCallId, string arguments, CancellationToken cancellationToken = default)
        {
            try
            {
                var args = JsonSerializer.Deserialize<CalculatorArgs>(arguments, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Use DataTable.Compute for simple expression evaluation
                var table = new DataTable();
                var result = table.Compute(args.Expression, null);

                var response = new
                {
                    expression = args.Expression,
                    result = Convert.ToDouble(result)
                };

                return Task.FromResult(ToolResult.Succeeded(
                    toolCallId, 
                    Name, 
                    JsonSerializer.Serialize(response)));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Failed(
                    toolCallId, 
                    Name, 
                    $"Failed to evaluate expression: {ex.Message}"));
            }
        }

        private class CalculatorArgs
        {
            public string Expression { get; set; }
        }
    }
}
