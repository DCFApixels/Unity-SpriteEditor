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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 brushUv : TEXCOORD0;
                float2 canvasUv : TEXCOORD1;
            };

            fixed4 _Color;
            float _Hardness;
            float2 _CanvasSize;
            float _ClipMode;
            float4 _ClipRect;
            float2 _PatternCenter;
            float _ClipAngleCenter;
            float _ClipAngleHalfWidth;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.brushUv = input.uv;
                output.canvasUv = input.vertex.xy;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                if (_ClipMode > 0.5 && _ClipMode < 1.5)
                {
                    if (input.canvasUv.x < _ClipRect.x || input.canvasUv.y < _ClipRect.y ||
                        input.canvasUv.x > _ClipRect.z || input.canvasUv.y > _ClipRect.w)
                    {
                        discard;
                    }
                }
                else if (_ClipMode > 1.5)
                {
                    float2 delta = (input.canvasUv - _PatternCenter) * _CanvasSize;
                    float angle = atan2(delta.y, delta.x);
                    float difference = atan2(
                        sin(angle - _ClipAngleCenter),
                        cos(angle - _ClipAngleCenter));
                    if (abs(difference) > _ClipAngleHalfWidth)
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
