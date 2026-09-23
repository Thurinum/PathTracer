struct Params
{
    float4 Sphere0;
    float4 Sphere1;
    float4 Sphere2;
    float4 Sphere3;
    float4 Albedo0;
    float4 Albedo1;
    float4 Albedo2;
    float4 Albedo3;
    float4 Properties0;
    float4 Properties1;
    float4 Properties2;
    float4 Properties3;
    uint SphereCount;
    uint SampleIndex;
    float Time;
    float PreviousTime;
}

RWTexture2D<float4> outputImage : register(u0);
Texture2D<float4> env : register(t1);
ConstantBuffer<Params> params : register(b2);
RWTexture2D<float4> guideImage : register(u3);
RWTexture2D<float4> motionImage : register(u4);

static const float Pi = 3.14159265;
static const float RayEpsilon = 0.001;
static const float GroundHeight = -0.5;
static const float LightHeight = 1.8;
static const float LightWidth = 1.2;
static const float LightDepth = 1.0;
static const float LightArea = LightWidth * LightDepth;
static const float3 LightEmission = float3(14.0, 12.0, 9.0);
static const uint MaxBounces = 6;

float intersectSphere(float3 origin, float3 direction, float3 center, float radius, float tMax)
{
    float3 offset = origin - center;
    float halfB = dot(offset, direction);
    float c = dot(offset, offset) - radius * radius;
    float discriminant = halfB * halfB - dot(direction, direction) * c;
    if (discriminant < 0.0)
        return -1.0;

    float root = sqrt(discriminant);
    float t = (-halfB - root) / dot(direction, direction);
    if (t > RayEpsilon && t < tMax)
        return t;
    t = (-halfB + root) / dot(direction, direction);
    return t > RayEpsilon && t < tMax ? t : -1.0;
}

uint hash(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352d;
    value ^= value >> 15;
    value *= 0x846ca68b;
    value ^= value >> 16;
    return value;
}

float random(inout uint state)
{
    state = hash(state + 0x9e3779b9);
    return float(state) * (1.0 / 4294967296.0);
}

float3 sampleEnvironment(float3 direction)
{
    uint width, height;
    env.GetDimensions(width, height);

    float u = frac(0.5 + atan2(direction.z, direction.x) * (0.5 / Pi));
    float v = 0.5 - asin(clamp(direction.y, -1.0, 1.0)) * (1.0 / Pi);
    float2 texel = float2(u * width - 0.5, v * height - 0.5);
    int2 baseTexel = int2(floor(texel));
    float2 blend = frac(texel);
    int imageWidth = int(width);
    int imageHeight = int(height);
    int x0 = (baseTexel.x % imageWidth + imageWidth) % imageWidth;
    int x1 = (x0 + 1) % imageWidth;
    int y0 = clamp(baseTexel.y, 0, imageHeight - 1);
    int y1 = min(y0 + 1, imageHeight - 1);

    float3 upper = lerp(env.Load(int3(x0, y0, 0)).rgb, env.Load(int3(x1, y0, 0)).rgb, blend.x);
    float3 lower = lerp(env.Load(int3(x0, y1, 0)).rgb, env.Load(int3(x1, y1, 0)).rgb, blend.x);
    return lerp(upper, lower, blend.y);
}

float powerHeuristic(float a, float b)
{
    float a2 = a * a;
    return a2 / max(a2 + b * b, 1e-8);
}

float specularExponent(float roughness)
{
    float alpha = max(roughness * roughness, 1e-4);
    return max(0.0, 2.0 / alpha - 2.0);
}

float specularPdf(float3 direction, float3 reflectionAxis, float roughness)
{
    float exponent = specularExponent(roughness);
    float cosine = max(dot(direction, reflectionAxis), 0.0);
    return (exponent + 1.0) * pow(cosine, exponent) / (2.0 * Pi);
}

float evaluateBsdfPdf(float3 direction, float3 normal, float3 reflectionAxis, float metallic, float roughness)
{
    float diffusePdf = max(dot(normal, direction), 0.0) / Pi;
    float glossyPdf = specularPdf(direction, reflectionAxis, roughness);
    return (1.0 - metallic) * diffusePdf + metallic * glossyPdf;
}

