struct Params
{
    float3 Center;
    float Radius;
    float3 Color;
}

RWTexture2D<float4> outputImage : register(u0);
ConstantBuffer<Params> params : register(b1);

float intersectSphere(float3 origin, float3 dir, float3 center, float radius, float tMin, float tMax) {
    float3 q = origin - center;
    float a = dot(dir,dir);
    float b = 2 * dot(q, dir);
    float c = dot(q,q) - (radius * radius);
    float discriminant = b * b - (4 * a * c);
    
    if (discriminant < 0.0) {
        return -1.0;
    }
    
    float sqrtDiscriminant = sqrt(discriminant);
    
    float t1 = (-b - sqrtDiscriminant) / (2.0 * a);
    if (t1 > tMin && t1 < tMax) {
        return t1;
    }
    
    float t2 = (-b + sqrtDiscriminant) / (2.0 * a);
    if (t2 > tMin && t2 < tMax) {
        return t2;
    }
    
    
   return -1.0; 
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint2 frameSize;
    outputImage.GetDimensions(frameSize.x, frameSize.y);
    
    uint2 pixel = dispatchThreadId.xy;
    if (any(pixel >= frameSize))
        return;
        
    float aspect = float(frameSize.x) / float(frameSize.y);
    float2 uv = (float2(pixel) + 0.5) / float2(frameSize);
    float3 origin = float3(0.0);
    float h = 2.0 * tan(radians(90.0) * 0.5);
    float3 dir = normalize(float3((uv.x - 0.5) * h * aspect, (0.5 - uv.y) * h, -1.0));
    
    float3 color = float3(0.0);
    
    float t = intersectSphere(origin, dir, params.Center, params.Radius, 0.0, 1e30); // on est rendu ou sur le rayon
    if (params.Radius > 0.0 && t > 0.0)
    {
        float3 p = origin + t * dir; // obtenir le point sur la surface de la sphere
        float3 n = normalize(p - params.Center); // la normale est perpendiculaire a la surface
        
        if (dot(n, dir) > 0.0)
            n = -n; // gere le cas ou la camera est derriere la surface (render backfaces)
        
        color = n;
        //color = params.Color;
    }   
        
    outputImage[pixel] = float4(color, 1.0);
}