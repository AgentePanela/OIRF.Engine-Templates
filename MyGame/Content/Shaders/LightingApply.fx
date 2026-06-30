#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// SceneTexture: the frame as drawn by SpriteBatch (set by RenderManager).
Texture2D SceneTexture;

// LightMap: the lighting buffer — lit pixels are bright, unlit are dark.
Texture2D LightMap;

sampler2D SceneSampler = sampler_state
{
    Texture = <SceneTexture>;
};

sampler2D LightSampler = sampler_state
{
    Texture = <LightMap>;
    MinFilter = Linear;
    MagFilter = Linear;
};

sampler2D LightSamplerPoint = sampler_state
{
    Texture = <LightMap>;
    MinFilter = Point;
    MagFilter = Point;
};

// Global intensity multiplier (matches LightingManager.LightIntensity).
float Intensity = 1.0;

// Output color for areas that receive no light at all (debug helper).
float4 AmbientColor = float4(0.0, 0.0, 0.0, 1.0);

// Size of one lightmap texel in UV space (1/lightmapW, 1/lightmapH).
// Used by PixelatedLight to snap UVs to texel centres, eliminating mixels.
float2 LightmapTexelSize = float2(1.0, 1.0);

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 ApplyLight(float4 scene, float3 light)
{
    float3 result = scene.rgb * light + AmbientColor.rgb * (1.0 - light);
    return float4(result, scene.a);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 scene = tex2D(SceneSampler, input.TextureCoordinates);
    float3 light = tex2D(LightSampler, input.TextureCoordinates).rgb;
    return ApplyLight(scene, light);
}

float4 MainPS_Pixel(VertexShaderOutput input) : COLOR
{
    float4 scene = tex2D(SceneSampler, input.TextureCoordinates);
    // Snap to the centre of the lightmap texel that owns this screen pixel.
    // floor(uv / texelSize) gives the texel index; * texelSize + texelSize*0.5
    // moves to its centre. This guarantees each screen pixel maps to exactly
    // one lightmap texel with no bleeding between neighbours (no mixels).
    float2 uv = floor(input.TextureCoordinates / LightmapTexelSize) * LightmapTexelSize + LightmapTexelSize * 0.5;
    float3 light = tex2D(LightSamplerPoint, uv).rgb;
    return ApplyLight(scene, light);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};

technique PixelatedLight
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS_Pixel();
    }
};
