using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.Sample.AzureFoundry.Tools
{
    /// <summary>
    /// Sample tool that provides weather information (simulated)
    /// </summary>
    public class WeatherTool : IAgentTool
    {
        public string Name => "weather";
        
        public string Description => "Get the current weather for a specified city. Returns temperature, conditions, and humidity.";
        
        public string ParametersSchema => @"{
            ""type"": ""object"",
            ""properties"": {
                ""city"": {
                    ""type"": ""string"",
                    ""description"": ""The city name to get weather for (e.g., 'London', 'New York')""
                }
            },
            ""required"": [""city""]
        }";

        public Task<ToolResult> ExecuteAsync(string toolCallId, string arguments, CancellationToken cancellationToken = default)
        {
            try
            {
                var args = JsonSerializer.Deserialize<WeatherArgs>(arguments, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Simulated weather data
                var random = new Random();
                var temp = random.Next(0, 35);
                var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Partly Cloudy", "Overcast" };
                var condition = conditions[random.Next(conditions.Length)];
                var humidity = random.Next(30, 90);

                var result = new
                {
                    city = args.City,
                    temperature_celsius = temp,
                    temperature_fahrenheit = (temp * 9 / 5) + 32,
                    conditions = condition,
                    humidity_percent = humidity
                };

                return Task.FromResult(ToolResult.Succeeded(
                    toolCallId, 
                    Name, 
                    JsonSerializer.Serialize(result)));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Failed(toolCallId, Name, ex.Message));
            }
        }

        private class WeatherArgs
        {
            public string City { get; set; }
        }
    }
}
