Shader "Hidden/TextureCompositor/PaintBrush"
{
    Properties
    {
        _Color ("Linear brush RGBA", Vector) = (1, 1, 1, 1)
        _BrushTip ("Brush Tip", 2D) = "white" {}
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
            #pragma multi_compile_local __ BRUSH_DYNAMICS BRUSH_TEXTURE
            #include "UnityCG.cginc"
            #include "HdrColor.cginc"
            float _HdrBlend;
            #include "ColorBlend.cginc"
            float _StampBlendEnabled, _StampBlendMode;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 clipMin : TEXCOORD1;
                float2 clipMax : TEXCOORD2;
                float3 clipData : TEXCOORD3;
                float3 tileData : TEXCOORD4;
                #if defined(BRUSH_DYNAMICS) || defined(BRUSH_TEXTURE)
                float3 color : TEXCOORD5;
                float4 size : TEXCOORD6;
                #endif
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
                #if defined(BRUSH_DYNAMICS) || defined(BRUSH_TEXTURE)
                float4 color : TEXCOORD6;
                float4 shape : TEXCOORD7;
                #endif
            };

            float4 _Color;
            sampler2D _BrushTip;
            float4 _BrushTip_TexelSize;
            float2 _TipAspect;
            float _TipChannel, _TipDecode, _TipEncodeMask, _TipStandard;
            float _TipSdf;
            sampler2D _BrushSdfGradient;
            float4 _BrushSdfGradient_TexelSize;
            sampler2D _Backdrop;
            sampler2D _SelectionMask;
            float _UseSelection, _SelectionWrap;
            float3 _SelectionToDocumentX, _SelectionToDocumentY;
            float _PrepareStandard;
            float _Hardness;
            float _PencilShape;
            float2 _CanvasSize;
            float2 _PatternCenter;
            float2 _WrapBasisU;
            float2 _WrapBasisV;
            float3 _SourceToDocumentX;
            float3 _SourceToDocumentY;
            float _BrushSize;

            float4 SdfTipGradient(float value)
            {
                float u = saturate(value) * (1.0 - _BrushSdfGradient_TexelSize.x) + .5 * _BrushSdfGradient_TexelSize.x;
                return tex2D(_BrushSdfGradient, float2(u, .5));
            }

            float PencilDistance(float2 delta)
            {
                return _PencilShape > 2.5 ? abs(delta.x) + abs(delta.y) : max(abs(delta.x), abs(delta.y));
            }

            float2 NearestPolygonDelta(float2 delta, float row)
            {
                float2 best = 0.0;
                float bestDistance = 3.4e38;
                [unroll]
                for (int i = -2; i <= 2; i++)
                {
                    float2 candidate = delta - (row + i) * _WrapBasisV;
                    [unroll]
                    for (int axis = 0; axis < 3; axis++)
                    {
                        float numerator = dot(candidate, _WrapBasisU);
                        float denominator = dot(_WrapBasisU, _WrapBasisU);
                        if (axis > 0)
                        {
                            if (_PencilShape > 2.5)
                            {
                                numerator = axis == 1 ? candidate.x : candidate.y;
                                denominator = axis == 1 ? _WrapBasisU.x : _WrapBasisU.y;
                            }
                            else
                            {
                                numerator = axis == 1 ? candidate.x + candidate.y : candidate.x - candidate.y;
                                denominator = axis == 1 ? _WrapBasisU.x + _WrapBasisU.y : _WrapBasisU.x - _WrapBasisU.y;
                            }
                        }
                        if (abs(denominator) < 0.000001) continue;
                        float column = floor(numerator / denominator);
                        [unroll]
                        for (int side = 0; side <= 1; side++)
                        {
                            float2 value = candidate - (column + side) * _WrapBasisU;
                            float distance = PencilDistance(value);
                            if (distance < bestDistance) { best = value; bestDistance = distance; }
                        }
                    }
                }
                return best;
            }

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
                #if defined(BRUSH_DYNAMICS) || defined(BRUSH_TEXTURE)
                output.color = float4(input.color, input.size.y);
                output.shape = float4(input.size.x, cos(input.size.z), sin(input.size.z), input.size.w);
                #endif
                return output;
            }

            float2 NearestPeriodicDelta(float2 delta)
            {
                float cross = _WrapBasisU.x * _WrapBasisV.y - _WrapBasisU.y * _WrapBasisV.x;
                float row = floor((_WrapBasisU.x * delta.y - _WrapBasisU.y * delta.x) / cross + 0.5);
                if (_PencilShape > 1.5) return NearestPolygonDelta(delta, row);
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
                float4 color = _Color;
                float brushSize = _BrushSize;
                #if defined(BRUSH_DYNAMICS) || defined(BRUSH_TEXTURE)
                color = input.color;
                brushSize = input.shape.x;
                #endif
                bool tiled = input.tileData.z > 0.5;
                float2 clipUv = input.canvasUv;
                float2 brushDelta = (input.brushUv - 0.5) * 2.0;
                float extent = 1.0;
                #if defined(BRUSH_TEXTURE)
                extent = abs(input.shape.y) + abs(input.shape.z);
                brushDelta *= extent;
                #endif
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
                        float2 ownership = nearest - (input.brushUv - 0.5) * brushSize * extent;
                        if (dot(ownership, ownership) > 0.0001) discard;
                    }
                    clipUv = input.tileData.xy + nearest / _CanvasSize;
                    brushDelta = nearest * (2.0 / brushSize);
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
                float coverage;
                #if defined(BRUSH_TEXTURE)
                brushDelta = float2(input.shape.y * brushDelta.x + input.shape.z * brushDelta.y,
                    -input.shape.z * brushDelta.x + input.shape.y * brushDelta.y);
                float flipY = step(1.5, input.shape.w);
                float flipX = input.shape.w - 2.0 * flipY;
                brushDelta *= 1.0 - 2.0 * float2(flipX, flipY);
                float2 tipUv = brushDelta / _TipAspect * 0.5 + 0.5;
                if (any(tipUv < 0.0) || any(tipUv > 1.0)) discard;
                tipUv = clamp(tipUv, _BrushTip_TexelSize.xy * .5, 1.0 - _BrushTip_TexelSize.xy * .5);
                float4 tip = tex2D(_BrushTip, tipUv);
                float tipValue = tip.a;
                float tipOpacity = 1.0;
                if (_TipChannel > 0.5 && _TipChannel < 2.5)
                {
                    float3 maskColor = _TipEncodeMask > .5 ? SpriteEncode(tip.rgb) : tip.rgb;
                    float value = saturate(dot(maskColor, float3(.2126, .7152, .0722)));
                    tipValue = _TipChannel > 1.5 ? 1.0 - value : value;
                    tipOpacity = tip.a;
                }
                coverage = tipValue * tipOpacity;
                if (_TipSdf > .5)
                {
                    float4 gradient = SdfTipGradient(1.0 - tipValue);
                    coverage = gradient.a * tipOpacity;
                    color.rgb *= gradient.rgb / max(gradient.a, .00001);
                }
                if (_TipChannel > 2.5)
                {
                    color.rgb *= _TipDecode > .5 ? SpriteDecode(tip.rgb) : tip.rgb;
                }
                color.rgb = _TipStandard > .5 ? saturate(color.rgb) : clamp(color.rgb, -65504.0, 65504.0);
                #else
                if (_PencilShape > 0.5)
                {
                    if (_PencilShape > 2.5) radius = abs(brushDelta.x) + abs(brushDelta.y);
                    else if (_PencilShape > 1.5) radius = max(abs(brushDelta.x), abs(brushDelta.y));
                    if (radius > 1.00001) discard;
                    coverage = 1.0;
                }
                else
                {
                    if (radius > 1.0) discard;
                    if (_TipSdf > .5)
                    {
                        float4 gradient = SdfTipGradient(radius);
                        coverage = gradient.a;
                        color.rgb *= gradient.rgb / max(gradient.a, .00001);
                        color.rgb = _TipStandard > .5 ? saturate(color.rgb) : clamp(color.rgb, -65504.0, 65504.0);
                    }
                    else
                    {
                        float inner = min(saturate(_Hardness), 0.9999);
                        coverage = 1.0 - smoothstep(inner, 1.0, radius);
                    }
                }
                #endif
                if (_UseSelection > 0.5)
                {
                    float3 source = float3(input.canvasUv, 1.0);
                    float2 uv = float2(dot(source, _SelectionToDocumentX), dot(source, _SelectionToDocumentY));
                    if (_SelectionWrap > 0.5) uv = frac(uv);
                    else if (any(uv < 0.0) || any(uv >= 1.0)) discard;
                    coverage *= tex2D(_SelectionMask, uv).r;
                }
                float alpha = saturate(color.a * coverage);
                if (alpha <= 0.0) discard;
                if (_PrepareStandard > 0.5)
                {
                    float4 before = tex2D(_Backdrop, input.canvasUv);
                    return float4(clamp(before.rgb, 0.0, before.a), before.a);
                }
                float4 stamp = float4(color.rgb * alpha, alpha);
                if (_StampBlendEnabled > .5)
                    return CompositeBrushPixel(tex2D(_Backdrop, input.canvasUv), stamp, 1.0, _StampBlendMode, 0.0, _TipStandard);
                return stamp;
            }
            ENDCG
        }
    }
}