float3 sampleSpecularLobe(float3 axis, float roughness, inout uint rng)
{
    float exponent = specularExponent(roughness);
    float cosine = pow(max(random(rng), 1e-7), 1.0 / (exponent + 1.0));
    float sine = sqrt(max(0.0, 1.0 - cosine * cosine));
    float phi = 2.0 * Pi * random(rng);
    float3 helper = abs(axis.y) < 0.999 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0);
    float3 tangent = normalize(cross(helper, axis));
    float3 bitangent = cross(axis, tangent);
    return normalize(axis * cosine + tangent * (sine * cos(phi)) + bitangent * (sine * sin(phi)));
}

void getSphere(uint index, float time, out float4 sphere, out float4 material, out float4 properties)
{
    sphere = float4(0.0);
    material = float4(0.0);
    properties = float4(0.5, 0.0, 0.0, 0.0);
    if (index == 0) { sphere = params.Sphere0; material = params.Albedo0; properties = params.Properties0; }
    if (index == 1) { sphere = params.Sphere1; material = params.Albedo1; properties = params.Properties1; }
    if (index == 2) { sphere = params.Sphere2; material = params.Albedo2; properties = params.Properties2; }
    if (index == 3) { sphere = params.Sphere3; material = params.Albedo3; properties = params.Properties3; }

    float phase = time;
    if (index == 0) sphere.xyz += float3(0.30 * sin(phase * 0.8), 0.16 * (1.0 + sin(phase * 1.3)), 0.0);
    if (index == 1) sphere.xyz += float3(0.12 * sin(phase * 0.45), 0.12 * sin(phase * 1.1), 0.0);
    if (index == 2) sphere.xyz += float3(0.32 * sin(phase * 0.65 + 1.4), 0.14 * (1.0 + sin(phase * 1.2 + 0.7)), 0.0);
    if (index == 3) sphere.xyz += float3(0.28 * sin(phase * 0.9 + 3.0), 0.12 * (1.0 + sin(phase * 1.5 + 2.0)), 0.0);
}

float3 lightCenter(float time)
{
    return float3(0.48 * sin(time * 0.65), LightHeight, -2.7 + 0.3 * cos(time * 0.65));
}

bool intersectScene(
    float3 origin,
    float3 direction,
    out float hitDistance,
    out float3 normal,
    out float3 albedo,
    out float metallic,
    out float roughness,
    out float3 emission,
    out uint objectId)
{
    hitDistance = 1e30;
    normal = float3(0.0);
    albedo = float3(0.0);
    metallic = 0.0;
    roughness = 0.8;
    emission = float3(0.0);
    objectId = 9;
    bool hit = false;

    for (uint i = 0; i < params.SphereCount; i++)
    {
        float4 sphere;
        float4 material;
        float4 properties;
        getSphere(i, params.Time, sphere, material, properties);
        float t = intersectSphere(origin, direction, sphere.xyz, sphere.w, hitDistance);
        if (t > 0.0)
        {
            hitDistance = t;
            normal = normalize(origin + direction * t - sphere.xyz);
            albedo = material.rgb;
            metallic = saturate(material.w);
            roughness = clamp(properties.x, 0.02, 1.0);
            objectId = i;
            hit = true;
        }
    }

    // Checker-tiled studio floor.
    if (abs(direction.y) > 1e-6)
    {
        float t = (GroundHeight - origin.y) / direction.y;
        float3 point = origin + direction * t;
        if (t > RayEpsilon && t < hitDistance && abs(point.x) < 2.5 && point.z > -5.0 && point.z < 0.5)
        {
            hitDistance = t;
            normal = float3(0.0, 1.0, 0.0);
            int tile = int(floor(point.x * 2.0)) + int(floor(point.z * 2.0));
            albedo = (tile & 1) == 0 ? float3(0.55, 0.57, 0.60) : float3(0.16, 0.18, 0.22);
            objectId = (tile & 1) == 0 ? 4 : 10;
            hit = true;
        }

        // Rectangular ceiling emitter; its underside emits into the room.
        float lightT = (LightHeight - origin.y) / direction.y;
        float3 lightPoint = origin + direction * lightT;
        float3 center = lightCenter(params.Time);
        if (direction.y > 0.0 && lightT > RayEpsilon && lightT < hitDistance &&
            abs(lightPoint.x - center.x) < LightWidth * 0.5 && abs(lightPoint.z - center.z) < LightDepth * 0.5)
        {
            hitDistance = lightT;
            normal = float3(0.0, -1.0, 0.0);
            emission = LightEmission;
            objectId = 8;
            roughness = 1.0;
            hit = true;
        }
    }

    // Colored side walls and a blue-gray back wall make indirect color bleed visible.
    if (abs(direction.z) > 1e-6)
    {
        float t = (-5.0 - origin.z) / direction.z;
        float3 point = origin + direction * t;
        if (t > RayEpsilon && t < hitDistance && abs(point.x) < 2.5 && point.y > GroundHeight && point.y < LightHeight)
        {
            hitDistance = t;
            normal = float3(0.0, 0.0, 1.0);
            albedo = float3(0.28, 0.34, 0.48);
            objectId = 5;
            hit = true;
        }
    }

    if (abs(direction.x) > 1e-6)
    {
        float leftT = (-2.5 - origin.x) / direction.x;
        float3 leftPoint = origin + direction * leftT;
        if (leftT > RayEpsilon && leftT < hitDistance && leftPoint.y > GroundHeight && leftPoint.y < LightHeight && leftPoint.z > -5.0 && leftPoint.z < 0.2)
        {
            hitDistance = leftT;
            normal = float3(1.0, 0.0, 0.0);
            albedo = float3(0.55, 0.12, 0.10);
            objectId = 6;
            hit = true;
        }

        float rightT = (2.5 - origin.x) / direction.x;
        float3 rightPoint = origin + direction * rightT;
        if (rightT > RayEpsilon && rightT < hitDistance && rightPoint.y > GroundHeight && rightPoint.y < LightHeight && rightPoint.z > -5.0 && rightPoint.z < 0.2)
        {
            hitDistance = rightT;
            normal = float3(-1.0, 0.0, 0.0);
            albedo = float3(0.12, 0.42, 0.20);
            objectId = 7;
            hit = true;
        }
    }

    if (hit && dot(normal, direction) > 0.0)
        normal = -normal;
    return hit;
}

