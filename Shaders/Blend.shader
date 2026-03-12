Shader "Hidden/TextureCompositor/Blend"
{
    Properties
    {
        _MainTex ("Base", 2D) = "black" {}
        _Blend ("Blend", 2D) = "black" {}
        _Opacity ("Opacity", Float) = 1
        _Mode ("Mode", Float) = 0
    }
    SubShader
    {
        Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" "PreviewType"="Plane" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

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

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 base = tex2D(_MainTex, i.uv);
                fixed4 blend = tex2D(_Blend, i.uv);
                fixed4 result = base;

                if (_Mode == 0) // Overwrite
                {
                    result.a = saturate(base.a + blend.a * _Opacity);
                    result.rgb = lerp(base, blend, blend.a).rgb;
                }
                else if (_Mode == 1) // Multiply
                {
                    result.a = base.a;
                    result.rgb = lerp(result.rgb, base * blend, blend.a).rgb;
                }

                result.rgb = lerp(base, result, _Opacity).rgb;

                return result;
            }
            ENDCG
        }
    }
}