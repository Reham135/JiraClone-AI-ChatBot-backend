using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JiraClone.Application.DTOs.Chat;
using JiraClone.Application.DTOs.Tasks;
using JiraClone.Application.Interfaces;
using JiraClone.Domain.Enums;
using JiraClone.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using TaskStatus = JiraClone.Domain.Enums.TaskStatus;

namespace JiraClone.Application.Services
{
    public partial class ChatService : IChatService
    {
        private readonly ITaskService _taskService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConversationStateManager _conversationManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _apiKey;
        private readonly string _model;
        private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";

        public ChatService(
            ITaskService taskService,
            IHttpClientFactory httpClientFactory,
            IConversationStateManager conversationManager,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _taskService = taskService;
            _httpClientFactory = httpClientFactory;
            _conversationManager = conversationManager;
            _unitOfWork = unitOfWork;
            _apiKey = configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API Key not configured");
            _model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";
        }

        public async Task<ChatResponseDto> ProcessMessageAsync(ChatRequestDto request, ulong userId)
        {
            try
            {
                // PRIORITY 1: Check for active conversation or start new one
                var conversation = _conversationManager.GetConversation(userId);

                if (conversation != null || IsStartConversationIntent(request.Message))
                {
                    return await ProcessConversationMessageAsync(request, userId);
                }

                // PRIORITY 2: Fall back to single-message mode
                var isTaskCreationRequest = await AnalyzeMessageIntent(request.Message);

                if (isTaskCreationRequest)
                {
                    // Extract task details using X.AI
                    var taskDetails = await ExtractTaskDetailsAsync(request.Message, request.ProjectId);

                    if (taskDetails != null && request.ProjectId.HasValue)
                    {
                        // Create the task
                        var createTaskDto = new CreateTaskDto
                        {
                            Title = taskDetails.Title,
                            Description = taskDetails.Description,
                            Status = taskDetails.Status,
                            Priority = taskDetails.Priority,
                            Type = taskDetails.Type,
                            ProjectId = request.ProjectId.Value,
                            SprintId = request.SprintId,
                            AssigneeId = request.AssigneeId ?? taskDetails.AssigneeId,
                            DueDate = taskDetails.DueDate
                        };

                        var createdTask = await _taskService.CreateTaskAsync(createTaskDto, userId);

                        // Build success message with details
                        var messageBuilder = new System.Text.StringBuilder();
                        messageBuilder.Append($"Great! I've created the {createdTask.PriorityName} priority {createdTask.TypeName} task '{createdTask.Title}'");

                        if (createdTask.DueDate.HasValue)
                        {
                            messageBuilder.Append($" due on {createdTask.DueDate.Value:MMM dd, yyyy}");
                        }

                        messageBuilder.Append(".");

                        return new ChatResponseDto
                        {
                            Message = messageBuilder.ToString(),
                            TaskCreated = new TaskCreatedDto
                            {
                                Id = createdTask.Id,
                                TaskKey = createdTask.TaskKey ?? $"TASK-{createdTask.Id}",
                                Title = createdTask.Title,
                                Status = createdTask.StatusName ?? "To Do"
                            }
                        };
                    }
                    else if (!request.ProjectId.HasValue)
                    {
                        return new ChatResponseDto
                        {
                            Message = "I'd love to create a task for you! However, I need to know which project this task should be added to. Please select a project first."
                        };
                    }
                    else
                    {
                        return new ChatResponseDto
                        {
                            Message = "I couldn't extract enough details to create a task. Could you provide more information? For example: 'Create a task to fix the login bug with high priority'"
                        };
                    }
                }
                else
                {
                    // Generate conversational response
                    var response = await GenerateConversationalResponse(request.Message);
                    return new ChatResponseDto
                    {
                        Message = response
                    };
                }
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                Console.WriteLine($"ChatService Error: {ex}");

                return new ChatResponseDto
                {
                    Message = $"I encountered an error while processing your request. Please try again with a valid email address for assignment (e.g., john@example.com) or contact support if the issue persists."
                };
            }
        }

