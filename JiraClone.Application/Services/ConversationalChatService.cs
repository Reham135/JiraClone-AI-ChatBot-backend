using System.Text;
using JiraClone.Application.DTOs.Chat;
using JiraClone.Application.DTOs.Tasks;
using JiraClone.Application.Interfaces;
using JiraClone.Domain.Enums;
using JiraClone.Infrastructure.Repositories;
using TaskStatus = JiraClone.Domain.Enums.TaskStatus;

namespace JiraClone.Application.Services
{
    /// <summary>
    /// Handles multi-turn conversational task creation
    /// </summary>
    public partial class ChatService
    {
        public async Task<ChatResponseDto> ProcessConversationMessageAsync(
            ChatRequestDto request,
            ulong userId)
        {
            // Get or create conversation
            var conversation = _conversationManager.GetConversation(userId);

            // Check if user wants to start a new conversation
            if (conversation == null && IsStartConversationIntent(request.Message))
            {
                return await StartNewConversation(userId);
            }

            // If no active conversation, return guidance
            if (conversation == null)
            {
                return new ChatResponseDto
                {
                    Message = "👋 Hi! I can help you create tasks with a guided conversation.\n\n" +
                              "Just say **\"create task\"** or **\"new task\"** to get started!"
                };
            }

            // Process current step
            return await ProcessConversationStep(conversation, request.Message, userId);
        }

        private bool IsStartConversationIntent(string message)
        {
            var lower = message.ToLower().Trim();
            var startKeywords = new[] { "create task", "new task", "add task", "make task",
                                       "create bug", "new bug", "create story", "new story" };

            return startKeywords.Any(keyword => lower.Contains(keyword));
        }

        private async Task<ChatResponseDto> StartNewConversation(ulong userId)
        {
            var conversation = new ConversationState
            {
                UserId = userId,
                CurrentStep = ConversationStep.AskType
            };

            _conversationManager.SaveConversation(conversation);

            return AskType();
        }

        private async Task<ChatResponseDto> ProcessConversationStep(
            ConversationState conversation,
            string userInput,
            ulong userId)
        {
            try
            {
                switch (conversation.CurrentStep)
                {
                    case ConversationStep.AskType:
                        return await HandleTypeResponse(conversation, userInput);

                    case ConversationStep.AskProject:
                        return await HandleProjectResponse(conversation, userInput, userId);

                    case ConversationStep.AskDescription:
                        return await HandleDescriptionResponse(conversation, userInput);

                    case ConversationStep.AskPriority:
                        return await HandlePriorityResponse(conversation, userInput);

                    case ConversationStep.AskAssignee:
                        return await HandleAssigneeResponse(conversation, userInput);

                    case ConversationStep.AskDueDate:
                        return await HandleDueDateResponse(conversation, userInput);

                    case ConversationStep.AskSprintOrBacklog:
                        return await HandleSprintResponse(conversation, userInput);

                    case ConversationStep.AskAttachment:
                        return await HandleAttachmentResponse(conversation, userInput, userId);

                    case ConversationStep.UploadAttachment:
                        return await HandleFileUploadResponse(conversation, userInput, userId);

                    default:
                        return new ChatResponseDto { Message = "Something went wrong. Let's start over. Say 'create task' to begin." };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Conversation Error: {ex}");
                _conversationManager.ClearConversation(userId);
                return new ChatResponseDto
                {
                    Message = "I encountered an error. Let's start fresh. Say 'create task' to begin a new conversation."
                };
            }
        }

        // ==================== STEP HANDLERS ====================

        private ChatResponseDto AskType()
        {
            return new ChatResponseDto
            {
                Message = "What type of item would you like to create?",
                Choices = new List<ChatChoice>
                {
                    new() { Label = "🐛 Bug", Value = "Bug", Description = "Something that needs fixing" },
                    new() { Label = "📋 Task", Value = "Task", Description = "General work item" },
                    new() { Label = "📖 Story", Value = "Story", Description = "User story or feature" }
                }
            };
        }

        private async Task<ChatResponseDto> HandleTypeResponse(ConversationState conversation, string input)
        {
            // Parse type
            var type = input.ToLower() switch
            {
                "bug" or "🐛 bug" => TaskType.Bug,
                "story" or "📖 story" => TaskType.Story,
                _ => TaskType.Task
            };

            conversation.Type = type;
            conversation.CurrentStep = ConversationStep.AskProject;
            _conversationManager.SaveConversation(conversation);

            return await AskProject(conversation.UserId);
        }

        private async Task<ChatResponseDto> AskProject(ulong userId)
        {
            var projects = await _unitOfWork.Projects.GetAllAsync();

            if (!projects.Any())
            {
                return new ChatResponseDto
                {
                    Message = "❌ No projects found. Please create a project first before creating tasks."
                };
            }

            return new ChatResponseDto
            {
                Message = "Which project should this be added to?",
                Choices = projects.Select(p => new ChatChoice
                {
                    Label = p.Name,
                    Value = p.Id.ToString(),
                    Description = p.Description?.Length > 50 ? p.Description.Substring(0, 47) + "..." : p.Description
                }).ToList()
            };
        }

        private async Task<ChatResponseDto> HandleProjectResponse(ConversationState conversation, string input, ulong userId)
        {
            if (ulong.TryParse(input, out var projectId))
            {
                conversation.ProjectId = projectId;
                conversation.CurrentStep = ConversationStep.AskDescription;
                _conversationManager.SaveConversation(conversation);

                return AskDescription();
            }

            return new ChatResponseDto { Message = "Invalid project. Please try again." };
        }

        private ChatResponseDto AskDescription()
        {
            return new ChatResponseDto
            {
                Message = "📝 Please describe what needs to be done",
                ExpectsInput = true,
                InputType = "text"
            };
        }

        private async Task<ChatResponseDto> HandleDescriptionResponse(ConversationState conversation, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ChatResponseDto { Message = "Description cannot be empty. Please describe the task." };
            }

            conversation.Description = input;

            // Generate smart title using AI
            var title = await GenerateSmartTitleAsync(input, conversation.Type);
            conversation.Title = title;

            conversation.CurrentStep = ConversationStep.AskPriority;
            _conversationManager.SaveConversation(conversation);

            return AskPriority();
        }

