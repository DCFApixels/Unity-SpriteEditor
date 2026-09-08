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
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Blend;
            float _Mode;
            float _Opacity;

            float3 ToPhotoshopBlendSpace(float3 color)
            {
                color = saturate(color);
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return color;
                #else
                    float3 low = color * 12.92;
                    float3 high = 1.055 * pow(color, 1.0 / 2.4) - 0.055;
                    return lerp(low, high, step(0.0031308, color));
                #endif
            }

            float3 FromPhotoshopBlendSpace(float3 color)
            {
                color = saturate(color);
                #if defined(UNITY_COLORSPACE_GAMMA)
                    return color;
                #else
                    float3 low = color / 12.92;
                    float3 high = pow((color + 0.055) / 1.055, 2.4);
                    return lerp(low, high, step(0.04045, color));
                #endif
            }

            float3 BlendColorDodge(float3 backdrop, float3 source)
            {
                const float epsilon = 0.000001;
                float3 result = saturate(backdrop / max(1.0 - source, epsilon));
                result = lerp(result, 1.0, step(1.0 - epsilon, source));
                result = lerp(result, 0.0, 1.0 - step(epsilon, backdrop));
                return result;
            }

            float3 BlendColorBurn(float3 backdrop, float3 source)
            {
                const float epsilon = 0.000001;
                float3 result = 1.0 - saturate((1.0 - backdrop) / max(source, epsilon));
                result = lerp(result, 0.0, 1.0 - step(epsilon, source));
                result = lerp(result, 1.0, step(1.0 - epsilon, backdrop));
                return result;
            }

            float3 BlendSoftLight(float3 backdrop, float3 source)
            {
                float3 polynomial = ((16.0 * backdrop - 12.0) * backdrop + 4.0) * backdrop;
                float3 d = lerp(polynomial, sqrt(backdrop), step(0.25, backdrop));
                float3 dark = backdrop - (1.0 - 2.0 * source) * backdrop * (1.0 - backdrop);
                float3 light = backdrop + (2.0 * source - 1.0) * (d - backdrop);
                return lerp(dark, light, step(0.5, source));
            }

            float3 EvaluateBlend(float3 backdrop, float3 source, float mode)
            {
                if (mode == 1.0)  return backdrop * source;
                if (mode == 4.0)  return saturate(backdrop + source);
                if (mode == 5.0)  return saturate(backdrop - source);
                if (mode == 6.0)  return saturate(backdrop / max(source, 0.000001));
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
                if (mode == 13.0) return saturate(backdrop + source);
                if (mode == 14.0) return saturate(backdrop + source - 1.0);
                if (mode == 15.0) return saturate(backdrop + 2.0 * source - 1.0);
                if (mode == 16.0) return saturate(source + 2.0 * backdrop - 1.0);
                if (mode == 17.0)
                {
                    float3 burn = BlendColorBurn(backdrop, saturate(2.0 * source));
                    float3 dodge = BlendColorDodge(backdrop, saturate(2.0 * source - 1.0));
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

                // Alpha-only union used when a non-isolated group is an effect target.
                if (_Mode == 100.0)
                {
                    float alpha = 1.0 - (1.0 - saturate(backdrop.a)) * (1.0 - saturate(source.a * opacity));
                    return float4(alpha, alpha, alpha, alpha);
                }

                // True overwrite replaces RGBA. Opacity interpolates the complete pixel,
                // so an opaque overwrite can also erase the backdrop with transparent pixels.
                if (_Mode == 2)
                    return lerp(backdrop, source, opacity);

                // DMBlend.None is a true no-op for both color and alpha.
                if (_Mode == 3)
                    return backdrop;

                float sourceAlpha = saturate(source.a * opacity);
                float backdropAlpha = saturate(backdrop.a);
                float outputAlpha = sourceAlpha + backdropAlpha * (1.0 - sourceAlpha);
                float3 backdropColor = ToPhotoshopBlendSpace(backdrop.rgb);
                float3 sourceColor = ToPhotoshopBlendSpace(source.rgb);
                float3 blendedColor = saturate(EvaluateBlend(backdropColor, sourceColor, _Mode));

                // Adobe/PDF source-over blending. The blend function contributes only where
                // both layers overlap; either layer keeps its own color outside that overlap.
                float3 premultiplied = sourceAlpha * (1.0 - backdropAlpha) * sourceColor
                                     + sourceAlpha * backdropAlpha * blendedColor
                                     + (1.0 - sourceAlpha) * backdropAlpha * backdropColor;
                float3 outputColor = outputAlpha > 0.000001
                    ? premultiplied / outputAlpha
                    : float3(0.0, 0.0, 0.0);
                return float4(FromPhotoshopBlendSpace(outputColor), outputAlpha);
            }
            ENDCG
        }
    }
}
