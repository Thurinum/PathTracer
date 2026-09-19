using Microsoft.Extensions.Options;
using Prowl.Slang;

namespace PathTracerCore.Renderer.Shaders;

public class SlangCompiler
{
    private readonly EngineOptions _options;
    private readonly Session _session;

    public SlangCompiler(IOptions<EngineOptions> options)
    {
        _options = options.Value;
        
        TargetDescription target = new()
        {
            Format = CompileTarget.Spirv,
            Profile = GlobalSession.FindProfile("spirv_1_5")
        };

        SessionDescription sessionDescription = new()
        {
            Targets = [target],
            SearchPaths = ["./"]
        };

        _session = GlobalSession.CreateSession(sessionDescription);
    }
    
    public byte[] CompileComputeShader(string moduleName)
    {
        string fileName = moduleName + ".slang";
        string path = Path.Combine(AppContext.BaseDirectory, _options.ShadersDir, fileName);
        string source = File.ReadAllText(path);
        Module module = _session.LoadModuleFromSourceString(
            moduleName,
            fileName,
            source,
            out DiagnosticInfo loadDiagnostics);

        ThrowIfErrors(loadDiagnostics);

        EntryPoint entryPoint = module.FindAndCheckEntryPoint(
            "main",
            ShaderStage.Compute,
            out var entryPointDiagnostics);

        ThrowIfErrors(entryPointDiagnostics);

        ComponentType program = _session.CreateCompositeComponentType([module, entryPoint], out var componentDiagnostics);

        ThrowIfErrors(componentDiagnostics);

        Memory<byte> code = program.GetEntryPointCode(
            IntPtr.Zero,
            IntPtr.Zero,
            out DiagnosticInfo codeDiagnostics);

        ThrowIfErrors(codeDiagnostics);

        return code.ToArray();
    }

    private static void ThrowIfErrors(DiagnosticInfo diagnostics)
    {
        List<string> errors = diagnostics.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity >= Severity.Error)
            .Select(diagnostic => diagnostic.Message)
            .ToList();

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Slang compilation failed: " + string.Join(Environment.NewLine, errors));
        }
    }
}
