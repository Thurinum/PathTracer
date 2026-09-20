using Microsoft.Extensions.Options;
using Prowl.Slang;

namespace PathTracerCore.Renderer.Shaders;

public readonly record struct CompiledShader(byte[] Code, (uint x, uint y, uint z) GroupSize);

public class SlangCompiler
{
    private readonly EngineOptions _options;
    private readonly Session _session;
    public const string EntryPointName = "main";

    public SlangCompiler(IOptions<EngineOptions> options)
    {
        _options = options.Value;
        
        TargetDescription target = new()
        {
            Format = CompileTarget.Spirv,
            Profile = GlobalSession.FindProfile("spirv_1_5")
        };

        string shadersDirectory = Path.Combine(AppContext.BaseDirectory, _options.ShadersDir);
        SessionDescription sessionDescription = new()
        {
            Targets = [target],
            SearchPaths = [shadersDirectory, "./"]
        };

        _session = GlobalSession.CreateSession(sessionDescription);
    }
    
    public CompiledShader CompileComputeShader(string moduleName)
    {
        string fileName = moduleName + ".slang";
        string path = Path.Combine(AppContext.BaseDirectory, _options.ShadersDir, fileName);
        string source = File.ReadAllText(path);
        Module module = _session.LoadModuleFromSourceString(
            moduleName,
            path,
            source,
            out DiagnosticInfo loadDiagnostics);

        ThrowIfErrors(loadDiagnostics);

        EntryPoint entryPoint = module.FindAndCheckEntryPoint(
            EntryPointName,
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

        ShaderReflection layout = program.GetLayout();
        EntryPointReflection entry = layout.FindEntryPointByName(EntryPointName);
        var g = entry.GetComputeThreadGroupSize();

        return new CompiledShader(code.ToArray(), g);
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
