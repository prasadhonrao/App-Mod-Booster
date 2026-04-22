using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using System.Text.Json;

namespace ExpenseMgmt
{
    public interface IChatService
    {
        Task<string> GetResponseAsync(string userMessage);
    }

    public class ChatService : IChatService
    {
        private readonly IConfiguration _configuration;
        private readonly IExpenseRepository _expenseRepo;
        private readonly IUserRepository _userRepo;
        private readonly ICategoryRepository _catRepo;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IConfiguration configuration,
            IExpenseRepository expenseRepo,
            IUserRepository userRepo,
            ICategoryRepository catRepo,
            ILogger<ChatService> logger)
        {
            _configuration = configuration;
            _expenseRepo = expenseRepo;
            _userRepo = userRepo;
            _catRepo = catRepo;
            _logger = logger;
        }

        public async Task<string> GetResponseAsync(string userMessage)
        {
            var endpoint = _configuration["OpenAI:Endpoint"];
            var deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4o";

            if (string.IsNullOrEmpty(endpoint))
            {
                return "The GenAI services have not been deployed yet. " +
                       "Please run deploy-with-chat.sh to deploy Azure OpenAI and get the full chat experience.";
            }

            try
            {
                var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
                TokenCredential credential;
                if (!string.IsNullOrEmpty(managedIdentityClientId))
                {
                    _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                    credential = new ManagedIdentityCredential(managedIdentityClientId);
                }
                else
                {
                    _logger.LogInformation("Using DefaultAzureCredential");
                    credential = new DefaultAzureCredential();
                }

                var client = new OpenAIClient(new Uri(endpoint), credential);

                var tools = new List<ChatCompletionsFunctionToolDefinition>
                {
                    new ChatCompletionsFunctionToolDefinition
                    {
                        Name = "get_expenses",
                        Description = "Retrieves expenses from the database. Can filter by status (Draft, Submitted, Approved, Rejected) and/or userId.",
                        Parameters = BinaryData.FromObjectAsJson(new
                        {
                            type = "object",
                            properties = new
                            {
                                statusFilter = new { type = "string", description = "Filter by status: Draft, Submitted, Approved, Rejected" },
                                userId = new { type = "integer", description = "Filter by user ID" }
                            },
                            required = Array.Empty<string>()
                        })
                    },
                    new ChatCompletionsFunctionToolDefinition
                    {
                        Name = "get_users",
                        Description = "Retrieves all active users from the database.",
                        Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
                    },
                    new ChatCompletionsFunctionToolDefinition
                    {
                        Name = "get_categories",
                        Description = "Retrieves all expense categories.",
                        Parameters = BinaryData.FromObjectAsJson(new { type = "object", properties = new { } })
                    },
                    new ChatCompletionsFunctionToolDefinition
                    {
                        Name = "create_expense",
                        Description = "Creates a new expense record in the database.",
                        Parameters = BinaryData.FromObjectAsJson(new
                        {
                            type = "object",
                            properties = new
                            {
                                userId = new { type = "integer", description = "User ID" },
                                categoryId = new { type = "integer", description = "Category ID" },
                                amountMinor = new { type = "integer", description = "Amount in pence (e.g. 1234 for £12.34)" },
                                expenseDate = new { type = "string", description = "Date in yyyy-MM-dd format" },
                                description = new { type = "string", description = "Description of the expense" }
                            },
                            required = new[] { "userId", "categoryId", "amountMinor", "expenseDate" }
                        })
                    },
                    new ChatCompletionsFunctionToolDefinition
                    {
                        Name = "submit_expense",
                        Description = "Submits a draft expense for manager review.",
                        Parameters = BinaryData.FromObjectAsJson(new
                        {
                            type = "object",
                            properties = new { expenseId = new { type = "integer", description = "The expense ID to submit" } },
                            required = new[] { "expenseId" }
                        })
                    }
                };

                var options = new ChatCompletionsOptions(deploymentName, new[]
                {
                    new ChatRequestSystemMessage(
                        "You are a helpful AI assistant for an Expense Management system. " +
                        "You have access to real database functions to list, create, and manage expenses. " +
                        "When asked to list expenses, users, or categories - use the available functions. " +
                        "Format lists clearly with amounts in GBP (divide pence by 100). " +
                        "Currency is always GBP (£). Be concise and helpful."),
                    new ChatRequestUserMessage(userMessage)
                });

                foreach (var tool in tools)
                    options.Tools.Add(tool);

                // Agentic loop - keep calling until no more tool calls
                var toolMessages = new List<ChatRequestMessage>();
                Response<ChatCompletions> response;
                int iterations = 0;
                do
                {
                    foreach (var tm in toolMessages)
                        options.Messages.Add(tm);
                    toolMessages.Clear();

                    response = await client.GetChatCompletionsAsync(options);
                    var choice = response.Value.Choices[0];

                    if (choice.FinishReason == CompletionsFinishReason.ToolCalls)
                    {
                        options.Messages.Add(new ChatRequestAssistantMessage(choice.Message));
                        foreach (var toolCall in choice.Message.ToolCalls.OfType<ChatCompletionsFunctionToolCall>())
                        {
                            var result = await ExecuteToolAsync(toolCall.Name, toolCall.Arguments);
                            toolMessages.Add(new ChatRequestToolMessage(result, toolCall.Id));
                        }
                    }
                    else
                    {
                        return choice.Message.Content;
                    }
                    iterations++;
                } while (iterations < 5);

                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatService.cs:GetResponseAsync");
                return $"Chat service error: {ex.Message} | File: ChatService.cs, Method: GetResponseAsync. " +
                       "Check that AZURE_CLIENT_ID and OpenAI:Endpoint are set correctly in App Service configuration.";
            }
        }

        private async Task<string> ExecuteToolAsync(string functionName, string arguments)
        {
            try
            {
                using var doc = JsonDocument.Parse(arguments);
                var args = doc.RootElement;

                switch (functionName)
                {
                    case "get_expenses":
                    {
                        string? statusFilter = args.TryGetProperty("statusFilter", out var sf) ? sf.GetString() : null;
                        int? userId = args.TryGetProperty("userId", out var ui) ? ui.GetInt32() : null;
                        var expenses = await _expenseRepo.GetAllExpensesAsync(statusFilter, userId);
                        return JsonSerializer.Serialize(expenses);
                    }
                    case "get_users":
                    {
                        var users = await _userRepo.GetAllUsersAsync();
                        return JsonSerializer.Serialize(users);
                    }
                    case "get_categories":
                    {
                        var cats = await _catRepo.GetCategoriesAsync();
                        return JsonSerializer.Serialize(cats);
                    }
                    case "create_expense":
                    {
                        var req = new CreateExpenseRequest
                        {
                            UserId = args.GetProperty("userId").GetInt32(),
                            CategoryId = args.GetProperty("categoryId").GetInt32(),
                            AmountMinor = args.GetProperty("amountMinor").GetInt32(),
                            ExpenseDate = DateTime.Parse(args.GetProperty("expenseDate").GetString()!),
                            Description = args.TryGetProperty("description", out var d) ? d.GetString() : null
                        };
                        var newId = await _expenseRepo.CreateExpenseAsync(req);
                        return JsonSerializer.Serialize(new { expenseId = newId, success = true });
                    }
                    case "submit_expense":
                    {
                        var expenseId = args.GetProperty("expenseId").GetInt32();
                        var ok = await _expenseRepo.SubmitExpenseAsync(expenseId);
                        return JsonSerializer.Serialize(new { success = ok });
                    }
                    default:
                        return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {FunctionName} - ChatService.cs:ExecuteToolAsync", functionName);
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }
    }
}
