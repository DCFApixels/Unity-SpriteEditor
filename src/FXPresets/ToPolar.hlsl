// @whimtex-effect Distortion/Polar Coordinates/To Polar
// @param float _AngleOffset = 0
// @param float _RadialOffset = 0
// @param transform2D _Area

float4 ApplyFX(float2 uv, float4 color)
{
    float2 p = (_Area_ToLocal(uv) - 0.5) * 2.0;
    float radius = length(p);
    // The center has no unique angle. Choose zero without evaluating atan2(0, 0).
    float angle = 0.0;
    if (radius > 0.0) angle = atan2(p.y, p.x) / 6.28318530718;
    // Only the angular coordinate wraps; radius continues beyond the frame.
    float2 sampleUV = float2(frac(angle - _AngleOffset / 360.0), radius - _RadialOffset);
    return SampleInput(sampleUV);
}
