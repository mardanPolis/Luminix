float4x4 MatrixTransform;

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
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
    float3 Position : TEXCOORD1;
    float2 Origin : TEXCOORD2;
    float2 Scale : TEXCOORD3;
    float4 SourceRect  : TEXCOORD4; 
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

    float4 pos = float4(((vertex.Position.xy + instance.Position) * instance.SourceRect.zw - instance.Origin.xy) * instance.Scale, 0, 1);

    output.Position = mul(pos, MatrixTransform);
    output.Color = vertex.Color;
    output.TextureCoordinates = vertex.TextureCoordinates;
    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    return tex2D(SpriteTextureSampler, input.TextureCoordinates);
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 MainPS();
    }
};