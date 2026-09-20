using System.Numerics;
using ImGuiNET;
using PathTracerCore.SceneGraph;

namespace PathTracerApp.Components;

public class OrbitCameraComponent : Component
{
    private const float Deg2Rad = MathF.PI / 180f;
    private const float MaxElevation = 89.5f;

    private Transform _transform = null!;

    public Vector3 Target { get; set; } = Vector3.Zero;
    public float Distance { get; set; } = 5f;
    public float ElevationDegrees { get; set; } = 20f;
    public float AzimuthDegrees { get; set; } = 0f;
    public float OrbitSpeedDegreesPerSecond { get; set; } = 0f;

    public override void Awake()
    {
        _transform = Parent.GetComponent<Transform>();
    }

    public override void Update(float deltaTime)
    {
        AzimuthDegrees = MathF.IEEERemainder(
            AzimuthDegrees + OrbitSpeedDegreesPerSecond * deltaTime, 360f);

        float elevation = Math.Clamp(ElevationDegrees, -MaxElevation, MaxElevation) * Deg2Rad;
        float azimuth = AzimuthDegrees * Deg2Rad;

        // direction from the target to the camera
        Vector3 offset = new(
            MathF.Cos(elevation) * MathF.Sin(azimuth),
            MathF.Sin(elevation),
            MathF.Cos(elevation) * MathF.Cos(azimuth));

        Vector3 position = Target + offset * MathF.Max(Distance, 1e-3f);

        _transform.Position = position;
        _transform.Rotation = LookRotation(position, Target);
    }

    public override void OnGUI()
    {
        ImGui.Begin("Orbit Camera");

        Vector3 target = Target;
        if (ImGui.DragFloat3("Target", ref target, 0.05f))
            Target = target;

        float distance = Distance;
        if (ImGui.SliderFloat("Distance", ref distance, 0.1f, 50f))
            Distance = distance;

        float elevation = ElevationDegrees;
        if (ImGui.SliderFloat("Elevation", ref elevation, -MaxElevation, MaxElevation))
            ElevationDegrees = elevation;

        float azimuth = AzimuthDegrees;
        if (ImGui.SliderFloat("Azimuth", ref azimuth, -180f, 180f))
            AzimuthDegrees = azimuth;

        float speed = OrbitSpeedDegreesPerSecond;
        if (ImGui.DragFloat("Orbit Speed", ref speed, 0.5f, -180f, 180f))
            OrbitSpeedDegreesPerSecond = speed;

        ImGui.End();
    }

    private static Quaternion LookRotation(Vector3 position, Vector3 target)
    {
        Matrix4x4 view = Matrix4x4.CreateLookAt(position, target, Vector3.UnitY);
        return Quaternion.CreateFromRotationMatrix(Matrix4x4.Transpose(view));
    }
}