        private ChatResponseDto AskPriority()
        {
            return new ChatResponseDto
            {
                Message = "What priority should this have?",
                Choices = new List<ChatChoice>
                {
                    new() { Label = "🔴 Critical", Value = "Critical", Description = "Needs immediate attention" },
                    new() { Label = "🟠 High", Value = "High", Description = "Important" },
                    new() { Label = "🟡 Medium", Value = "Medium", Description = "Normal priority" },
                    new() { Label = "🟢 Low", Value = "Low", Description = "Can wait" }
                }
            };
        }

        private async Task<ChatResponseDto> HandlePriorityResponse(ConversationState conversation, string input)
        {
            var priority = input.ToLower().Replace("🔴 ", "").Replace("🟠 ", "").Replace("🟡 ", "").Replace("🟢 ", "").Trim() switch
            {
                "critical" => TaskPriority.Critical,
                "high" => TaskPriority.High,
                "low" => TaskPriority.Low,
                _ => TaskPriority.Medium
            };

            conversation.Priority = priority;
            conversation.CurrentStep = ConversationStep.AskAssignee;
            _conversationManager.SaveConversation(conversation);

            return AskAssignee();
        }

        private ChatResponseDto AskAssignee()
        {
            return new ChatResponseDto
            {
                Message = "👤 Who should I assign this to? (Enter first name, or type 'skip' to leave unassigned)",
                ExpectsInput = true,
                InputType = "text"
            };
        }

        private async Task<ChatResponseDto> HandleAssigneeResponse(ConversationState conversation, string input)
        {
            if (input.ToLower() != "skip")
            {
                var assigneeId = await FindUserByFirstName(input);
                conversation.AssigneeId = assigneeId;
                conversation.AssigneeName = input;
            }

            conversation.CurrentStep = ConversationStep.AskDueDate;
            _conversationManager.SaveConversation(conversation);

            return AskDueDate();
        }

        private ChatResponseDto AskDueDate()
        {
            return new ChatResponseDto
            {
                Message = "📅 When is this due? (e.g., '2025-01-15', 'tomorrow', 'next week', or 'skip')",
                ExpectsInput = true,
                InputType = "date"
            };
        }

        private async Task<ChatResponseDto> HandleDueDateResponse(ConversationState conversation, string input)
        {
            if (input.ToLower() != "skip")
            {
                conversation.DueDate = ParseDueDate(input);
            }

            conversation.CurrentStep = ConversationStep.AskSprintOrBacklog;
            _conversationManager.SaveConversation(conversation);

            return await AskSprintOrBacklog(conversation.ProjectId!.Value);
        }

