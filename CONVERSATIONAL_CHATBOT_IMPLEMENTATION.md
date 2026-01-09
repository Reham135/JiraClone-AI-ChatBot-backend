# Conversational Chatbot Implementation Plan

## Overview
Transform the chatbot from single-message extraction to a **guided, multi-turn conversation** that walks users through task creation step-by-step.

---

## Conversation Flow

```
START: User says "create task" or similar
  ↓
STEP 1: "What type of item would you like to create?"
  Choices: [🐛 Bug] [📋 Task] [📖 Story]
  ↓
STEP 2: "Which project should this be added to?"
  Choices: [Project A] [Project B] [Project C] ...
  ↓
STEP 3: "Please describe what needs to be done"
  Input: Free text description
  ↓
STEP 4: "What priority should this have?"
  Choices: [🔴 Critical] [🟠 High] [🟡 Medium] [🟢 Low]
  ↓
STEP 5: "Who should I assign this to?"
  Input: First name (e.g., "John", "Sarah")
  → System looks up user by first name
  ↓
STEP 6: "When is this due?"
  Input: Date (e.g., "2025-01-15", "tomorrow", "next week")
  ↓
STEP 7: "Should this go in a sprint or backlog?"
  Choices: [Sprint 1 (Active)] [Sprint 2] [Backlog]
  ↓
STEP 8: "Would you like to upload any attachments?"
  Choices: [Yes, upload image] [No, skip]
  ↓
  IF YES → Upload image(s)
  IF NO → Skip
  ↓
FINAL: Create task and show summary with link
```

---

## Backend Implementation

### 1. Conversation State Management ✅ DONE

**Files Created:**
- `ConversationState.cs` - Tracks progress and collected data
- `ConversationStateManager.cs` - In-memory state storage
- Updated `ChatResponseDto.cs` - Added Choices, ExpectsInput, InputType

**State Properties:**
```csharp
- ConversationId: Unique ID
- UserId: Current user
- CurrentStep: Which step we're on
- Type, ProjectId, Description, Priority, etc.: Collected data
- AttachmentPaths: Uploaded files
```

### 2. Conversation Flow Logic (TODO)

**Update ProcessMessageAsync:**
```csharp
public async Task<ChatResponseDto> ProcessMessageAsync(...)
{
    // 1. Check for active conversation
    var conversation = _conversationManager.GetConversation(userId);

    if (conversation != null)
    {
        // Continue existing conversation
        return await ProcessConversationStep(conversation, request.Message, userId);
    }

    // 2. Check if user wants to start conversation
    if (IsStartConversationIntent(request.Message))
    {
        // Start new conversation
        return await StartConversation(userId);
    }

    // 3. Fall back to old single-message mode
    return await ProcessSingleMessage(request, userId);
}
```

### 3. Step Handlers (TODO)

Create method for each step:

**Step 1: Ask Type**
```csharp
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
```

**Step 2: Ask Project**
```csharp
private async Task<ChatResponseDto> AskProject(ulong userId)
{
    var projects = await _unitOfWork.Projects
        .FindAsync(p => p.CreatedBy == userId || /* user has access */);

    return new ChatResponseDto
    {
        Message = "Which project should this be added to?",
        Choices = projects.Select(p => new ChatChoice
        {
            Label = p.Name,
            Value = p.Id.ToString(),
            Description = p.Description
        }).ToList()
    };
}
```

**Step 3: Ask Description**
```csharp
private ChatResponseDto AskDescription()
{
    return new ChatResponseDto
    {
        Message = "Please describe what needs to be done",
        ExpectsInput = true,
        InputType = "text"
    };
}
```

**Step 4: Ask Priority**
```csharp
private ChatResponseDto AskPriority()
{
    return new ChatResponseDto
    {
        Message = "What priority should this have?",
        Choices = new List<ChatChoice>
        {
            new() { Label = "🔴 Critical", Value = "Critical" },
            new() { Label = "🟠 High", Value = "High" },
            new() { Label = "🟡 Medium", Value = "Medium" },
            new() { Label = "🟢 Low", Value = "Low" }
        }
    };
}
```

**Step 5: Ask Assignee**
```csharp
private ChatResponseDto AskAssignee()
{
    return new ChatResponseDto
    {
        Message = "Who should I assign this to? (Enter first name)",
        ExpectsInput = true,
        InputType = "text"
    };
}

private async Task<ulong?> FindUserByFirstName(string firstName)
{
    var users = await _unitOfWork.Users
        .FindAsync(u => u.FirstName.ToLower().Contains(firstName.ToLower()));

    return users.FirstOrDefault()?.Id;
}
```

**Step 6: Ask Due Date**
```csharp
private ChatResponseDto AskDueDate()
{
    return new ChatResponseDto
    {
        Message = "When is this due? (e.g., '2025-01-15', 'tomorrow', 'next week')",
        ExpectsInput = true,
        InputType = "date"
    };
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

    return null;
}
```

**Step 7: Ask Sprint/Backlog**
```csharp
private async Task<ChatResponseDto> AskSprintOrBacklog(ulong projectId)
{
    var sprints = await _unitOfWork.Sprints
        .FindAsync(s => s.ProjectId == projectId && s.Status == SprintStatus.Active);

    var choices = sprints.Select(s => new ChatChoice
    {
        Label = $"📅 {s.Name}",
        Value = s.Id.ToString(),
        Description = $"{s.StartDate:MMM dd} - {s.EndDate:MMM dd}"
    }).ToList();

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
```

