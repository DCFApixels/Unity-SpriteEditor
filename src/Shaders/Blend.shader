Shader "Hidden/TextureCompositor/Blend"
{
    Properties
    {
        _MainTex ("Base", 2D) = "black" {}
        _Blend ("Layer", 2D) = "black" {}
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Mode ("Mode", Float) = 0
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
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Blend;
            float _Mode;
            float _Opacity;
            float _HdrBlend;
            float _PreserveAlpha;
            float3 bound(float3 c) { return _HdrBlend > 0.5 ? c : saturate(c); }

            float3 ToSrgbBlendSpace(float3 c)
            {
                float3 magnitude = abs(c);
                float3 encoded = lerp(magnitude * 12.92,
                    1.055 * pow(magnitude, 1.0 / 2.4) - 0.055, step(0.0031308, magnitude));
                return sign(c) * encoded;
            }

            float3 FromSrgbBlendSpace(float3 c)
            {
                float3 magnitude = abs(c);
                float3 decoded = lerp(magnitude / 12.92,
                    pow((magnitude + 0.055) / 1.055, 2.4), step(0.04045, magnitude));
                return sign(c) * decoded;
            }

            float3 BlendColorDodge(float3 backdrop, float3 source)
            {
                if (_HdrBlend > 0.5) return backdrop / (1.0 - source);
                const float epsilon = 0.000001;
                float3 result = saturate(backdrop / max(1.0 - source, epsilon));
                result = lerp(result, 1.0, step(1.0 - epsilon, source));
                result = lerp(result, 0.0, 1.0 - step(epsilon, backdrop));
                return result;
            }

            float3 BlendColorBurn(float3 backdrop, float3 source)
            {
                if (_HdrBlend > 0.5) return 1.0 - (1.0 - backdrop) / source;
                const float epsilon = 0.000001;
                float3 result = 1.0 - saturate((1.0 - backdrop) / max(source, epsilon));
                result = lerp(result, 0.0, 1.0 - step(epsilon, source));
                result = lerp(result, 1.0, step(1.0 - epsilon, backdrop));
                return result;
            }

            float3 BlendSoftLight(float3 backdrop, float3 source)
            {
                float3 polynomial = ((16.0 * backdrop - 12.0) * backdrop + 4.0) * backdrop;
                float3 d = lerp(polynomial, sqrt(max(backdrop, 0.0)), step(0.25, backdrop));
                float3 dark = backdrop - (1.0 - 2.0 * source) * backdrop * (1.0 - backdrop);
                float3 light = backdrop + (2.0 * source - 1.0) * (d - backdrop);
                return lerp(dark, light, step(0.5, source));
            }

            float3 EvaluateBlend(float3 backdrop, float3 source, float mode)
            {
                if (mode == 1.0)  return backdrop * source;
                if (mode == 4.0)  return bound(backdrop + source);
                if (mode == 5.0)  return bound(backdrop - source);
                if (mode == 6.0)  return bound(backdrop / (_HdrBlend > 0.5 ? source : max(source, 0.000001)));
                if (mode == 7.0)  return 1.0 - (1.0 - backdrop) * (1.0 - source);
                if (mode == 8.0)
                {
                    float3 multiply = 2.0 * backdrop * source;
                    float3 screen = 1.0 - 2.0 * (1.0 - backdrop) * (1.0 - source);
                    return lerp(multiply, screen, step(0.5, backdrop));
                }
                if (mode == 9.0)  return min(backdrop, source);
                if (mode == 10.0) return max(backdrop, source);
                if (mode == 11.0) return BlendColorDodge(backdrop, source);
                if (mode == 12.0) return BlendColorBurn(backdrop, source);
                if (mode == 13.0) return bound(backdrop + source);
                if (mode == 14.0) return bound(backdrop + source - 1.0);
                if (mode == 15.0) return bound(backdrop + 2.0 * source - 1.0);
                if (mode == 16.0) return bound(source + 2.0 * backdrop - 1.0);
                if (mode == 17.0)
                {
                    float3 burn = BlendColorBurn(backdrop, bound(2.0 * source));
                    float3 dodge = BlendColorDodge(backdrop, bound(2.0 * source - 1.0));
                    return lerp(burn, dodge, step(0.5, source));
                }
                if (mode == 18.0)
                {
                    float3 dark = min(backdrop, 2.0 * source);
                    float3 light = max(backdrop, 2.0 * source - 1.0);
                    return lerp(dark, light, step(0.5, source));
                }
                if (mode == 19.0) return step(1.0, backdrop + source);
                if (mode == 20.0)
                {
                    float3 multiply = 2.0 * backdrop * source;
                    float3 screen = 1.0 - 2.0 * (1.0 - backdrop) * (1.0 - source);
                    return lerp(multiply, screen, step(0.5, source));
                }
                if (mode == 21.0) return BlendSoftLight(backdrop, source);
                if (mode == 22.0) return abs(backdrop - source);
                if (mode == 23.0) return backdrop + source - 2.0 * backdrop * source;
                if (mode == 24.0) return 1.0 - abs(1.0 - backdrop - source);
                return source;
            }

            float4 frag(v2f_img input) : SV_Target
            {
                float4 backdrop = tex2D(_MainTex, input.uv);
                float4 source = tex2D(_Blend, input.uv);
                float opacity = saturate(_Opacity);

                if (_Mode == 102.0)
                    return float4(backdrop.rgb, saturate(backdrop.a) * saturate(source.a) * opacity);

                // Alpha-only union used when a group is an effect target.
                if (_Mode == 100.0)
                {
                    float alpha = 1.0 - (1.0 - saturate(backdrop.a)) * (1.0 - saturate(source.a * opacity));
                    return float4(alpha, alpha, alpha, alpha);
                }

                // Pass-through opacity interpolates the before/after group in premultiplied linear light.
                if (_Mode == 101.0)
                {
                    float a = lerp(backdrop.a, source.a, opacity);
                    float3 rgb = lerp(backdrop.rgb * backdrop.a, source.rgb * source.a, opacity);
                    return float4(a > 0.000001 ? rgb / a : 0.0, a);
                }

                if (opacity <= 0.0) return backdrop;
                if (_PreserveAlpha > 0.5)
                {
                    // All members share the base alpha. Mixing their coverage by
                    // source-over would incorrectly make soft base edges opaque.
                    if (_Mode == 3 || backdrop.a <= 0.0 || source.a <= 0.0) return backdrop;
                    bool hdr = _HdrBlend > 0.5;
                    float3 b = hdr ? backdrop.rgb : ToSrgbBlendSpace(backdrop.rgb);
                    float3 s = hdr ? source.rgb : ToSrgbBlendSpace(source.rgb);
                    float3 color = bound(EvaluateBlend(bound(b), bound(s), _Mode));
                    color = lerp(b, color, saturate(source.a * opacity));
                    return float4(hdr ? color : FromSrgbBlendSpace(color), backdrop.a);
                }

                if (_Mode == 2)
                    // Outside a clipping chain, overwrite replaces the complete RGBA pixel.
                    return lerp(backdrop, source, opacity);

                // DMBlend.None is a true no-op for both color and alpha.
                if (_Mode == 3)
                    return backdrop;

                float sourceAlpha = saturate(source.a * opacity);
                if (sourceAlpha <= 0.0) return backdrop;
                float backdropAlpha = saturate(backdrop.a);
                if (backdropAlpha <= 0.0) return float4(source.rgb, sourceAlpha);
                float outputAlpha = sourceAlpha + backdropAlpha * (1.0 - sourceAlpha);
                bool hdr = _HdrBlend > 0.5;
                float3 backdropColor = hdr ? backdrop.rgb : ToSrgbBlendSpace(backdrop.rgb);
                float3 sourceColor = hdr ? source.rgb : ToSrgbBlendSpace(source.rgb);
                float3 blendedColor = bound(EvaluateBlend(bound(backdropColor), bound(sourceColor), _Mode));

                // The blend function applies only to overlap. Non-overlap retains unbounded RGB.
                float3 premultiplied = sourceAlpha * (1.0 - backdropAlpha) * sourceColor
                                     + sourceAlpha * backdropAlpha * blendedColor
                                     + (1.0 - sourceAlpha) * backdropAlpha * backdropColor;
                float3 outputColor = premultiplied / outputAlpha;
                return float4(hdr ? outputColor : FromSrgbBlendSpace(outputColor), outputAlpha);
            }
            ENDCG
        }
    }
}
