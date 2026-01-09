# JiraClone - Project Management System

A full-stack project management application inspired by Jira, built with .NET 8 and Angular 17. Features real-time notifications via SignalR and AI-powered task creation using natural language processing.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technologies](#technologies)
- [Features](#features)
- [Database Schema](#database-schema)
- [AI Integration](#ai-integration)
- [Real-Time Notifications](#real-time-notifications)
- [Getting Started](#getting-started)
- [API Documentation](#api-documentation)
- [Project Structure](#project-structure)
- [Contributing](#contributing)

---

## Overview

JiraClone is a comprehensive project management system that enables teams to:
- Organize work into projects and sprints
- Create and track tasks with multiple types (Task, Bug, Story)
- Collaborate with comments and mentions
- Receive real-time notifications via WebSocket
- Create tasks using AI-powered natural language processing
- Visualize workflow with Kanban boards

---

## Architecture

The project follows **Clean Architecture** principles with clear separation of concerns:

### Backend (.NET 8)

```
JiraClone/
├── JiraClone.Domain/          # Core entities and enums
│   ├── Entities/              # 7 domain models
│   └── Enums/                 # 4 enumerations
├── JiraClone.Infrastructure/  # Data access layer
│   ├── Data/                  # DbContext, migrations
│   ├── Repositories/          # Repository implementations
│   └── UnitOfWork/            # Unit of Work pattern
├── JiraClone.Application/     # Business logic layer
│   ├── Services/              # 10 service implementations
│   ├── Interfaces/            # Service contracts
│   └── DTOs/                  # Data transfer objects
└── JiraClone.API/             # Presentation layer
    ├── Controllers/           # 10 REST API controllers
    ├── Hubs/                  # SignalR notification hub
    └── Program.cs             # Application configuration
```

**Design Patterns:**
- Repository Pattern
- Unit of Work
- Dependency Injection
- DTOs for data transfer
- AutoMapper for object mapping

### Frontend (Angular 17)

```
JiraCloneFront/
├── auth/                      # Authentication module
├── core/
│   ├── guards/                # Route protection
│   ├── interceptors/          # JWT token injection
│   └── services/              # 9 core services
├── features/
│   ├── board/                 # Kanban board with drag-drop
│   ├── projects/              # Project management
│   ├── tasks/                 # Task CRUD operations
│   ├── sprints/               # Sprint lifecycle
│   ├── backlog/               # Task backlog
│   └── dashboard/             # Overview dashboard
└── shared/
    ├── components/
    │   ├── chatbot/           # AI assistant widget
    │   ├── layout/            # Navigation & notifications
    │   └── toast/             # Toast notifications
    └── models/                # TypeScript interfaces
```

---

## Technologies

### Backend Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Backend framework |
| ASP.NET Core Web API | 8.0 | RESTful API |
| Entity Framework Core | 8.0.11 | ORM for database access |
| SQL Server | LocalDB | Database engine |
| SignalR | 1.2.9 | Real-time communication |
| JWT Bearer | 8.2.1 | Authentication |
| AutoMapper | 12.0.1 | Object-object mapping |
| ASP.NET Core Identity | 8.0.11 | User management |

### Frontend Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| Angular | 17.3.0 | Frontend framework |
| TypeScript | 5.4.2 | Type-safe JavaScript |
| Angular Material | 17.3.10 | UI component library |
| Angular CDK | 17.3.10 | Drag-drop functionality |
| @microsoft/signalr | 10.0.0 | WebSocket client |
| RxJS | 7.8.0 | Reactive programming |

### External APIs

- **Groq API** - AI-powered task creation (llama-3.3-70b-versatile model)

---

## Features

### Core Functionality

#### 1. User Authentication & Authorization
- JWT-based authentication with refresh tokens
- Secure registration with email validation
- Password policies (8+ chars, uppercase, lowercase, digit)
- Account lockout after 5 failed attempts (5-minute duration)
- Protected API endpoints with role-based access

#### 2. Project Management
- Create, update, and delete projects
- Project overview with task statistics
- Multi-user collaboration
- Project-level task organization

#### 3. Task/Issue Management
- **Task Types**: Task, Bug, Story
- **Priority Levels**: Low, Medium, High, Critical
- **Status Tracking**: To Do, In Progress, Done
- Rich text descriptions
- Tag support for categorization
- Due date management
- Assignee assignment with email lookup
- Task key generation (e.g., TASK-123)
- File attachments (images, documents)

#### 4. Sprint Management
- Create and manage sprints with goals
- Three sprint statuses: Planned, Active, Completed
- Start and complete sprint workflows
- Associate tasks with sprints
- Backlog for unassigned tasks

#### 5. Kanban Board
- Visual drag-and-drop interface
- Three columns: To Do, In Progress, Done
- Real-time updates across users
- Filter by sprint and assignee
- Priority color coding
- Quick task preview

#### 6. Comments & Collaboration
- Task-level commenting
- User mentions (notifications on mention)
- Comment timestamps and author tracking
- Edit and delete own comments

#### 7. Real-Time Notifications (SignalR)
- Instant notifications for mentions
- Persistent WebSocket connections
- Automatic reconnection with exponential backoff
- User-specific notification groups
- Read/unread status tracking
- Database persistence for offline users
- **Performance**: 99% reduction in latency (30s → <100ms)

#### 8. AI-Powered Task Creation
- Natural language task creation
- Two modes: Single-message and Conversational
- Intelligent extraction of task details
- Smart title generation
- Priority and type inference
- Email-based assignee detection
- Due date parsing (relative and absolute)
- File upload support during conversation

#### 9. File Attachments
- Multi-format support (.jpg, .png, .gif, .webp, etc.)
- File size limit: 50MB per file
- Metadata tracking (filename, size, type, uploader)
- Download and preview functionality

---

## Database Schema

The application uses **SQL Server** with Entity Framework Core for data access.

### Core Tables

#### 1. **AspNetUsers** (Identity)
Primary user management table extended with custom fields.

| Column | Type | Description |
|--------|------|-------------|
| Id | string (PK) | User identifier |
| FirstName | string | User's first name |
| LastName | string | User's last name |
| Email | string | User email (unique) |
| UserName | string | Username (unique) |
| PasswordHash | string | Hashed password |
| CreatedAt | DateTime | Account creation date |
| UpdatedAt | DateTime | Last update timestamp |

#### 2. **Projects**
Project organization and management.

| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Project identifier |
| Name | string | Project name |
| Description | string | Project description |
| CreatedBy | string (FK) | Creator user ID |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |

**Relationships:**
- `Creator` → User (Many-to-One)
- `Tasks` → TaskItem (One-to-Many, cascade delete)
- `Sprints` → Sprint (One-to-Many, cascade delete)

#### 3. **Tasks** (TaskItem)
Core task/issue tracking table.

| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Task identifier |
| Title | string | Task title |
| Description | string | Detailed description |
| Status | int | 0=ToDo, 1=InProgress, 2=Done |
| Priority | int | 0=Low, 1=Medium, 2=High, 3=Critical |
| Type | int | 0=Task, 1=Bug, 2=Story |
| AssigneeId | string (FK) | Assigned user ID |
| ProjectId | int (FK) | Parent project |
| SprintId | int? (FK) | Associated sprint (nullable) |
| DueDate | DateTime? | Due date (nullable) |
| Tags | string | Comma-separated tags |
| CreatedBy | string (FK) | Creator user ID |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |

**Relationships:**
- `Project` → Project (Many-to-One)
- `Sprint` → Sprint (Many-to-One)
- `Assignee` → User (Many-to-One)
- `Creator` → User (Many-to-One)
- `Comments` → Comment (One-to-Many, cascade delete)
- `Attachments` → Attachment (One-to-Many)

#### 4. **Sprints**
Sprint planning and tracking.

| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Sprint identifier |
| Name | string | Sprint name |
| Goal | string | Sprint goal/objective |
| StartDate | DateTime | Sprint start date |
| EndDate | DateTime | Sprint end date |
| Status | int | 0=Planned, 1=Active, 2=Completed |
| ProjectId | int (FK) | Parent project |
| CreatedBy | string (FK) | Creator user ID |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |

**Relationships:**
- `Project` → Project (Many-to-One)
- `Creator` → User (Many-to-One)
- `Tasks` → TaskItem (One-to-Many)

#### 5. **Comments**
Task-level discussions and collaboration.

| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Comment identifier |
| Content | string | Comment text |
| TaskId | int (FK) | Associated task |
| CreatedBy | string (FK) | Comment author |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |

**Relationships:**
- `Task` → TaskItem (Many-to-One, cascade delete)
- `Creator` → User (Many-to-One)

#### 6. **Notifications**
Real-time notification tracking.

| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Notification identifier |
| Message | string | Notification content |
| UserId | string (FK) | Target user |
| TaskId | int? (FK) | Related task (nullable) |
| CommentId | int? (FK) | Related comment (nullable) |
| IsRead | bool | Read status (default: false) |
| CreatedAt | DateTime | Creation timestamp |

**Relationships:**
- `User` → User (Many-to-One)
- `Task` → TaskItem (Many-to-One)
- `Comment` → Comment (Many-to-One)

#### 7. **Attachments**
File attachments for tasks.

| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Attachment identifier |
| TaskId | int (FK) | Associated task |
| FileName | string | Original filename |
| FilePath | string | Server storage path |
| FileType | string | MIME type |
| FileSize | long | File size in bytes |
| UploadedBy | string (FK) | Uploader user ID |
| UploadedAt | DateTime | Upload timestamp |

**Relationships:**
- `Task` → TaskItem (Many-to-One)
- `UploadedByUser` → User (Many-to-One)

### Database Enumerations

```csharp
public enum TaskStatus
{
    ToDo = 0,
    InProgress = 1,
    Done = 2
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum TaskType
{
    Task = 0,
    Bug = 1,
    Story = 2
}

public enum SprintStatus
{
    Planned = 0,
    Active = 1,
    Completed = 2
}
```

### Entity Relationships Diagram

```
User (AspNetUsers)
  |
  |--- (1:N) Projects (as Creator)
  |--- (1:N) Tasks (as Assignee)
  |--- (1:N) Tasks (as Creator)
  |--- (1:N) Sprints (as Creator)
  |--- (1:N) Comments (as Creator)
  |--- (1:N) Notifications (as Recipient)
  |--- (1:N) Attachments (as Uploader)

Project
  |
  |--- (1:N) Tasks (cascade delete)
  |--- (1:N) Sprints (cascade delete)
  |--- (N:1) User (Creator)

Sprint
  |
  |--- (1:N) Tasks
  |--- (N:1) Project
  |--- (N:1) User (Creator)

TaskItem
  |
  |--- (1:N) Comments (cascade delete)
  |--- (1:N) Attachments
  |--- (1:N) Notifications
  |--- (N:1) Project
  |--- (N:1) Sprint (nullable)
  |--- (N:1) User (Assignee, nullable)
  |--- (N:1) User (Creator)

Comment
  |
  |--- (1:N) Notifications
  |--- (N:1) TaskItem (cascade delete)
  |--- (N:1) User (Creator)
```

---

## AI Integration

The application features AI-powered task creation using natural language processing. The integration evolved through two approaches:

### Evolution of AI Integration

#### Phase 1: n8n Workflow Integration (Initial)
Initially, the AI chat functionality was implemented using **n8n** (workflow automation platform):
- n8n workflows handled AI request routing
- External webhook endpoints for AI processing
- Workflow-based task extraction and processing
- Required separate n8n instance deployment

#### Phase 2: Direct Groq API Integration (Current)
The system was refactored to use **direct Groq API integration** for better performance and simplicity:
- Direct HTTP calls to Groq API
- Reduced latency (no middleware)
- Simplified deployment (no external dependencies)
- Better error handling and fallback mechanisms

### Current AI Architecture

#### Groq API Configuration
```json
{
  "GroqApi": {
    "ApiKey": "your-groq-api-key",
    "BaseUrl": "https://api.groq.com/openai/v1/chat/completions",
    "Model": "llama-3.3-70b-versatile"
  }
}
```

#### AI Services

**1. ChatService (Single-Message Mode)**
- Analyzes user intent for task creation
- Extracts task details from natural language
- Generates concise task titles
- Infers priority and type from context
- Matches assignee by email
- Parses due dates (relative and absolute)
- Fallback to rule-based extraction on AI failure

**Example:**
```
User: "Create a high priority bug for fixing login issues, assign to john@example.com"
AI Extracts:
  - Title: "Fix login issues"
  - Type: Bug
  - Priority: High
  - Assignee: john@example.com
```

**2. ConversationalChatService (Multi-Turn Mode)**
- Guided step-by-step task creation
- Interactive conversation flow
- Dynamic project and sprint selection
- Smart title generation from descriptions
- File upload support during conversation
- 30-minute conversation timeout
- Context-aware responses

**Conversation Flow:**
1. Type selection (Task/Bug/Story)
2. Project selection (from user's projects)
3. Description input
4. Priority selection (Low/Medium/High/Critical)
5. Assignee selection (by first name)
6. Due date (relative or absolute)
7. Sprint selection (or backlog)
8. Optional file attachments

**3. ConversationStateManager**
- In-memory state management
- 30-minute TTL for conversations
- Automatic cleanup of stale conversations
- Thread-safe implementation

#### AI Features

**Intent Detection:**
- Keywords: "create task", "new bug", "add story"
- Context analysis for urgency
- Bug indicators: "error", "crash", "broken"
- Story indicators: "feature", "user wants", "as a user"

**Smart Extraction:**
- Priority keywords: "urgent", "critical", "high priority"
- Type keywords: "bug", "story", "feature"
- Date parsing: "tomorrow", "next week", "2024-12-31"
- Email extraction from text
- User lookup by first name

**Error Handling:**
- Graceful degradation to rule-based extraction
- Clear error messages for users
- Retry mechanisms for API failures
- Timeout handling (30-second timeout)

#### Chatbot UI Component
- Floating widget with minimize/maximize
- Message history with timestamps
- Typing indicators
- File upload with preview
- Clear conversation option
- Responsive design
- Smooth animations

---

## Real-Time Notifications

The application uses **SignalR** for instant, bi-directional communication between server and clients.

### Architecture

#### Backend (SignalR Hub)
```csharp
// Hub endpoint: /hubs/notifications
public class NotificationHub : Hub
{
    // User joins their personal group on connection
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    // Broadcast notification to specific user group
    public async Task SendNotification(string userId, string message)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", message);
    }
}
```

#### Frontend (SignalR Service)
```typescript
export class SignalRService {
  private hubConnection: signalR.HubConnection;

  // Automatic reconnection with exponential backoff
  private reconnectDelays = [0, 2000, 10000, 30000, 60000];

  // WebSocket with Long Polling fallback
  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/notifications', {
        accessTokenFactory: () => this.authService.getToken()
      })
      .withAutomaticReconnect(this.reconnectDelays)
      .build();
  }
}
```

### Features

- **Instant Delivery**: Notifications appear in <100ms (vs 30 seconds with polling)
- **Automatic Reconnection**: Exponential backoff (0s, 2s, 10s, 30s, 60s)
- **WebSocket Transport**: Primary transport with Long Polling fallback
- **JWT Authentication**: Token passed via query string
- **User Groups**: Each user has isolated notification channel
- **Persistent Storage**: Database backup for offline users
- **Read Status**: Track which notifications have been viewed

### Performance Improvements

| Metric | Before (Polling) | After (SignalR) | Improvement |
|--------|------------------|-----------------|-------------|
| Notification Latency | 30 seconds | <100ms | **99% faster** |
| Server Requests | 120/hour/user | 1/hour/user | **99% reduction** |
| Bandwidth Usage | ~500KB/hour | ~50KB/hour | **90% reduction** |
| Scalability | Limited | High | N/A |

### Connection Lifecycle

1. **User Login**: Frontend establishes WebSocket connection
2. **Authentication**: JWT token validated by SignalR hub
3. **Group Join**: User added to personal notification group (`user_{userId}`)
4. **Active Connection**: Persistent connection maintained
5. **Reconnection**: Automatic retry on disconnect
6. **User Logout**: Connection gracefully closed

---

## Getting Started

### Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **SQL Server** (LocalDB or full instance)
- **Visual Studio 2022** or **VS Code**
- **Angular CLI** - `npm install -g @angular/cli`

### Backend Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/JiraClone.git
   cd JiraClone
   ```

2. **Configure database connection**

   Edit `JiraClone.API/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=JiraCloneDb;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Configure JWT settings**

   Generate a secure secret key (32+ characters):
   ```json
   {
     "Jwt": {
       "Key": "YourSuperSecretKeyHere123456789012345678901234567890",
       "Issuer": "JiraCloneAPI",
       "Audience": "JiraCloneClient",
       "ExpireMinutes": 60
     }
   }
   ```

4. **Configure Groq API (for AI features)**

   Get API key from [Groq Console](https://console.groq.com):
   ```json
   {
     "GroqApi": {
       "ApiKey": "gsk_your_groq_api_key_here",
       "BaseUrl": "https://api.groq.com/openai/v1/chat/completions",
       "Model": "llama-3.3-70b-versatile"
     }
   }
   ```

5. **Apply database migrations**
   ```bash
   cd JiraClone.API
   dotnet ef database update
   ```

6. **Run the backend**
   ```bash
   dotnet run
   ```

   API will be available at `http://localhost:5000`
   Swagger UI at `http://localhost:5000/swagger`

### Frontend Setup

1. **Navigate to frontend directory**
   ```bash
   cd JiraCloneFront
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Configure API endpoint**

   Edit `src/environments/environment.ts`:
   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'http://localhost:5000/api/v1',
     signalRUrl: 'http://localhost:5000/hubs/notifications'
   };
   ```

4. **Run the frontend**
   ```bash
   ng serve
   ```

   Application will be available at `http://localhost:4200`

### First-Time Setup

1. **Register a new user** at `http://localhost:4200/register`
2. **Login** with your credentials
3. **Create your first project**
4. **Add tasks** using the UI or AI chatbot
5. **Organize tasks** on the Kanban board

---

## API Documentation

### Base URL
```
http://localhost:5000/api/v1
```

### Authentication Endpoints

#### Register New User
```http
POST /auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

#### Login
```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123!"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "abc123...",
  "expiration": "2024-12-31T23:59:59Z"
}
```

### Project Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/projects` | Get all projects | Yes |
| GET | `/projects/{id}` | Get project by ID | Yes |
| POST | `/projects` | Create new project | Yes |
| PUT | `/projects/{id}` | Update project | Yes |
| DELETE | `/projects/{id}` | Delete project | Yes |

### Task Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/tasks` | Get all tasks | Yes |
| GET | `/tasks/{id}` | Get task by ID | Yes |
| GET | `/tasks/by-project/{projectId}` | Get tasks by project | Yes |
| GET | `/tasks/by-sprint/{sprintId}` | Get tasks by sprint | Yes |
| POST | `/tasks` | Create new task | Yes |
| PUT | `/tasks/{id}` | Update task | Yes |
| PATCH | `/tasks/{id}/status` | Update task status | Yes |
| DELETE | `/tasks/{id}` | Delete task | Yes |

### Sprint Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/sprints` | Get all sprints | Yes |
| GET | `/sprints/{id}` | Get sprint by ID | Yes |
| GET | `/sprints/by-project/{projectId}` | Get sprints by project | Yes |
| POST | `/sprints` | Create new sprint | Yes |
| PUT | `/sprints/{id}` | Update sprint | Yes |
| POST | `/sprints/{id}/start` | Start sprint | Yes |
| POST | `/sprints/{id}/complete` | Complete sprint | Yes |
| DELETE | `/sprints/{id}` | Delete sprint | Yes |

### Comment Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/comments/by-task/{taskId}` | Get comments for task | Yes |
| POST | `/comments` | Create comment | Yes |
| PUT | `/comments/{id}` | Update comment | Yes |
| DELETE | `/comments/{id}` | Delete comment | Yes |

### Notification Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/notifications` | Get user notifications | Yes |
| POST | `/notifications/{id}/mark-as-read` | Mark as read | Yes |

### AI Chat Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/chat` | Send message to AI | Yes |
| POST | `/chat/upload` | Upload file for conversation | Yes |
| POST | `/chat/clear` | Clear conversation state | Yes |

**Request Example:**
```http
POST /chat
Content-Type: application/json
Authorization: Bearer {token}

{
  "message": "Create a high priority bug to fix the login page",
  "projectId": 1
}

Response:
{
  "message": "I've created a high priority bug task titled 'Fix login page issue'",
  "taskCreated": true,
  "taskId": 42
}
```

### Attachment Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/attachments/upload` | Upload file | Yes |
| GET | `/attachments/{id}/download` | Download file | Yes |
| DELETE | `/attachments/{id}` | Delete attachment | Yes |

### SignalR Hub

**Connection Endpoint:**
```
ws://localhost:5000/hubs/notifications?access_token={jwt_token}
```

**Events:**
- `ReceiveNotification` - Receive new notification
- `Connected` - Connection established
- `Disconnected` - Connection closed

---

## Project Structure

### Backend Directory Tree

```
JiraClone/
├── JiraClone.API/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ProjectsController.cs
│   │   ├── TasksController.cs
│   │   ├── SprintsController.cs
│   │   ├── CommentsController.cs
│   │   ├── NotificationsController.cs
│   │   ├── AttachmentsController.cs
│   │   ├── ChatController.cs
│   │   ├── UsersController.cs
│   │   └── TestXAIController.cs
│   ├── Hubs/
│   │   └── NotificationHub.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── JiraClone.API.csproj
├── JiraClone.Application/
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── ProjectService.cs
│   │   ├── TaskService.cs
│   │   ├── SprintService.cs
│   │   ├── CommentService.cs
│   │   ├── NotificationService.cs
│   │   ├── AttachmentService.cs
│   │   ├── ChatService.cs
│   │   ├── ConversationalChatService.cs
│   │   └── ConversationStateManager.cs
│   ├── Interfaces/
│   ├── DTOs/
│   └── JiraClone.Application.csproj
├── JiraClone.Infrastructure/
│   ├── Data/
│   │   ├── JiraCloneDbContext.cs
│   │   └── Migrations/ (13 migration files)
│   ├── Repositories/
│   │   ├── IRepository.cs
│   │   ├── Repository.cs
│   │   └── IUnitOfWork.cs
│   └── JiraClone.Infrastructure.csproj
├── JiraClone.Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Project.cs
│   │   ├── TaskItem.cs
│   │   ├── Sprint.cs
│   │   ├── Comment.cs
│   │   ├── Notification.cs
│   │   └── Attachment.cs
│   ├── Enums/
│   │   ├── TaskStatus.cs
│   │   ├── TaskPriority.cs
│   │   ├── TaskType.cs
│   │   └── SprintStatus.cs
│   └── JiraClone.Domain.csproj
└── JiraClone.sln
```

### Frontend Directory Tree

```
JiraCloneFront/
├── src/
│   ├── app/
│   │   ├── auth/
│   │   │   ├── login/
│   │   │   └── register/
│   │   ├── core/
│   │   │   ├── guards/
│   │   │   │   └── auth.guard.ts
│   │   │   ├── interceptors/
│   │   │   │   └── auth.interceptor.ts
│   │   │   └── services/
│   │   │       ├── auth.service.ts
│   │   │       ├── project.service.ts
│   │   │       ├── task.service.ts
│   │   │       ├── sprint.service.ts
│   │   │       ├── comment.service.ts
│   │   │       ├── notification.service.ts
│   │   │       ├── chatbot.service.ts
│   │   │       ├── attachment.service.ts
│   │   │       ├── signalr.service.ts
│   │   │       └── user.service.ts
│   │   ├── features/
│   │   │   ├── board/
│   │   │   │   └── kanban-board/
│   │   │   ├── projects/
│   │   │   │   ├── project-list/
│   │   │   │   └── project-form/
│   │   │   ├── tasks/
│   │   │   │   ├── task-list/
│   │   │   │   ├── task-detail/
│   │   │   │   └── task-form/
│   │   │   ├── sprints/
│   │   │   │   ├── sprint-list/
│   │   │   │   └── sprint-form/
│   │   │   ├── backlog/
│   │   │   └── dashboard/
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   │   ├── chatbot/
│   │   │   │   │   ├── chatbot.component.ts
│   │   │   │   │   ├── chatbot.component.html
│   │   │   │   │   └── chatbot.component.scss
│   │   │   │   ├── layout/
│   │   │   │   │   ├── layout.component.ts
│   │   │   │   │   ├── layout.component.html
│   │   │   │   │   └── layout.component.scss
│   │   │   │   ├── loading/
│   │   │   │   └── toast/
│   │   │   ├── models/
│   │   │   │   ├── user.model.ts
│   │   │   │   ├── project.model.ts
│   │   │   │   ├── task.model.ts
│   │   │   │   ├── sprint.model.ts
│   │   │   │   ├── comment.model.ts
│   │   │   │   ├── notification.model.ts
│   │   │   │   ├── attachment.model.ts
│   │   │   │   └── chatbot.model.ts
│   │   │   └── services/
│   │   └── app.component.ts
│   ├── environments/
│   │   ├── environment.ts
│   │   └── environment.prod.ts
│   ├── assets/
│   ├── index.html
│   └── main.ts
├── angular.json
├── package.json
├── tsconfig.json
└── README.md
```

---

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Commit your changes**
   ```bash
   git commit -m "Add your feature description"
   ```
4. **Push to the branch**
   ```bash
   git push origin feature/your-feature-name
   ```
5. **Open a Pull Request**

### Code Standards

- Follow C# coding conventions for backend
- Follow Angular style guide for frontend
- Write unit tests for new features
- Update documentation for API changes
- Ensure all tests pass before submitting PR

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

- **SignalR** for real-time communication
- **Groq API** for AI-powered features
- **Angular Material** for UI components
- **Entity Framework Core** for data access
- **JWT** for secure authentication

---

## Contact

For questions or support, please open an issue on GitHub.

---

## Roadmap

Future enhancements planned:

- [ ] Email notifications
- [ ] User avatars and profiles
- [ ] Advanced search and filtering
- [ ] Task templates
- [ ] Time tracking
- [ ] Reporting and analytics
- [ ] Mobile app (React Native)
- [ ] Dark mode theme
- [ ] Internationalization (i18n)
- [ ] API rate limiting
- [ ] Audit logging
- [ ] Export to CSV/PDF

---

**Built with passion for project management and collaboration**




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