**Step 8: Ask Attachment**
```csharp
private ChatResponseDto AskAttachment()
{
    return new ChatResponseDto
    {
        Message = "Would you like to upload any attachments?",
        Choices = new List<ChatChoice>
        {
            new() { Label = "Yes, upload image", Value = "yes" },
            new() { Label = "No, skip", Value = "no" }
        }
    };
}
```

**Final Step: Create Task**
```csharp
private async Task<ChatResponseDto> CreateTaskFromConversation(
    ConversationState conversation,
    ulong userId)
{
    var createTaskDto = new CreateTaskDto
    {
        Title = GenerateTitleFromDescription(conversation.Description),
        Description = conversation.Description,
        Type = conversation.Type.Value,
        Priority = conversation.Priority.Value,
        Status = TaskStatus.ToDo,
        ProjectId = conversation.ProjectId.Value,
        SprintId = conversation.SprintId,
        AssigneeId = conversation.AssigneeId,
        DueDate = conversation.DueDate
    };

    var task = await _taskService.CreateTaskAsync(createTaskDto, userId);

    // Upload attachments if any
    if (conversation.AttachmentPaths.Any())
    {
        // Handle attachment uploads
    }

    // Clear conversation
    _conversationManager.ClearConversation(userId);

    return new ChatResponseDto
    {
        Message = $"✅ Task created successfully!\n\n" +
                  $"**{task.TaskKey}**: {task.Title}\n" +
                  $"Priority: {task.PriorityName}\n" +
                  $"Type: {task.TypeName}",
        TaskCreated = new TaskCreatedDto
        {
            Id = task.Id,
            TaskKey = task.TaskKey,
            Title = task.Title,
            Status = task.StatusName
        }
    };
}
```

### 4. Service Registration (TODO)

**Program.cs:**
```csharp
builder.Services.AddSingleton<IConversationStateManager, ConversationStateManager>();
builder.Services.AddHostedService<ConversationCleanupService>(); // Background cleanup
```

---

## Frontend Implementation

### 1. Update ChatbotComponent (TODO)

**Add Choice Buttons:**
```typescript
// In template
@if (currentResponse?.choices && currentResponse.choices.length > 0) {
  <div class="choice-buttons">
    @for (choice of currentResponse.choices; track choice.value) {
      <button
        class="choice-btn"
        (click)="selectChoice(choice.value)">
        {{ choice.label }}
        @if (choice.description) {
          <span class="choice-desc">{{ choice.description }}</span>
        }
      </button>
    }
  </div>
}
```

**Handle Button Clicks:**
```typescript
selectChoice(value: string): void {
  this.sendMessage(value);
}
```

### 2. Add File Upload (TODO)

**Template:**
```html
@if (currentResponse?.inputType === 'file') {
  <div class="file-upload">
    <input
      type="file"
      accept="image/*"
      (change)="onFileSelected($event)"
      #fileInput>
    <button (click)="fileInput.click()">Choose Image</button>
  </div>
}
```

**Component:**
```typescript
selectedFile: File | null = null;

onFileSelected(event: any): void {
  this.selectedFile = event.target.files[0];
  if (this.selectedFile) {
    this.uploadFile();
  }
}

uploadFile(): void {
  const formData = new FormData();
  formData.append('file', this.selectedFile!);

  this.chatbotService.uploadAttachment(formData).subscribe({
    next: (response) => {
      this.sendMessage('File uploaded');
    }
  });
}
```

### 3. Update ChatbotService (TODO)

**Add File Upload Method:**
```typescript
uploadAttachment(formData: FormData): Observable<any> {
  return this.http.post(`${this.apiUrl}/upload`, formData);
}
```

### 4. Update Chat Models (TODO)

**Add to ChatResponse:**
```typescript
export interface ChatResponse {
  message: string;
  taskCreated?: TaskCreated;
  choices?: ChatChoice[];
  expectsInput?: boolean;
  inputType?: 'text' | 'date' | 'file';
}

export interface ChatChoice {
  label: string;
  value: string;
  description?: string;
}
```

---

## File Upload Handler (TODO)

### Backend: Upload Endpoint

**ChatController:**
```csharp
[HttpPost("upload")]
public async Task<IActionResult> UploadAttachment(IFormFile file)
{
    var userId = GetCurrentUserId();
    var conversation = _conversationManager.GetConversation(userId);

    if (conversation == null)
        return BadRequest("No active conversation");

    // Save file
    var filePath = await SaveFileAsync(file);
    conversation.AttachmentPaths.Add(filePath);
    _conversationManager.SaveConversation(conversation);

    return Ok(new { filePath });
}
```

---

## Testing Checklist

- [ ] Start conversation with "create task"
- [ ] Select Bug/Task/Story
- [ ] Choose project from list
- [ ] Enter description
- [ ] Select priority
- [ ] Assign to user by first name
- [ ] Set due date (various formats)
- [ ] Choose sprint or backlog
- [ ] Upload attachment (optional)
- [ ] Verify task created with all fields
- [ ] Check task link works
- [ ] Test conversation timeout (30 min)
- [ ] Test canceling mid-conversation
- [ ] Test multiple users simultaneously

---

## Estimated Completion Time

- Backend conversation flow: 2-3 hours
- Frontend UI updates: 1-2 hours
- File upload: 1 hour
- Testing & debugging: 1-2 hours
- **Total: 5-8 hours**

---

## Current Status

✅ Conversation state system
✅ DTOs updated
✅ ChatService dependencies added
⏳ Conversation flow logic - IN PROGRESS
⏳ Frontend updates - PENDING
⏳ File upload - PENDING
⏳ Testing - PENDING
