#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Light shader — draws a single point-light disk into the lightmap with
// soft shadows sampled from the shadow map. Ported from Robust Toolbox's
// Clyde light-soft.swsl.
//
// The engine feeds this via DrawUserPrimitives (not SpriteBatch) so it
// can supply both POSITION0 (world-space corner) and TEXCOORD0
// (world-space position, same as POSITION0 — used for the shadow-map
// angle lookup). The VS applies the engine's viewProj so the disk is
// rasterized at the correct screen position.

// --- Engine-supplied transform ---
float4x4 viewProj;         // camera view × orthographic projection

// --- Per-light uniforms ---
float2 lightCenter;
float4 lightColor;
float  lightRange;
float  lightPower;
float  lightSoftness;
float  lightFalloff;
float  lightCurveFactor;
float  lightIndex;      // NORMALIZED V coord (0..1) into shadow map, or -1 for no shadow
float  lightRadius;     // == lightRange; needed to unpack shadow-map depth
float2 shadowMapTexel;  // 1 / shadow map size
float  shadowContactBias;

// --- Spotlight-only uniforms (unused by the LightSoft/LightHard techniques) ---
float  lightDirection;     // cone center angle, radians, 0 = +X
float  lightConeAngle;     // cone half-angle, radians
float  lightConeSoftness;  // edge softness, radians

// --- Shadow map ---
Texture2D ShadowMap;

sampler2D ShadowSampler = sampler_state
{
    Texture = <ShadowMap>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Wrap;
    AddressV = Clamp;
};

struct VSIn
{
    float2 WorldPos    : POSITION0;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float2 WorldPos : TEXCOORD0;
};

VSOut MainVS(VSIn input)
{
    VSOut o;
    o.Position = mul(float4(input.WorldPos.xy, 0.0, 1.0), viewProj);
    o.WorldPos = input.WorldPos;
    return o;
}

float ShadowDepthAt(float2 rel, float texelOffset)
{
    float deflect = atan2(rel.y, -rel.x) / 3.14159265; // -1..+1
    float u = deflect * 0.5 + 0.5 + texelOffset * shadowMapTexel.x;
    return tex2D(ShadowSampler, float2(u, lightIndex)).r;
}

float ShadowVisibility(float2 rel, float texelOffset)
{
    float blocker = ShadowDepthAt(rel, texelOffset);
    if (blocker >= 0.999)
        return 1.0;

    float receiver = length(rel) / max(lightRadius, 0.0001);
    return receiver <= blocker - shadowContactBias ? 1.0 : 0.0;
}

float CreateOcclusion(float2 diff)
{
    if (lightIndex < 0.0)
        return 1.0;

    float center = ShadowDepthAt(diff, 0.0);
    if (center >= 0.999)
        return 1.0;

    float receiver = length(diff) / max(lightRadius, 0.0001);
    float penumbra = saturate((receiver - center) * 18.0);
    float radius = max(0.25, lightSoftness) * lerp(0.75, 3.0, penumbra);

    float occ = ShadowVisibility(diff, 0.0) * 0.28;
    occ += ShadowVisibility(diff, -radius) * 0.20;
    occ += ShadowVisibility(diff,  radius) * 0.20;
    occ += ShadowVisibility(diff, -radius * 2.0) * 0.12;
    occ += ShadowVisibility(diff,  radius * 2.0) * 0.12;
    occ += ShadowVisibility(diff, -radius * 3.0) * 0.04;
    occ += ShadowVisibility(diff,  radius * 3.0) * 0.04;

    return saturate(occ);
}

// Returns 0..1 cone attenuation for a spotlight: 1 inside the cone, 0
// outside, smoothly blended across lightConeSoftness near the edge.
// diff of (near-)zero length (pixel at the light center) is always lit —
// direction is undefined there.
float ConeFactor(float2 diff)
{
    float len = length(diff);
    if (len < 0.0001)
        return 1.0;

    float2 dirVec = float2(cos(lightDirection), sin(lightDirection));
    float2 nrm = diff / len;
    float angleToPixel = acos(clamp(dot(dirVec, nrm), -1.0, 1.0));

    if (angleToPixel > lightConeAngle)
        return 0.0;

    return 1.0 - smoothstep(lightConeAngle - lightConeSoftness, lightConeAngle, angleToPixel);
}