        private async Task<bool> AnalyzeMessageIntent(string message)
        {
            // Quick heuristic check for task creation keywords
            var taskKeywords = new[] { "create", "add", "new task", "make a task", "task for", "bug", "feature", "story" };
            var lowerMessage = message.ToLower();

            return taskKeywords.Any(keyword => lowerMessage.Contains(keyword));
        }

        private async Task<ExtractedTaskDetails?> ExtractTaskDetailsAsync(string message, ulong? projectId)
        {
            var systemPrompt = @"You are a helpful task extraction assistant for a Jira-like project management system.
Analyze the user's message and extract task details. Return ONLY a valid JSON object with these fields:
- title (string, required): A concise task title (max 100 chars)
- description (string, optional): Detailed task description
- type (string): one of 'Task', 'Bug', or 'Story' (default: 'Task')
- priority (string): one of 'Low', 'Medium', 'High', or 'Critical' (default: 'Medium')
- status (string): one of 'ToDo', 'InProgress', or 'Done' (default: 'ToDo')
- assigneeEmail (string, optional): MUST be a valid email address (e.g., user@example.com). Only extract if message contains a valid email. Do NOT extract plain names.
- dueDate (string, optional): ISO date format YYYY-MM-DD (extract from 'due', 'deadline', 'by', dates like 'tomorrow', 'next week', '2025-01-15')

Rules:
1. If the message mentions 'bug' or 'fix', set type to 'Bug' and priority to at least 'High'
2. If the message mentions 'urgent', 'critical', or 'asap', set priority to 'Critical'
3. If the message mentions 'feature' or 'story', set type to 'Story'
4. Extract assignee from patterns like 'assign to john@example.com', 'for Sarah', '@mike'
5. Extract due date from patterns like 'due Jan 15', 'deadline 2025-01-20', 'by tomorrow'
6. For relative dates: tomorrow = today + 1 day, next week = today + 7 days (use ISO format)
7. Return ONLY the JSON object, no additional text or markdown

Examples:
Input: 'Create a high priority task to fix the login bug due tomorrow assign to john@example.com'
Output: {""title"":""Fix login bug"",""description"":""Fix the login bug"",""type"":""Bug"",""priority"":""High"",""status"":""ToDo"",""assigneeEmail"":""john@example.com"",""dueDate"":""2025-12-18""}

Input: 'Add feature for dark mode by next Friday for sarah@company.com'
Output: {""title"":""Add dark mode feature"",""description"":""Add feature for dark mode"",""type"":""Story"",""priority"":""Medium"",""status"":""ToDo"",""assigneeEmail"":""sarah@company.com"",""dueDate"":""2025-12-20""}";

            var userPrompt = $"User message: {message}";

            var response = await CallXAIAsync(systemPrompt, userPrompt);

            try
            {
                // Parse the JSON response
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

                var assigneeEmail = jsonResponse.TryGetProperty("assigneeEmail", out var assignee) ? assignee.GetString() : null;
                var dueDateStr = jsonResponse.TryGetProperty("dueDate", out var dueDate) ? dueDate.GetString() : null;
                DateTime? parsedDueDate = null;

                if (!string.IsNullOrEmpty(dueDateStr) && DateTime.TryParse(dueDateStr, out var tempDate))
                {
                    parsedDueDate = tempDate;
                }

                return new ExtractedTaskDetails
                {
                    Title = jsonResponse.GetProperty("title").GetString() ?? "Untitled Task",
                    Description = jsonResponse.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                    Type = ParseTaskType(jsonResponse.GetProperty("type").GetString()),
                    Priority = ParseTaskPriority(jsonResponse.GetProperty("priority").GetString()),
                    Status = ParseTaskStatus(jsonResponse.TryGetProperty("status", out var status) ? status.GetString() : "ToDo"),
                    AssigneeEmail = assigneeEmail,
                    DueDate = parsedDueDate
                };
            }
            catch (Exception)
            {
                // Fallback: Use rule-based extraction if AI fails
                return ExtractTaskDetailsWithRules(message);
            }
        }

        private async Task<string> GenerateConversationalResponse(string message)
        {
            var systemPrompt = @"You are a helpful project management assistant for a Jira-like system.
Respond to user queries about task management, project organization, and general help.
Be concise, friendly, and helpful. If users ask about creating tasks, guide them on how to phrase their request.
Keep responses under 150 words.";

            var response = await CallXAIAsync(systemPrompt, message);
            return response;
        }

