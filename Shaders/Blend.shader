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

            fixed4 frag(v2f_img input) : SV_Target
            {
                float4 backdrop = tex2D(_MainTex, input.uv);
                float4 source = tex2D(_Blend, input.uv);
                float opacity = saturate(_Opacity);

                // Alpha-only union used when a non-isolated group is an effect target.
                if (_Mode == 3)
                {
                    float alpha = 1.0 - (1.0 - saturate(backdrop.a)) * (1.0 - saturate(source.a * opacity));
                    return float4(alpha, alpha, alpha, alpha);
                }

                // True overwrite replaces RGBA. Opacity interpolates the complete pixel,
                // so an opaque overwrite can also erase the backdrop with transparent pixels.
                if (_Mode == 2)
                    return lerp(backdrop, source, opacity);

                float sourceAlpha = saturate(source.a * opacity);
                float backdropAlpha = saturate(backdrop.a);
                float outputAlpha = sourceAlpha + backdropAlpha * (1.0 - sourceAlpha);
                float3 premultiplied;

                if (_Mode == 1) // Multiply using the standard separable blend formula.
                {
                    float3 multiplied = backdrop.rgb * source.rgb;
                    premultiplied = sourceAlpha * (1.0 - backdropAlpha) * source.rgb
                                  + sourceAlpha * backdropAlpha * multiplied
                                  + (1.0 - sourceAlpha) * backdropAlpha * backdrop.rgb;
                }
                else // Normal/source-over.
                {
                    premultiplied = source.rgb * sourceAlpha
                                  + backdrop.rgb * backdropAlpha * (1.0 - sourceAlpha);
                }

                float3 outputColor = outputAlpha > 0.00001
                    ? premultiplied / outputAlpha
                    : float3(0.0, 0.0, 0.0);
                return float4(outputColor, outputAlpha);
            }
            ENDCG
        }
    }
}
