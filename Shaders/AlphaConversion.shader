Shader "Hidden/TextureCompositor/AlphaConversion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
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
            float _Mode;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float4 color = tex2D(_MainTex, input.uv);
                if (_Mode < 0.5)
                    return float4(color.rgb * color.a, color.a);
                if (color.a <= 0.00001)
                    return float4(0.0, 0.0, 0.0, 0.0);
                return float4(color.rgb / color.a, color.a);
            }
            ENDCG
        }
    }
}