        private async Task<string> CallXAIAsync(string systemPrompt, string userMessage)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.7,
                max_tokens = 500
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GROQ_API_URL, content);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Groq API request failed with status {response.StatusCode}. " +
                    $"Response: {responseContent}");
            }

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            return jsonResponse
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }

        private ExtractedTaskDetails ExtractTaskDetailsWithRules(string message)
        {
            var lowerMessage = message.ToLower();

            // Determine type
            var type = TaskType.Task;
            if (lowerMessage.Contains("bug") || lowerMessage.Contains("fix"))
                type = TaskType.Bug;
            else if (lowerMessage.Contains("story") || lowerMessage.Contains("feature"))
                type = TaskType.Story;

            // Determine priority
            var priority = TaskPriority.Medium;
            if (lowerMessage.Contains("critical") || lowerMessage.Contains("urgent") || lowerMessage.Contains("asap"))
                priority = TaskPriority.Critical;
            else if (lowerMessage.Contains("high priority") || lowerMessage.Contains("important"))
                priority = TaskPriority.High;
            else if (lowerMessage.Contains("low priority"))
                priority = TaskPriority.Low;
            else if (type == TaskType.Bug)
                priority = TaskPriority.High;

            // Extract title (simple heuristic)
            var title = message.Length > 100 ? message.Substring(0, 97) + "..." : message;

            return new ExtractedTaskDetails
            {
                Title = title,
                Description = message,
                Type = type,
                Priority = priority,
                Status = TaskStatus.ToDo
            };
        }

        private TaskType ParseTaskType(string? type)
        {
            return type?.ToLower() switch
            {
                "bug" => TaskType.Bug,
                "story" => TaskType.Story,
                _ => TaskType.Task
            };
        }

        private TaskPriority ParseTaskPriority(string? priority)
        {
            return priority?.ToLower() switch
            {
                "critical" => TaskPriority.Critical,
                "high" => TaskPriority.High,
                "low" => TaskPriority.Low,
                _ => TaskPriority.Medium
            };
        }

        private TaskStatus ParseTaskStatus(string? status)
        {
            return status?.ToLower() switch
            {
                "inprogress" => TaskStatus.InProgress,
                "done" => TaskStatus.Done,
                _ => TaskStatus.ToDo
            };
        }

        public async Task<ChatResponseDto> HandleFileUploadAsync(IFormFile file, ulong userId)
        {
            try
            {
                // Get active conversation
                var conversation = _conversationManager.GetConversation(userId);

                if (conversation == null || conversation.CurrentStep != ConversationStep.UploadAttachment)
                {
                    return new ChatResponseDto
                    {
                        Message = "No active file upload session. Please start a new conversation."
                    };
                }

                // Create temporary upload directory if it doesn't exist
                var tempUploadPath = Path.Combine(Path.GetTempPath(), "JiraClone", "ChatUploads", userId.ToString());
                Directory.CreateDirectory(tempUploadPath);

                // Generate unique filename
                var extension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(tempUploadPath, uniqueFileName);

                // Save file temporarily
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Store file info in conversation
                conversation.AttachmentPaths.Add(filePath);
                _conversationManager.SaveConversation(conversation);

                // Return success and ask if they want to upload more or continue
                return new ChatResponseDto
                {
                    Message = $"✅ File '{file.FileName}' uploaded successfully! Would you like to upload another image or create the task?",
                    Choices = new List<ChatChoice>
                    {
                        new() { Label = "Upload another", Value = "upload_more" },
                        new() { Label = "Create task now", Value = "create_task" }
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File upload error: {ex}");
                return new ChatResponseDto
                {
                    Message = "Failed to upload file. Please try again or skip this step."
                };
            }
        }

        public void ClearConversation(ulong userId)
        {
            _conversationManager.ClearConversation(userId);
        }

        private class ExtractedTaskDetails
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public TaskType Type { get; set; }
            public TaskPriority Priority { get; set; }
            public TaskStatus Status { get; set; }
            public ulong? AssigneeId { get; set; }
            public string? AssigneeEmail { get; set; }
            public DateTime? DueDate { get; set; }
        }
    }
}
