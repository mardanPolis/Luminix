float4x4 MatrixTransform;
Texture2D Texture;
float textureSize;
float textureRegionSize;
float textureRegionOrigin;

float2 Scale;

float PositionZ;

sampler2D TextureSampler = sampler_state
{
    Texture = <Texture>;
    Filter = Point;
};

struct VertexInput 
{
    float3 Position : POSITION;
    float4 Color : COLOR;
    float2 TextureCoordinates : TEXCOORD0;
};

struct InstanceInput
{
    float2 Position : POSITION;
    float2 SourceRectPos  : TEXCOORD0; 
};

struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput MainVS(VertexInput vertex, InstanceInput instance)
{
    VertexShaderOutput output;
    
    float2 localPos = (vertex.Position.xy * textureRegionSize - textureRegionOrigin) * Scale.xy;
    float4 worldPos = float4(localPos + instance.Position.xy, PositionZ, 1);

    output.Position = mul(worldPos, MatrixTransform);
    output.Color = vertex.Color;
    output.TextureCoordinates = (vertex.TextureCoordinates * textureRegionSize + instance.SourceRectPos) / textureSize;

    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    return tex2D(TextureSampler, input.TextureCoordinates) * input.Color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
};