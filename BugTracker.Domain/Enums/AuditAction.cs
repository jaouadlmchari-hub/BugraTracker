namespace BugTracker.Domain.Enums;

public enum AuditAction
{
    // =========================
    // Authentication (1 - 9)
    // =========================

    LoginSucceeded = 1,
    LoginFailed = 2,
    Logout = 3,
    RefreshTokenUsed = 4,
    RefreshTokenRevoked = 5,


    // =========================
    // Users (10 - 19)
    // =========================

    UserCreated = 10,
    UserUpdated = 11,
    UserDeactivated = 12,
    UserActivated = 13,
    UserRoleChanged = 14,
    PasswordChanged = 15,
    PasswordReset = 16,
    UserUnlocked = 17,


    // =========================
    // Projects (20 - 29)
    // =========================

    ProjectCreated = 20,
    ProjectUpdated = 21,
    ProjectArchived = 22,
    ProjectActivated = 23,
    ProjectDeleted = 24,
    ProjectOwnerChanged = 25,


    // =========================
    // Project Members (30 - 39)
    // =========================

    ProjectMemberAdded = 30,
    ProjectMemberRoleChanged = 31,
    ProjectMemberRemoved = 32,


    // =========================
    // Epics (40 - 49)
    // =========================

    EpicCreated = 40,
    EpicUpdated = 41,
    EpicDeleted = 42,
    EpicStatusChanged = 43,


    // =========================
    // Sprints (50 - 59)
    // =========================

    SprintCreated = 50,
    SprintUpdated = 51,
    SprintStarted = 52,
    SprintCompleted = 53,
    SprintDeleted = 54,


    // =========================
    // Issues (60 - 79)
    // =========================

    IssueCreated = 60,
    IssueUpdated = 61,
    IssueDeleted = 62,

    IssueStatusChanged = 63,
    IssuePriorityChanged = 64,
    IssueTypeChanged = 65,
    IssueStoryPointsChanged = 66,

    IssueAssigned = 67,
    IssueUnassigned = 68,

    IssueEpicChanged = 69,
    IssueSprintChanged = 70,

    IssueDueDateChanged = 71,


    // =========================
    // Comments (80 - 89)
    // =========================

    CommentCreated = 80,
    CommentUpdated = 81,
    CommentDeleted = 82,


    // =========================
    // Attachments (90 - 99)
    // =========================

    AttachmentUploaded = 90,
    AttachmentDeleted = 91
}