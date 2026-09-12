// @whimtex-effect Distortion/Spherize
// @param float _Strength = 0.5 [-1 .. 1]
// @param transform2D _Area

float4 ApplyFX(float2 uv, float4 color)
{
    if (_Strength == 0) return color;

    float2 p = (_Area_ToLocal(uv) - 0.5) * 2.0;
    float exponent = exp2(_Strength);
    // The epsilon only protects the center; the radius is not limited to the frame.
    float scale = pow(max(dot(p, p), 1e-12), 0.5 * (exponent - 1.0));
    float2 sampleUV = _Area_ToInput(0.5 + p * scale * 0.5);
    return SampleInput(sampleUV);
}
