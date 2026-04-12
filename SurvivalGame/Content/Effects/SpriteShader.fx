#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0

float4x4 MatrixTransform;
float GlobalScale; // Global scale multiplier

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

struct VertexShaderInput
{
    // Changed to float3 to accept X, Y, and Z
    float3 Position : POSITION0; 
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output;

    // 1. APPLY GLOBAL SCALE 
    // We scale X and Y, but usually keep Z (depth) as is
    float3 finalPos = input.Position;
    finalPos.xy *= GlobalScale;

    // 2. FLIP Y POSITION
    // Moving this here ensures the flip happens in "World Space"
    finalPos.y = -finalPos.y;

    // 3. TRANSFORM TO SCREEN SPACE
    // We convert our float3 to a float4 (w = 1.0) for matrix math
    output.Position = mul(float4(finalPos, 1.0), MatrixTransform);
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    return tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};