float3 sampleCosineHemisphere(float3 normal, inout uint rng)
{
    float r = sqrt(random(rng));
    float phi = 2.0 * Pi * random(rng);
    float3 local = float3(r * cos(phi), r * sin(phi), sqrt(max(0.0, 1.0 - r * r)));
    float3 helper = abs(normal.y) < 0.999 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0);
    float3 tangent = normalize(cross(helper, normal));
    float3 bitangent = cross(normal, tangent);
    return normalize(tangent * local.x + bitangent * local.y + normal * local.z);
}

float3 estimateDirectLight(float3 point, float3 incoming, float3 normal, float3 albedo,
    float metallic, float roughness, inout uint rng)
{
    const uint CandidateCount = 2;
    if (metallic >= 0.999 && roughness < 0.02)
        return float3(0.0);

    float weightSum = 0.0;
    float selectedWeight = 0.0;
    float3 selectedContribution = float3(0.0);
    float3 selectedDirection = float3(0.0);
    float selectedDistance = 0.0;
    float3 center = lightCenter(params.Time);

    // Weighted reservoir resampling focuses the single shadow ray on a useful
    // area-light sample instead of spending one shadow ray on every candidate.
    for (uint candidate = 0; candidate < CandidateCount; candidate++)
    {
        float3 lightPoint = center + float3(
            (random(rng) - 0.5) * LightWidth,
            0.0,
            (random(rng) - 0.5) * LightDepth);
        float3 toLight = lightPoint - point;
        float distanceSquared = dot(toLight, toLight);
        float distanceToLight = sqrt(distanceSquared);
        float3 lightDirection = toLight / distanceToLight;
        float surfaceCosine = max(dot(normal, lightDirection), 0.0);
        float lightCosine = max(lightDirection.y, 0.0);
        if (surfaceCosine <= 0.0 || lightCosine <= 0.0)
            continue;

        float lightPdf = distanceSquared / (LightArea * lightCosine);
        float3 reflectionAxis = reflect(incoming, normal);
        float mixturePdf = evaluateBsdfPdf(lightDirection, normal, reflectionAxis, metallic, roughness);
        float misWeight = powerHeuristic(lightPdf, mixturePdf);
        float3 contribution = albedo * (mixturePdf * LightEmission * (misWeight / lightPdf));
        float target = max(dot(contribution, float3(0.2126, 0.7152, 0.0722)), 1e-6);

        weightSum += target;
        if (random(rng) * weightSum < target)
        {
            selectedWeight = target;
            selectedContribution = contribution;
            selectedDirection = lightDirection;
            selectedDistance = distanceToLight;
        }
    }

    if (selectedWeight <= 0.0)
        return float3(0.0);

    float3 shadowOrigin = point + normal * RayEpsilon;
    float shadowDistance;
    float3 shadowNormal, shadowAlbedo, shadowEmission;
    float shadowMetallic, shadowRoughness;
    uint shadowObjectId;
    bool blocked = intersectScene(shadowOrigin, selectedDirection, shadowDistance, shadowNormal, shadowAlbedo,
        shadowMetallic, shadowRoughness, shadowEmission, shadowObjectId)
        && shadowDistance < selectedDistance - RayEpsilon * 2.0;
    if (blocked)
        return float3(0.0);

    return selectedContribution * (weightSum / (CandidateCount * selectedWeight));
}