        private async Task<ChatResponseDto> AskSprintOrBacklog(ulong projectId)
        {
            var sprints = (await _unitOfWork.Sprints
                .FindAsync(s => s.ProjectId == projectId))
                .OrderByDescending(s => s.Status)
                .ThenByDescending(s => s.CreatedAt)
                .ToList();

            var choices = new List<ChatChoice>();

            foreach (var sprint in sprints.Take(5))
            {
                var statusIcon = sprint.Status == SprintStatus.Active ? "🟢" : "⚪";
                choices.Add(new ChatChoice
                {
                    Label = $"{statusIcon} {sprint.Name}",
                    Value = sprint.Id.ToString(),
                    Description = $"{sprint.StartDate:MMM dd} - {(sprint.EndDate?.ToString("MMM dd") ?? "Ongoing")}"
                });
            }

            choices.Add(new ChatChoice
            {
                Label = "📝 Backlog",
                Value = "backlog",
                Description = "Add to product backlog"
            });

            return new ChatResponseDto
            {
                Message = "Should this go in a sprint or the backlog?",
                Choices = choices
            };
        }

        private async Task<ChatResponseDto> HandleSprintResponse(ConversationState conversation, string input)
        {
            if (input != "backlog" && ulong.TryParse(input, out var sprintId))
            {
                conversation.SprintId = sprintId;
            }

            conversation.CurrentStep = ConversationStep.AskAttachment;
            _conversationManager.SaveConversation(conversation);

            return AskAttachment();
        }

        private ChatResponseDto AskAttachment()
        {
            return new ChatResponseDto
            {
                Message = "📎 Would you like to upload any attachments?",
                Choices = new List<ChatChoice>
                {
                    new() { Label = "Yes, upload image", Value = "yes" },
                    new() { Label = "No, skip", Value = "no" }
                }
            };
        }

        private async Task<ChatResponseDto> HandleAttachmentResponse(
            ConversationState conversation,
            string input,
            ulong userId)
        {
            if (input.ToLower() == "yes")
            {
                conversation.WantsAttachment = true;
                conversation.CurrentStep = ConversationStep.UploadAttachment;
                _conversationManager.SaveConversation(conversation);

                return new ChatResponseDto
                {
                    Message = "📤 Please upload your image",
                    ExpectsInput = true,
                    InputType = "file"
                };
            }

            // No attachment, create task
            return await CreateTaskFromConversation(conversation, userId);
        }

        private async Task<ChatResponseDto> HandleFileUploadResponse(
            ConversationState conversation,
            string input,
            ulong userId)
        {
            // Handle user choice after file upload
            if (input.ToLower() == "upload_more")
            {
                // User wants to upload another file
                return new ChatResponseDto
                {
                    Message = "📤 Please upload another image",
                    ExpectsInput = true,
                    InputType = "file"
                };
            }

            // User chose "create_task" or any other input - proceed to create task
            return await CreateTaskFromConversation(conversation, userId);
        }

        private async Task<ChatResponseDto> CreateTaskFromConversation(
            ConversationState conversation,
            ulong userId)
        {
            var createTaskDto = new CreateTaskDto
            {
                Title = conversation.Title ?? "Untitled Task",
                Description = conversation.Description,
                Type = conversation.Type!.Value,
                Priority = conversation.Priority!.Value,
                Status = TaskStatus.ToDo,
                ProjectId = conversation.ProjectId!.Value,
                SprintId = conversation.SprintId,
                AssigneeId = conversation.AssigneeId,
                DueDate = conversation.DueDate
            };

            var task = await _taskService.CreateTaskAsync(createTaskDto, userId);

            // Handle file attachments if any
            var attachmentCount = 0;
            if (conversation.AttachmentPaths.Any())
            {
                attachmentCount = await AttachFilesToTask(task.Id, conversation.AttachmentPaths, userId);
            }

            // Clear conversation
            _conversationManager.ClearConversation(userId);

            // Build summary message
            var summary = new StringBuilder();
            summary.AppendLine($"✅ **Task created successfully!**\n");
            summary.AppendLine($"**{task.TaskKey}**: {task.Title}");
            summary.AppendLine($"📋 Type: {task.TypeName}");
            summary.AppendLine($"🎯 Priority: {task.PriorityName}");

            if (task.DueDate.HasValue)
                summary.AppendLine($"📅 Due: {task.DueDate.Value:MMM dd, yyyy}");

            if (conversation.AssigneeName != null)
                summary.AppendLine($"👤 Assigned to: {conversation.AssigneeName}");

            if (attachmentCount > 0)
                summary.AppendLine($"📎 Attachments: {attachmentCount} file(s)");

            return new ChatResponseDto
            {
                Message = summary.ToString(),
                TaskCreated = new TaskCreatedDto
                {
                    Id = task.Id,
                    TaskKey = task.TaskKey ?? $"TASK-{task.Id}",
                    Title = task.Title,
                    Status = task.StatusName ?? "To Do"
                }
            };
        }

