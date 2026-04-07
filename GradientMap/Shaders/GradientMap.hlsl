Texture2D    InputTexture    : register(t0);
SamplerState InputSampler    : register(s0);
Texture2D    GradientTexture : register(t1);
SamplerState GradientSampler : register(s1);

cbuffer constants : register(b0)
{
    float opacity      : packoffset(c0.x);
    int   blendMode    : packoffset(c0.y);
    int   isHorizontal : packoffset(c0.z);
    float _pad         : packoffset(c0.w);
};

float GetLuminance(float3 c)
{
    return dot(c, float3(0.299f, 0.587f, 0.114f));
}

float4 main(
    float4 pos      : SV_POSITION,
    float4 posScene : SCENE_POSITION,
    float4 uv0      : TEXCOORD0,
    float4 uv1      : TEXCOORD1
) : SV_Target
{
    float4 src = InputTexture.Sample(InputSampler, uv0.xy);

    if (src.a <= 0.0f)
        return src;

    float3 lin = src.rgb / src.a;
    float  lum = saturate(GetLuminance(lin));

    float2 gradUV = (isHorizontal != 0)
        ? float2(lum, 0.5f)
        : float2(0.5f, 1.0f - lum);

    float4 gs      = GradientTexture.Sample(GradientSampler, gradUV);
    float3 gradLin = (gs.a > 1e-6f) ? gs.rgb / gs.a : float3(0.0f, 0.0f, 0.0f);

    float3 blended;

    if (blendMode == 1)
    {
        float gradLum = GetLuminance(gradLin);
        blended = saturate(lin + (gradLum - lum));
    }
    else if (blendMode == 2)
    {
        float gl = length(gradLin);
        blended  = (gl > 1e-6f) ? normalize(gradLin) * lum : lin;
        blended  = saturate(blended);
    }
    else
    {
        blended = gradLin;
    }

    float3 result = lerp(lin, blended, opacity);
    return saturate(float4(result * src.a, src.a));
}
