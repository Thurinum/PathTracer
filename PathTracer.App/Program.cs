using NeoVeldrid;
using NeoVeldrid.Sdl2;
using NeoVeldrid.SPIRV;
using NeoVeldrid.StartupUtilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


const int WindowWidth = 1024;
const int WindowHeight = 768;

WindowCreateInfo windowDesc = new()
{
    X = 100,
    Y = 100,
    WindowWidth =  WindowWidth,
    WindowHeight = WindowHeight,
    WindowTitle = "Bruh"
};

NeoVeldridStartup.CreateWindowAndGraphicsDevice(
    windowDesc, 
    new GraphicsDeviceOptions
    {
        PreferStandardClipSpaceYDirection = true,
        PreferDepthRangeZeroToOne = true,
    },
    out Sdl2Window window,
    out GraphicsDevice device);

TextureDescription texDesc = TextureDescription.Texture2D(
    WindowWidth,
    WindowHeight,
    1,
    1,
    PixelFormat.R8_G8_B8_A8_UNorm,
    TextureUsage.Storage);
Texture outputTexture = device.ResourceFactory.CreateTexture(texDesc);

byte[] shaderBytes = File.ReadAllBytes("Resources/image.compute");
ShaderDescription shaderDesc = new ShaderDescription(
    ShaderStages.Compute,
    shaderBytes,
    "main"
);

Shader computeShader = device.ResourceFactory.CreateFromSpirv(shaderDesc);

ResourceLayout layout =
    device.ResourceFactory.CreateResourceLayout(
        new ResourceLayoutDescription(
            new ResourceLayoutElementDescription(
                "outputImage",
                ResourceKind.TextureReadWrite,
                ShaderStages.Compute
            )
        )
    );


ResourceSet set =
    device.ResourceFactory.CreateResourceSet(
        new ResourceSetDescription(
            layout,
            outputTexture
        )
    );


Pipeline pipeline =
    device.ResourceFactory.CreateComputePipeline(
        new ComputePipelineDescription(
            computeShader,
            layout,
            8,
            8,
            1
        )
    );


CommandList cl =
    device.ResourceFactory.CreateCommandList();


cl.Begin();

cl.SetPipeline(pipeline);
cl.SetComputeResourceSet(0, set);

cl.Dispatch(
    WindowWidth / 8,
    WindowHeight / 8,
    1
);

cl.End();


device.SubmitCommands(cl);
device.WaitForIdle();


//
// Copy GPU texture to CPU staging texture
//

Texture staging =
    device.ResourceFactory.CreateTexture(
        TextureDescription.Texture2D(
            WindowWidth,
            WindowHeight,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Staging
        )
    );


cl.Begin();

cl.CopyTexture(
    outputTexture,
    staging
);

cl.End();

device.SubmitCommands(cl);
device.WaitForIdle();


while (window.Exists)
{
    window.PumpEvents();
}

device.Dispose();