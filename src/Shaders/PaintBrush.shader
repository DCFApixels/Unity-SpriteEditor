Shader "Hidden/TextureCompositor/PaintBrush"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _SrcBlend ("Source Blend", Float) = 1
        _DstBlend ("Destination Blend", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        ZTest Always
        ZWrite Off
        Cull Off
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 clipMin : TEXCOORD1;
                float2 clipMax : TEXCOORD2;
                float3 clipData : TEXCOORD3;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 brushUv : TEXCOORD0;
                float2 canvasUv : TEXCOORD1;
                float2 clipMin : TEXCOORD2;
                float2 clipMax : TEXCOORD3;
                float3 clipData : TEXCOORD4;
            };

            fixed4 _Color;
            float _Hardness;
            float2 _CanvasSize;
            float2 _PatternCenter;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.brushUv = input.uv;
                output.canvasUv = input.vertex.xy;
                output.clipMin = input.clipMin;
                output.clipMax = input.clipMax;
                output.clipData = input.clipData;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                if (input.clipData.x > 0.5 && input.clipData.x < 1.5)
                {
                    if (input.canvasUv.x < input.clipMin.x || input.canvasUv.y < input.clipMin.y ||
                        input.canvasUv.x > input.clipMax.x || input.canvasUv.y > input.clipMax.y)
                    {
                        discard;
                    }
                }
                else if (input.clipData.x > 2.5)
                {
                    float2 delta = (input.canvasUv - _PatternCenter) * _CanvasSize;
                    float x = dot(delta, input.clipMin);
                    float y = dot(delta, float2(-input.clipMin.y, input.clipMin.x));
                    if ((input.clipMax.x > 0.5 && x < 0.0) || (input.clipMax.x < -0.5 && x >= 0.0) ||
                        (input.clipMax.y > 0.5 && y < 0.0) || (input.clipMax.y < -0.5 && y >= 0.0))
                        discard;
                }
                else if (input.clipData.x > 1.5)
                {
                    float2 delta = (input.canvasUv - _PatternCenter) * _CanvasSize;
                    float angle = atan2(delta.y, delta.x);
                    float difference = atan2(
                        sin(angle - input.clipData.y),
                        cos(angle - input.clipData.y));
                    if (abs(difference) > input.clipData.z)
                        discard;
                }

                float radius = length(input.brushUv - 0.5) * 2.0;
                if (radius > 1.0)
                    discard;
                float inner = min(saturate(_Hardness), 0.9999);
                float coverage = 1.0 - smoothstep(inner, 1.0, radius);
                float alpha = saturate(_Color.a * coverage);
                return fixed4(_Color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
