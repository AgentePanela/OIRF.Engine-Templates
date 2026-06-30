#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Occlusion mask shader — paints the silhouette of "pixels reached by a
// shadow-casting light" into the OcclusionMask render target. The
// LightingSystem then samples this mask in the wall-bleed pass to keep
// the blurred light contribution inside the actual illuminated region,
// instead of adding the blurred lightmap everywhere on screen.
//
// The VS is identical to LightSoft.fx: takes a world-space quad, applies
// viewProj, and forwards the world position so the PS can compute the
// per-pixel angle and distance to the light. The PS then mirrors the
// CreateOcclusion math (kept in sync with LightSoft.fx — MonoGame's
// EffectCompiler doesn't reliably pull .fxh includes, so we duplicate
// rather than risk silent shadow regressions) and writes the result as
// alpha into a single-channel target.

// --- Engine-supplied transform ---
float4x4 viewProj;

// --- Per-light uniforms (mirrors LightSoft.fx) ---
float2 lightCenter;
float  lightRange;
float  lightSoftness;
float  lightIndex;
float  lightRadius;
float2 shadowMapTexel;
float  shadowContactBias;

// --- Spotlight-only uniforms (unused by the OcclusionMask/OcclusionMaskHard techniques) ---
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

// --- BEGIN mirror of LightSoft.fx occlusion helpers ---

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

// --- END mirror of LightSoft.fx occlusion helpers ---

// Mirrors ConeFactor in LightSoft.fx — kept in sync for the same reason as
// the occlusion helpers above.
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

    if (ourDist > lightRange)
        discard;

    float occlusion = CreateOcclusion(diff);
    if (occlusion <= 0.001)
        discard;

    // Write occlusion as alpha. The LightingSystem draws all shadow-
    // casting lights additively into the mask, so a pixel covered by
    // N lights accumulates up to N. We use the raw occlusion (no
    // attenuation) because the mask answers the binary "is this pixel
    // lit by any shadow-casting light" question; the wall-bleed pass
    // applies the blur on top, not the per-pixel intensity.
    return float4(0, 0, 0, occlusion);
}

technique OcclusionMask
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS();
    }
};

// Single-sample variant — matches LightHard in LightSoft.fx.
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

    return float4(0, 0, 0, occlusion);
}

technique OcclusionMaskHard
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS_Hard();
    }
};

// Spotlight variants — gated by ConeFactor so the wall-bleed pass doesn't
// leak blurred light outside the cone.
float4 MainPS_Spot(VSOut input) : COLOR0
{
    float2 diff = input.WorldPos - lightCenter;
    float ourDist = length(diff);

    if (ourDist > lightRange)
        discard;

    float cone = ConeFactor(diff);
    if (cone <= 0.001)
        discard;

    float occlusion = CreateOcclusion(diff) * cone;
    if (occlusion <= 0.001)
        discard;

    return float4(0, 0, 0, occlusion);
}

technique SpotOcclusionMask
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
    occlusion *= cone;

    if (occlusion <= 0.001)
        discard;

    return float4(0, 0, 0, occlusion);
}

technique SpotOcclusionMaskHard
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS_SpotHard();
    }
};
