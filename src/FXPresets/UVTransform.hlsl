// @whimtex-effect Transform/UV Transform
// @param transform2D _Area

float4 ApplyFX(float2 uv, float4 color)
{
    float2 localUV = _Area_ToLocal(uv);
    if (any(localUV < 0) || any(localUV > 1)) return 0;
    return SampleInput(localUV);
}
