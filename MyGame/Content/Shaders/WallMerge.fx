#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Wall bleed merge pass — adds the blurred lightmap back into the
// non-blurred one, but only where the occlusion mask is positive
// (i.e. pixels reached by a shadow-casting light). This restricts the
// glow to lit areas, instead of bleeding light into the dark behind
// occluders like the original fullscreen-add approach did.
Texture2D BlurredLightMap;
Texture2D OcclusionMask;

sampler2D BlurredSampler = sampler_state
{
    Texture = <BlurredLightMap>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

sampler2D OcclusionSampler = sampler_state
{
    Texture = <OcclusionMask>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

struct VSIn
{
    float2 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOut
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VSOut MainVS(VSIn input)
{
    VSOut o;
    o.Position = float4(input.Position.xy, 0.0, 1.0);
    o.TexCoord = input.TexCoord;
    return o;
}

float4 MainPS(VSOut input) : COLOR0
{
    float3 blurred = tex2D(BlurredSampler, input.TexCoord).rgb;
    // OcclusionMask is single-channel (Alpha8 preferred, Color fallback).
    // The .a sample works for both formats.
    float mask = tex2D(OcclusionSampler, input.TexCoord).a;
    return float4(blurred * mask, 1.0);
}

technique WallMerge
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS();
    }
};
