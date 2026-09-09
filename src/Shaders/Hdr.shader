Shader "Hidden/TextureCompositor/Hdr"
{
    Properties
    {
        // Graphics.Blit only binds its source automatically when this texture is declared.
        [HideInInspector] _MainTex ("Source", 2D) = "black" {}
        [HideInInspector] _Errors ("Accumulated errors", 2D) = "black" {}
        [HideInInspector] _Saturate ("Clamp layer output", Float) = 0
        [HideInInspector] _Encode ("Encode LDR output", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off
        CGINCLUDE
        #include "UnityCG.cginc"
        #include "HdrColor.cginc"
        sampler2D _MainTex, _Errors;
        float4 _MainTex_TexelSize;
        float _Saturate, _Encode;
        // Inspect bits so fast-math cannot turn NaN comparisons into an always-finite result.
        bool invalid(float v) { return (asuint(v) & 0x7fffffffu) > 0x477fe000u; }
        float safe(float v) { return (asuint(v) & 0x7fffffffu) >= 0x7f800000u ? 0.0 : clamp(v, -65504.0, 65504.0); }
        float4 clean(v2f_img i) : SV_Target
        {
            float4 c = tex2D(_MainTex, i.uv);
            c = float4(safe(c.r), safe(c.g), safe(c.b), saturate(safe(c.a)));
            if (_Saturate > 0.5) c.rgb = saturate(c.rgb);
            if (_Encode > 0.5) c.rgb = SpriteEncode(saturate(c.rgb));
            return c;
        }
        float4 errors(v2f_img i) : SV_Target
        {
            float4 c = tex2D(_MainTex, i.uv);
            float bad = invalid(c.r) || invalid(c.g) || invalid(c.b) || invalid(c.a);
            return max(bad, tex2D(_Errors, i.uv).r);
        }
        float4 reduce(v2f_img i) : SV_Target
        {
            // Destination dimensions are ceil(source / 2); clamp handles odd edges.
            float2 p = floor(i.pos.xy) * 2.0;
            float2 t = _MainTex_TexelSize.xy;
            return max(max(tex2D(_MainTex, (p + float2(.5,.5))*t).r,
                           tex2D(_MainTex, (p + float2(1.5,.5))*t).r),
                       max(tex2D(_MainTex, (p + float2(.5,1.5))*t).r,
                           tex2D(_MainTex, (p + float2(1.5,1.5))*t).r));
        }
        ENDCG
        Pass { CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment clean
        ENDCG }
        Pass { CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment errors
        ENDCG }
        Pass { CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment reduce
            ENDCG }
    }
}
