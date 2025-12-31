#nullable enable

using Microsoft.CodeAnalysis;

namespace GoBo.Infrastructure.CodeExecution;

/// <summary>
/// Exception thrown when script compilation fails.
/// Compatible with Microsoft.CodeAnalysis.Scripting.CompilationErrorException API.
/// </summary>
public class CompilationErrorException : Exception
{
    public IEnumerable<Diagnostic> Diagnostics { get; }

    public CompilationErrorException(string message, IEnumerable<Diagnostic> diagnostics)
        : base(message)
    {
        Diagnostics = diagnostics;
    }
}

