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
                if (_Channels.r + _Channels.g + _Channels.b < 0.5)
                    return fixed4(color.aaa * _Channels.a, 1.0);
                return fixed4(color.rgb * _Channels.rgb, lerp(1.0, color.a, _Channels.a));
            }
            ENDCG
        }
    }
}