float3 tracePath(float3 origin, float3 direction, inout uint rng)
{
    float3 radiance = float3(0.0);
    float3 throughput = float3(1.0);
    float previousBsdfPdf = 0.0;
    bool previousWasNonDelta = false;

    for (uint bounce = 0; bounce < MaxBounces; bounce++)
    {
        float hitDistance;
        float3 normal, albedo, emission;
        float metallic, roughness;
        uint objectId;
        if (!intersectScene(origin, direction, hitDistance, normal, albedo, metallic, roughness, emission, objectId))
        {
            radiance += throughput * sampleEnvironment(direction);
            break;
        }

        float3 hitPoint = origin + direction * hitDistance;
        if (any(emission > 0.0))
        {
            float misWeight = 1.0;
        if (previousWasNonDelta && direction.y > 0.0)
            {
                float lightPdf = hitDistance * hitDistance / (LightArea * direction.y);
                misWeight = powerHeuristic(previousBsdfPdf, lightPdf);
            }
            radiance += throughput * emission * misWeight;
            break;
        }

        radiance += throughput * estimateDirectLight(hitPoint, direction, normal, albedo, metallic, roughness, rng);

        origin = hitPoint + normal * RayEpsilon;
        float3 incidentDirection = direction;
        bool specularBranch = random(rng) < metallic;
        if (!specularBranch)
        {
            direction = sampleCosineHemisphere(normal, rng);
            previousWasNonDelta = true;
        }
        else
        {
            float3 reflectionAxis = reflect(incidentDirection, normal);
            if (roughness < 0.02)
            {
                direction = reflectionAxis;
                previousWasNonDelta = false;
            }
            else
            {
                direction = sampleSpecularLobe(reflectionAxis, roughness, rng);
                previousWasNonDelta = true;
                if (dot(direction, normal) <= 0.0)
                    break;
            }
        }
        previousBsdfPdf = previousWasNonDelta
            ? evaluateBsdfPdf(direction, normal, reflect(incidentDirection, normal), metallic, roughness)
            : 0.0;
        throughput *= albedo;

        if (bounce >= 2)
        {
            float survival = clamp(max(throughput.r, max(throughput.g, throughput.b)), 0.05, 0.95);
            if (random(rng) >= survival)
                break;
            throughput /= survival;
        }
    }
    return radiance;
}

float2 projectToPixel(float3 point, uint2 frameSize, float aspect, float halfFov)
{
    float depth = max(-point.z, 1e-4);
    float2 uv = float2(
        0.5 + point.x / (2.0 * halfFov * aspect * depth),
        0.5 - point.y / (2.0 * halfFov * depth));
    return uv * float2(frameSize) - 0.5;
}

