Shader "Hidden/TextureCompositor/Transform"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always
        ZWrite Off
        Cull Off
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            #if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)
                #define TRANSFORM_NATIVE_SAMPLERS
                Texture2D _MainTex;
                SamplerState sampler_MainTex;
                SamplerState transform_linear_repeat_sampler;
                SamplerState transform_linear_mirror_sampler;
            #else
                sampler2D _MainTex;
            #endif
            float4 _MainTex_TexelSize;
            float2 _Pivot;
            float2 _Position;
            float2 _Scale;
            float _Rotation;
            float2 _OutputSize;
            int _TilingMode;

            fixed4 SampleTiled(float2 uv)
            {
                #if defined(TRANSFORM_NATIVE_SAMPLERS)
                    if (_TilingMode == 1)
                        return _MainTex.SampleLevel(transform_linear_repeat_sampler, uv, 0);
                    return _MainTex.SampleLevel(transform_linear_mirror_sampler, uv, 0);
                #else
                    float2 texel = abs(_MainTex_TexelSize.xy);
                    if (_TilingMode == 2)
                        uv = 1.0 - abs(frac(uv * 0.5) * 2.0 - 1.0);
                    else
                        uv = frac(uv);
                    float2 pixel = uv * _MainTex_TexelSize.zw - 0.5;
                    float2 blend = frac(pixel);
                    float2 first = (floor(pixel) + 0.5) * texel;
                    float2 second = first + texel;
                    if (_TilingMode == 2)
                    {
                        first = clamp(first, texel * 0.5, 1.0 - texel * 0.5);
                        second = clamp(second, texel * 0.5, 1.0 - texel * 0.5);
                    }
                    else
                    {
                        first = frac(first);
                        second = frac(second);
                    }
                    fixed4 bottom = lerp(
                        tex2Dlod(_MainTex, float4(first, 0, 0)),
                        tex2Dlod(_MainTex, float4(second.x, first.y, 0, 0)), blend.x);
                    fixed4 top = lerp(
                        tex2Dlod(_MainTex, float4(first.x, second.y, 0, 0)),
                        tex2Dlod(_MainTex, float4(second, 0, 0)), blend.x);
                    return lerp(bottom, top, blend.y);
                #endif
            }

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 pivotPixels = _Pivot * _OutputSize;
                float2 local = input.uv * _OutputSize - pivotPixels - _Position;

                // Inverse visual rotation: map destination pixels back to source UVs.
                float sine = sin(-_Rotation);
                float cosine = cos(-_Rotation);
                local = float2(
                    cosine * local.x - sine * local.y,
                    sine * local.x + cosine * local.y);

                float2 safeScale = float2(
                    _Scale.x < 0.0 ? min(_Scale.x, -0.00001) : max(_Scale.x, 0.00001),
                    _Scale.y < 0.0 ? min(_Scale.y, -0.00001) : max(_Scale.y, 0.00001));
                float2 sourceUV = (pivotPixels + local / safeScale) / _OutputSize;

                if (_TilingMode == 1 || _TilingMode == 2)
                    return SampleTiled(sourceUV);
                if (sourceUV.x < 0.0 || sourceUV.x > 1.0 || sourceUV.y < 0.0 || sourceUV.y > 1.0)
                    return fixed4(0, 0, 0, 0);
                #if defined(TRANSFORM_NATIVE_SAMPLERS)
                    return _MainTex.Sample(sampler_MainTex, sourceUV);
                #else
                    return tex2D(_MainTex, sourceUV);
                #endif
            }
            ENDCG
        }
    }
}
