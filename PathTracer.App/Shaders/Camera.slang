module Camera;

public struct Camera
{
    public float3 Origin;    
    public float Aspect;
    public float3 Up;        
    public float TanHalfFov;
    public float3 Right;     
    float Pad0;
    public float3 Forward;   
    float Pad1;
};
       
public struct Ray
{
    public float3 Origin;
    public float3 Direction;
}

public Ray BuildPrimaryRay(Camera camera, uint2 pixel, uint2 frameSize)
{
    float2 screenUV  = (float2(pixel) + 0.5) / float2(frameSize);
    float viewPlaneX = (2.0 * screenUV.x - 1.0) * camera.Aspect * camera.TanHalfFov;
    float viewPlaneY = (1.0 - 2.0 * screenUV.y) * camera.TanHalfFov;
    
    Ray ray;
    ray.Origin = camera.Origin;
    ray.Direction = normalize(camera.Forward + viewPlaneX * camera.Right + viewPlaneY * camera.Up);
    return ray;
}