float2 encodeOctahedralNormal(float3 normal)
{
    normal /= abs(normal.x) + abs(normal.y) + abs(normal.z);
    float2 encoded = normal.xy;
    if (normal.z < 0.0)
        encoded = (1.0 - abs(encoded.yx)) * sign(encoded.xy);
    return encoded * 0.5 + 0.5;
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

    uint pixelSeed = hash(pixel.x + pixel.y * frameSize.x);
    uint rotationRng = hash(pixelSeed ^ 0xa511e9b3);
    float2 rotation = float2(random(rotationRng), random(rotationRng));
    float aspect = float(frameSize.x) / float(frameSize.y);
    float halfFov = tan(radians(55.0) * 0.5);

    float2 centerUv = (float2(pixel) + 0.5) / float2(frameSize);
    float3 centerDirection = normalize(float3(
        (centerUv.x - 0.5) * 2.0 * halfFov * aspect,
        (0.5 - centerUv.y) * 2.0 * halfFov,
        -1.0));
    float primaryDistance;
    float3 primaryNormal, primaryAlbedo, primaryEmission;
    float primaryMetallic, primaryRoughness;
    uint primaryObjectId;
    bool primaryHit = intersectScene(float3(0.0), centerDirection, primaryDistance,
        primaryNormal, primaryAlbedo, primaryMetallic, primaryRoughness, primaryEmission, primaryObjectId);

    float2 previousPixel = float2(pixel);
    float previousViewDepth = 0.0;
    float currentViewDepth = 0.0;
    if (primaryHit && primaryObjectId < 4)
    {
        float4 previousSphere, previousMaterial, previousProperties;
        getSphere(primaryObjectId, params.PreviousTime, previousSphere, previousMaterial, previousProperties);
        float3 currentPoint = centerDirection * primaryDistance;
        float3 previousPoint = previousSphere.xyz + primaryNormal * previousSphere.w;
        previousPixel = projectToPixel(previousPoint, frameSize, aspect, halfFov);
        previousViewDepth = -previousPoint.z;
        currentViewDepth = -currentPoint.z;
    }
    else if (primaryHit && primaryObjectId == 8)
    {
        float3 currentPoint = centerDirection * primaryDistance;
        float3 localLightPoint = currentPoint - lightCenter(params.Time);
        float3 previousPoint = lightCenter(params.PreviousTime) + localLightPoint;
        previousPixel = projectToPixel(previousPoint, frameSize, aspect, halfFov);
        previousViewDepth = -previousPoint.z;
        currentViewDepth = -currentPoint.z;
    }
    else if (primaryHit)
    {
        float3 currentPoint = centerDirection * primaryDistance;
        currentViewDepth = -currentPoint.z;
        previousViewDepth = currentViewDepth;
    }

    float encodedSurface = float(primaryObjectId) + saturate(primaryRoughness) * 0.1;
    float2 encodedNormal = primaryHit ? encodeOctahedralNormal(primaryNormal) : float2(0.5, 0.5);
    guideImage[pixel] = float4(encodedNormal, currentViewDepth, encodedSurface);
    motionImage[pixel] = float4(previousPixel, previousViewDepth, encodedSurface);

    float3 color = float3(0.0);
    float luminanceSum = 0.0;
    float luminanceSquaredSum = 0.0;
    // Glossy paths change rapidly as reflected objects and lights move, so spend
    // extra fresh samples on the mirror-like sphere instead of reusing stale paths.
    uint samplesPerFrame = primaryHit && primaryRoughness < 0.1 ? 64 : 8;
    for (uint sampleIndex = 0; sampleIndex < samplesPerFrame; sampleIndex++)
    {
        uint sequenceIndex = params.SampleIndex * samplesPerFrame + sampleIndex;
        float2 jitter = frac(float2(sequenceIndex) * float2(0.754877666, 0.569840296) + rotation) - 0.5;
        float2 uv = (float2(pixel) + 0.5 + jitter) / float2(frameSize);
        float3 direction = normalize(float3((uv.x - 0.5) * 2.0 * halfFov * aspect, (0.5 - uv.y) * 2.0 * halfFov, -1.0));
        uint pathRng = hash(pixelSeed ^ (sequenceIndex * 0x9e3779b9));
        float3 sampleColor = tracePath(float3(0.0), direction, pathRng);
        float sampleLuminance = dot(sampleColor, float3(0.2126, 0.7152, 0.0722));
        color += sampleColor;
        luminanceSum += sampleLuminance;
        luminanceSquaredSum += sampleLuminance * sampleLuminance;
    }

    float inverseSamples = 1.0 / float(samplesPerFrame);
    float3 estimate = color * inverseSamples;
    float meanLuminance = luminanceSum * inverseSamples;
    float variance = max(luminanceSquaredSum * inverseSamples - meanLuminance * meanLuminance, 0.0);
    outputImage[pixel] = float4(estimate, variance);
}