float4 MainPS(VSOut input) : COLOR0
{
    float2 diff = input.WorldPos - lightCenter;
    float ourDist = length(diff);

    // Skip pixels outside the light's reach.
    if (ourDist > lightRange)
        discard;

    // Shadow / occlusion lookup.
    float occlusion = CreateOcclusion(diff);
    if (occlusion <= 0.001)
        discard;

    // Inverse-shape attenuation adapted from
    // https://lisyarus.github.io/blog/posts/point-light-attenuation.html
    float s = saturate(ourDist / lightRange);
    float s2 = s * s;
    float curve = lerp(s, s2, saturate(lightCurveFactor));
    float atten = ((1.0 - s2) * (1.0 - s2)) / (1.0 + lightFalloff * curve);

    float val = atten * lightPower * occlusion;

    // Premultiplied-alpha output — Engine blends (SrcAlpha, One) so the
    // RGB is the light contribution and A is its strength.
    return float4(lightColor.rgb * val, val);
}

technique LightSoft
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS();
    }
};

// Single-sample shadow lookup — no PCF, no soft penumbra.
// Faster than LightSoft, use when shadow quality can be traded for speed.
float4 MainPS_Hard(VSOut input) : COLOR0
{
    float2 diff = input.WorldPos - lightCenter;
    float ourDist = length(diff);

    if (ourDist > lightRange)
        discard;

    float occlusion;
    if (lightIndex < 0.0)
        occlusion = 1.0;
    else
        occlusion = ShadowVisibility(diff, 0.0);

    if (occlusion <= 0.001)
        discard;

    float s = saturate(ourDist / lightRange);
    float s2 = s * s;
    float curve = lerp(s, s2, saturate(lightCurveFactor));
    float atten = ((1.0 - s2) * (1.0 - s2)) / (1.0 + lightFalloff * curve);

    float val = atten * lightPower * occlusion;
    return float4(lightColor.rgb * val, val);
}

technique LightHard
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS_Hard();
    }
};

// Spotlight variants — identical falloff/shadow math, gated by ConeFactor.
float4 MainPS_Spot(VSOut input) : COLOR0
{
    float2 diff = input.WorldPos - lightCenter;
    float ourDist = length(diff);

    if (ourDist > lightRange)
        discard;

    float cone = ConeFactor(diff);
    if (cone <= 0.001)
        discard;

    float occlusion = CreateOcclusion(diff);
    if (occlusion <= 0.001)
        discard;

    float s = saturate(ourDist / lightRange);
    float s2 = s * s;
    float curve = lerp(s, s2, saturate(lightCurveFactor));
    float atten = ((1.0 - s2) * (1.0 - s2)) / (1.0 + lightFalloff * curve);

    float val = atten * lightPower * occlusion * cone;
    return float4(lightColor.rgb * val, val);
}

technique SpotLightSoft
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS_Spot();
    }
};

float4 MainPS_SpotHard(VSOut input) : COLOR0
{
    float2 diff = input.WorldPos - lightCenter;
    float ourDist = length(diff);

    if (ourDist > lightRange)
        discard;

    float cone = ConeFactor(diff);
    if (cone <= 0.001)
        discard;

    float occlusion;
    if (lightIndex < 0.0)
        occlusion = 1.0;
    else
        occlusion = ShadowVisibility(diff, 0.0);

    if (occlusion <= 0.001)
        discard;

    float s = saturate(ourDist / lightRange);
    float s2 = s * s;
    float curve = lerp(s, s2, saturate(lightCurveFactor));
    float atten = ((1.0 - s2) * (1.0 - s2)) / (1.0 + lightFalloff * curve);

    float val = atten * lightPower * occlusion * cone;
    return float4(lightColor.rgb * val, val);
}

technique SpotLightHard
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS_SpotHard();
    }
};
