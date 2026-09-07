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
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _Pivot;
            float2 _Position;
            float2 _Scale;
            float _Rotation;
            float2 _OutputSize;

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

                if (sourceUV.x < 0.0 || sourceUV.x > 1.0 || sourceUV.y < 0.0 || sourceUV.y > 1.0)
                    return fixed4(0, 0, 0, 0);
                return tex2D(_MainTex, sourceUV);
            }
            ENDCG
        }
    }
}
