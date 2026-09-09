Shader "Hidden/TextureCompositor/PaintBrush"
{
    Properties
    {
        _Color ("Linear brush RGBA", Vector) = (1, 1, 1, 1)
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
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 clipMin : TEXCOORD1;
                float2 clipMax : TEXCOORD2;
                float3 clipData : TEXCOORD3;
                float3 tileData : TEXCOORD4;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 brushUv : TEXCOORD0;
                float2 canvasUv : TEXCOORD1;
                float2 clipMin : TEXCOORD2;
                float2 clipMax : TEXCOORD3;
                float3 clipData : TEXCOORD4;
                float3 tileData : TEXCOORD5;
            };

            float4 _Color;
            sampler2D _Backdrop;
            float _PrepareStandard;
            float _Hardness;
            float2 _CanvasSize;
            float2 _PatternCenter;
            float2 _WrapBasisU;
            float2 _WrapBasisV;
            float3 _SourceToDocumentX;
            float3 _SourceToDocumentY;
            float _BrushSize;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.brushUv = input.uv;
                output.canvasUv = input.vertex.xy;
                output.clipMin = input.clipMin;
                output.clipMax = input.clipMax;
                output.clipData = input.clipData;
                output.tileData = input.tileData;
                return output;
            }

            float2 NearestPeriodicDelta(float2 delta)
            {
                float cross = _WrapBasisU.x * _WrapBasisV.y - _WrapBasisU.y * _WrapBasisV.x;
                float row = floor((_WrapBasisU.x * delta.y - _WrapBasisU.y * delta.x) / cross + 0.5);
                float2 best = 0.0;
                float bestDistance = 3.4e38;
                [unroll]
                for (int i = -1; i <= 1; i++)
                {
                    float2 candidate = delta - (row + i) * _WrapBasisV;
                    candidate -= floor(dot(candidate, _WrapBasisU) / dot(_WrapBasisU, _WrapBasisU) + 0.5) * _WrapBasisU;
                    float distance = dot(candidate, candidate);
                    if (distance < bestDistance) { best = candidate; bestDistance = distance; }
                }
                return best;
            }

            float4 frag(v2f input) : SV_Target
            {
                bool tiled = input.tileData.z > 0.5;
                float2 clipUv = input.canvasUv;
                float2 brushDelta = (input.brushUv - 0.5) * 2.0;
                if (tiled)
                {
                    float3 source = float3(input.canvasUv, 1.0);
                    float2 documentUv = float2(dot(source, _SourceToDocumentX), dot(source, _SourceToDocumentY));
                    if (any(documentUv < 0.0) || any(documentUv >= 1.0)) discard;
                    float2 nearest = NearestPeriodicDelta((input.canvasUv - input.tileData.xy) * _CanvasSize);
                    if (input.tileData.z < 1.5)
                    {
                        // Overlapping copies share one owner, so a single stamp cannot
                        // accumulate extra opacity where its wrapped footprints meet.
                        float2 ownership = nearest - (input.brushUv - 0.5) * _BrushSize;
                        if (dot(ownership, ownership) > 0.0001) discard;
                    }
                    clipUv = input.tileData.xy + nearest / _CanvasSize;
                    brushDelta = nearest * (2.0 / _BrushSize);
                }
                if (input.clipData.x > 0.5 && input.clipData.x < 1.5)
                {
                    if (((!tiled || input.clipMin.x > 0.0) && clipUv.x < input.clipMin.x) ||
                        ((!tiled || input.clipMin.y > 0.0) && clipUv.y < input.clipMin.y) ||
                        ((!tiled || input.clipMax.x < 1.0) && clipUv.x > input.clipMax.x) ||
                        ((!tiled || input.clipMax.y < 1.0) && clipUv.y > input.clipMax.y))
                    {
                        discard;
                    }
                }
                else if (input.clipData.x > 2.5)
                {
                    float2 delta = (clipUv - _PatternCenter) * _CanvasSize;
                    float x = dot(delta, input.clipMin);
                    float y = dot(delta, float2(-input.clipMin.y, input.clipMin.x));
                    if ((input.clipMax.x > 0.5 && x < 0.0) || (input.clipMax.x < -0.5 && x >= 0.0) ||
                        (input.clipMax.y > 0.5 && y < 0.0) || (input.clipMax.y < -0.5 && y >= 0.0))
                        discard;
                }
                else if (input.clipData.x > 1.5)
                {
                    float2 delta = (clipUv - _PatternCenter) * _CanvasSize;
                    float angle = atan2(delta.y, delta.x);
                    float difference = atan2(
                        sin(angle - input.clipData.y),
                        cos(angle - input.clipData.y));
                    if (abs(difference) > input.clipData.z)
                        discard;
                }

                float radius = length(brushDelta);
                if (radius > 1.0)
                    discard;
                float inner = min(saturate(_Hardness), 0.9999);
                float coverage = 1.0 - smoothstep(inner, 1.0, radius);
                float alpha = saturate(_Color.a * coverage);
                if (alpha <= 0.0) discard;
                if (_PrepareStandard > 0.5)
                {
                    float4 before = tex2D(_Backdrop, input.canvasUv);
                    return float4(clamp(before.rgb, 0.0, before.a), before.a);
                }
                return float4(_Color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
