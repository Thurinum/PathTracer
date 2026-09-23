module BlinnPhong;

public float3 BlinnPhongDiffuse(float3 normal, float3 lightDir, float3 albedo)
{
    return albedo * abs(dot(normal, lightDir));
}

public float3 BlinnPhongSpecular(float3 normal, float3 lightDir, float3 viewDir, float shininess, float strength)
{
    float3 halfVector = normalize(lightDir + viewDir);
    return float3(strength * pow(saturate(dot(normal, halfVector)), shininess));
}

public float3 BlinnPhong(
    float3 diffuseNormal,
    float3 specularNormal,
    float3 lightDir,
    float3 viewDir,
    float3 albedo,
    float shininess,
    float specularStrength)
{
    return BlinnPhongDiffuse(diffuseNormal, lightDir, albedo)
         + BlinnPhongSpecular(specularNormal, lightDir, viewDir, shininess, specularStrength);
}
