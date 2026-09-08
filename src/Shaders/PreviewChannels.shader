Shader "Hidden/TextureCompositor/PreviewChannels"
{
    Properties
    {
        _MainTex ("Preview", 2D) = "white" {}
        _Channels ("RGBA", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Channels;

            fixed4 frag(v2f_img input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                float colorChannels = _Channels.r + _Channels.g + _Channels.b;
                if (colorChannels < 0.5)
                    return fixed4(color.aaa * _Channels.a, 1.0);
                if (colorChannels < 1.5)
                {
                    fixed value = dot(color.rgb, _Channels.rgb);
                    return fixed4(value, value, value, lerp(1.0, color.a, _Channels.a));
                }
                return fixed4(color.rgb * _Channels.rgb, lerp(1.0, color.a, _Channels.a));
            }
            ENDCG
        }
    }
}
