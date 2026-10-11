float4x4 MatrixTransform;
Texture2D Texture;
float2 TextureSize;

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
    float3 Position : TEXCOORD1;
    float2 Size     : TEXCOORD2;
    float  Rotation : TEXCOORD3;
    float4 Color    : COLOR1;
    float2 RectPos  : TEXCOORD4;
    float2 RectSize : TEXCOORD5;
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

    float2 scaled = vertex.Position.xy * instance.Size;

    //Rotate
    float c = cos(instance.Rotation);
    float s = sin(instance.Rotation);
    float2 rotated = float2(
        scaled.x * c - scaled.y * s,
        scaled.x * s + scaled.y * c
    );

    //translate
    float4 worldPos = float4(rotated.x + instance.Position.x, rotated.y + instance.Position.y, instance.Position.z, 1.0);

    output.Position = mul(worldPos, MatrixTransform);
    output.Color    = vertex.Color * instance.Color;
    output.TextureCoordinates = (instance.RectPos + vertex.TextureCoordinates * instance.RectSize) / TextureSize;

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