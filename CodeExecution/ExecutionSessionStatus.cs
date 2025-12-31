namespace GoBo.Infrastructure.CodeExecution;

public enum ExecutionSessionStatus
{
    Created,
    Running,
    Completed,
    Faulted,
    Cancelled
}