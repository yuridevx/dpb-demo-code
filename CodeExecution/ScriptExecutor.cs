#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DreamPoeBot.Loki.Game;
using GoBo.Infrastructure.Input;
using GoBo.Infrastructure.Modules;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GoBo.Infrastructure.CodeExecution;

[Module]
public class ScriptExecutor(IScope scope) : IScriptExecutor
{
    private static MetadataReference[] BuildReferences(AssemblyLoadContext? baseAlc, AssemblyLoadContext? currentAlc)
    {
        var refs = new List<MetadataReference>();
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Helper to add assembly reference if valid
        void AddAssemblyReference(Assembly assembly)
        {
            if (assembly.IsDynamic)
                return;
                
            if (string.IsNullOrEmpty(assembly.Location))
                return;
                
            if (!File.Exists(assembly.Location))
                return;
                
            if (addedPaths.Contains(assembly.Location))
                return;
                
            try
            {
                refs.Add(MetadataReference.CreateFromFile(assembly.Location));
                addedPaths.Add(assembly.Location);
            }
            catch
            {
                // Skip assemblies that can't be referenced
            }
        }
        
        // Add all assemblies from base ALC (AssemblyLoadContext.Default)
        if (baseAlc != null)
        {
            foreach (var assembly in baseAlc.Assemblies)
            {
                AddAssemblyReference(assembly);
            }
        }
        
        // Add all assemblies from current ALC (host context)
        if (currentAlc != null && currentAlc != baseAlc)
        {
            foreach (var assembly in currentAlc.Assemblies)
            {
                AddAssemblyReference(assembly);
            }
        }
        
        return refs.ToArray();
    }

    public async Task<T> EvalAsync<T>(string code)
    {
        ScriptContext.Scope = scope;
        return await WithInputSession(() => RunScriptAsync<T>(code));
    }

    public async Task EvalAsync(string code)
    {
        ScriptContext.Scope = scope;
        await WithInputSession(() => RunScriptAsync<object>(code));
    }

    public async Task<object> ExecuteAsync(string code, CancellationToken ct)
    {
        ScriptContext.Scope = scope;
        return await WithInputSession(() => RunScriptAsync<object>(code, ct));
    }

    private async Task<T> RunScriptAsync<T>(string code, CancellationToken ct = default)
    {
        var hostContext = AssemblyLoadContext.GetLoadContext(typeof(ScriptExecutor).Assembly);
        var baseAlc = AssemblyLoadContext.Default;
        var scriptAlc = new ScriptAssemblyLoadContext(hostContext);

        try
        {
            // Build references from all assemblies in base and current ALCs
            var references = BuildReferences(baseAlc, hostContext);
            
            var compilation = CSharpCompilation.Create(
                $"Script_{Guid.NewGuid():N}",
                new[] { CSharpSyntaxTree.ParseText(code, cancellationToken: ct) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            if (!compilation.Emit(ms, cancellationToken: ct).Success)
            {
                var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                throw new CompilationErrorException(
                    $"Compilation failed:\n{string.Join("\n", errors.Select(e => e.GetMessage()))}",
                    errors
                );
            }

            ms.Position = 0;
            var assembly = scriptAlc.LoadFromStream(ms);
            
            // Find the first public class in the assembly
            var scriptType = assembly.GetTypes()
                .FirstOrDefault(t => t.IsClass && t.IsPublic && !t.IsAbstract && !t.IsGenericTypeDefinition)
                ?? throw new InvalidOperationException("No public class found in compiled code");

            // Find Execute method
            var executeMethod = scriptType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, 
                null, new[] { typeof(CancellationToken) }, null)
                ?? throw new InvalidOperationException("Could not find Execute method. Expected: public async Task<object> Execute(CancellationToken ct) or public static async Task<object> Execute(CancellationToken ct)");

            // Create instance only if method is not static
            object? instance = null;
            if (!executeMethod.IsStatic)
            {
                instance = scope.New(scriptType);
            }

            var result = await (Task<object>)executeMethod.Invoke(instance, new object[] { ct })!;
            
            return result == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null
                ? throw new InvalidOperationException($"Script returned null but expected non-nullable type {typeof(T).Name}")
                : (T)(result ?? default(T)!);
        }
        finally
        {
            scriptAlc.Unload();
        }
    }


    private static async Task<T> WithInputSession<T>(Func<Task<T>> action)
    {
        // TryEnable returns null if suspended or if hook infrastructure unavailable (tests)
        IInputSession? session = null;
        try
        {
            session = InputSession.TryEnable("ScriptExecution");
        }
        catch
        {
            // Hook infrastructure not available (test environment)
        }

        try
        {
            return await action();
        }
        finally
        {
            session?.Dispose();
        }
    }
}