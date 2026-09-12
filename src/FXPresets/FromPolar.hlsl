// @whimtex-effect Distortion/Polar Coordinates/From Polar
// @param float _AngleOffset = 0
// @param float _RadialOffset = 0
// @param transform2D _Area

float4 ApplyFX(float2 uv, float4 color)
{
    float angle = (uv.x + _AngleOffset / 360.0) * 6.28318530718;
    float radius = uv.y + _RadialOffset;
    float sine, cosine;
    sincos(angle, sine, cosine);
    float2 localUV = 0.5 + float2(cosine, sine) * radius * 0.5;
    return SampleInput(_Area_ToInput(localUV));
}