        // ==================== HELPER METHODS ====================

        private async Task<string> GenerateSmartTitleAsync(string description, TaskType? taskType)
        {
            try
            {
                var taskTypeStr = taskType?.ToString() ?? "Task";
                var systemPrompt = $@"You are an expert at creating concise, clear task titles for a Jira-like project management system.

Given a task description, generate a short, actionable title that summarizes the main goal.

Rules:
1. Maximum 8 words (preferably 5-6 words)
2. Start with a verb when possible (e.g., 'Fix', 'Add', 'Update', 'Implement', 'Create')
3. Be specific and clear
4. Use title case
5. No punctuation at the end
6. For {taskTypeStr} type, make it relevant to that context
7. Return ONLY the title text, nothing else

Examples:
Description: 'We need to add a login feature that allows users to authenticate with email and password'
Title: Add User Email Login Feature

Description: 'The dashboard is loading very slowly when there are more than 100 tasks'
Title: Fix Slow Dashboard Loading Issue

Description: 'Create a new API endpoint for retrieving user profile information'
Title: Create User Profile API Endpoint";

                var userPrompt = $"Task Type: {taskTypeStr}\nDescription: {description}";

                // Use existing CallXAIAsync method from parent class
                var title = await CallXAIAsync(systemPrompt, userPrompt);

                // Clean up the response (remove quotes, extra whitespace)
                title = title.Trim().Trim('"', '\'', '.', '!');

                // Fallback to simple extraction if AI returns something too long or invalid
                if (string.IsNullOrWhiteSpace(title) || title.Length > 100)
                {
                    return GenerateTitleFromDescription(description);
                }

                return title;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating smart title: {ex.Message}");
                // Fallback to simple title generation
                return GenerateTitleFromDescription(description);
            }
        }

        private string GenerateTitleFromDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "Untitled Task";

            // Take first sentence or first 100 chars
            var firstSentence = description.Split('.', '!', '?')[0].Trim();
            return firstSentence.Length > 100
                ? firstSentence.Substring(0, 97) + "..."
                : firstSentence;
        }

        private async Task<ulong?> FindUserByFirstName(string firstName)
        {
            var users = await _unitOfWork.Users
                .FindAsync(u => u.FirstName.ToLower().Contains(firstName.ToLower()));

            return users.FirstOrDefault()?.Id;
        }

        private DateTime? ParseDueDate(string input)
        {
            // Try ISO format
            if (DateTime.TryParse(input, out var date))
                return date;

            // Handle relative dates
            var lower = input.ToLower();
            if (lower.Contains("tomorrow"))
                return DateTime.Today.AddDays(1);
            if (lower.Contains("next week"))
                return DateTime.Today.AddDays(7);
            if (lower.Contains("monday"))
                return GetNextWeekday(DayOfWeek.Monday);
            if (lower.Contains("friday"))
                return GetNextWeekday(DayOfWeek.Friday);

            return null;
        }

        private DateTime GetNextWeekday(DayOfWeek day)
        {
            var today = DateTime.Today;
            int daysToAdd = ((int)day - (int)today.DayOfWeek + 7) % 7;
            if (daysToAdd == 0) daysToAdd = 7; // Next occurrence
            return today.AddDays(daysToAdd);
        }

        private async Task<int> AttachFilesToTask(ulong taskId, List<string> tempFilePaths, ulong userId)
        {
            var attachmentCount = 0;

            try
            {
                // Get upload directory path (same as AttachmentService uses)
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadPath);

                foreach (var tempFilePath in tempFilePaths)
                {
                    try
                    {
                        if (!File.Exists(tempFilePath))
                            continue;

                        // Get file info
                        var fileInfo = new FileInfo(tempFilePath);
                        var extension = fileInfo.Extension;
                        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                        var permanentPath = Path.Combine(uploadPath, uniqueFileName);

                        // Copy file to permanent location
                        File.Copy(tempFilePath, permanentPath, true);

                        // Create attachment entity
                        var attachment = new Domain.Entities.Attachment
                        {
                            TaskId = taskId,
                            FileName = Path.GetFileName(tempFilePath),
                            FilePath = $"/uploads/{uniqueFileName}",
                            FileType = GetContentType(extension),
                            FileSize = fileInfo.Length,
                            UploadedBy = userId,
                            UploadedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Attachments.AddAsync(attachment);
                        attachmentCount++;

                        // Delete temp file
                        File.Delete(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to attach file {tempFilePath}: {ex.Message}");
                    }
                }

                // Save all attachments to database
                if (attachmentCount > 0)
                {
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error attaching files to task: {ex}");
            }

            return attachmentCount;
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
