Shader "Hidden/TextureCompositor/Noise"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "HdrColor.cginc"
            #include "ThirdParty/FastNoiseLite.hlsl"

            float4 _NoiseDomain, _NoiseFractalSettings;
            float _NoiseScale, _NoiseCellularJitter, _NoiseWarpStrength;
            int _NoiseSeed, _NoiseType, _NoiseFractal, _NoiseOctaves;
            int _NoiseCellularDistance, _NoiseCellularReturn, _NoiseWarp, _NoiseEncoding, _NoiseInverted;

            float4 frag(v2f_img i) : SV_Target
            {
                fnl_state state = fnlCreateState(_NoiseSeed);
                state.frequency = 1.0;
                state.noise_type = _NoiseType;
                state.fractal_type = _NoiseFractal;
                state.octaves = _NoiseOctaves;
                state.lacunarity = _NoiseFractalSettings.x;
                state.gain = _NoiseFractalSettings.y;
                state.weighted_strength = _NoiseFractalSettings.z;
                state.ping_pong_strength = _NoiseFractalSettings.w;
                state.cellular_distance_func = _NoiseCellularDistance;
                state.cellular_return_type = _NoiseCellularReturn;
                state.cellular_jitter_mod = _NoiseCellularJitter;
                float2 p = (i.uv - .5) * _NoiseDomain.xy * _NoiseScale + _NoiseDomain.zw;
                if (_NoiseWarp > 0 && _NoiseWarpStrength > 0.0)
                {
                    fnl_state warp = fnlCreateState(_NoiseSeed);
                    warp.frequency = 1.0;
                    warp.domain_warp_type = _NoiseWarp - 1;
                    warp.domain_warp_amp = _NoiseWarpStrength;
                    fnlDomainWarp2D(warp, p.x, p.y);
                }
                float value = saturate(fnlGetNoise2D(state, p.x, p.y) * .5 + .5);
                if (_NoiseInverted != 0) value = 1.0 - value;
                float3 rgb = value.xxx;
                if (_NoiseEncoding == 0) rgb = SpriteDecode(rgb);
                return float4(rgb, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
