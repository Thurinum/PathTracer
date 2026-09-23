module PathTracer;

import Primitives;
import BlinnPhong;
import Camera;

struct Params
{
    uint PlaneCount;
    uint SphereCount;
}

RWTexture2D<float4> outputImage : register(u0);
Texture2D<float4> circles : register(t1);
SamplerState circlesSampler : register(s2);
ConstantBuffer<Camera> camera : register(b3);
StructuredBuffer<Plane> planes : register(t4);
StructuredBuffer<Sphere> spheres : register(t5);
ConstantBuffer<Params> params : register(b6);

static const float3 SunDir = float3(0.5081, 0.5679, 0.6476);

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    outputImage.GetDimensions(frameSize.x, frameSize.y);
    
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;
        
    Ray ray = BuildPrimaryRay(camera, pixel, frameSize);
     
    float bestT = 1e30;
    HitResult bestHit = HitResult.Miss();
    
    for (uint i = 0; i < params.PlaneCount; ++i)
    {
        HitResult h = IntersectPlane(ray.Origin, ray.Direction, planes[i], 1e-4, bestT);
        if (h.IsHit()) { bestT = h.T; bestHit = h; }
    }
    for (uint i = 0; i < params.SphereCount; ++i)
    {
        HitResult h = IntersectSphere(ray.Origin, ray.Direction, spheres[i].Center, spheres[i].Radius, 1e-4, bestT);
        if (h.IsHit()) { bestT = h.T; bestHit = h; }
    }

    float3 color = float3(0.0);
    if (bestHit.IsHit())
    {
        float3 lightCol = float3(0.56f, 0.7f, 0.1f);

        float3 Ng = bestHit.Normal; 
        float3 Nv = dot(Ng, ray.Direction) > 0.0 ? -Ng : Ng; 
        float3 L  = SunDir;
        float3 V  = -ray.Direction;

        float3 albedo = circles.SampleLevel(circlesSampler, bestHit.UV, 0.0).xyz;
        color = albedo * BlinnPhong(Ng, Nv, L, V, 0.8, 64.0, 0.2);
    }
    else 
    {
        color = 0;
    }

    outputImage[pixel] = float4(color, 1.0);
}
