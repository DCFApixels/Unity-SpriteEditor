Shader "Hidden/TextureCompositor/MakeSeamless"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
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
            float4 _MainTex_TexelSize;
            float4 _Directions;
            float _BlendWidth, _Falloff;

            float Weight(float position, float direction)
            {
                if (direction < .5) return 0;
                float distance = direction < 1.5 ? 1 - position : position;
                float t = saturate(1 - distance / _BlendWidth);
                return pow(t * t * (3 - 2 * t), _Falloff);
            }
            float4 Sample(float2 uv)
            {
                float4 c = tex2D(_MainTex, uv);
                c.a = saturate(c.a);
                return float4(c.rgb * c.a, c.a);
            }
            float4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                // Pixel centers, not texture borders: the opposite edge texels match exactly.
                float2 texel = abs(_MainTex_TexelSize.xy);
                float2 position = saturate((uv - .5 * texel) / max(1 - texel, .000001));
                float x = Weight(position.x, _Directions.x);
                float y = Weight(position.y, _Directions.y);
                float4 original = Sample(uv);
                float4 acrossX = Sample(float2(1 - uv.x, uv.y));
                float4 acrossY = Sample(float2(uv.x, 1 - uv.y));
                float4 acrossBoth = Sample(1 - uv);
                float4 c = lerp(lerp(original, acrossX, x), lerp(acrossY, acrossBoth, x), y);
                return float4(c.a > 0 ? c.rgb / c.a : 0, c.a);
            }
            ENDCG
        }
    }
}
