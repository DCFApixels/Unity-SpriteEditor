Shader "Hidden/TextureCompositor/Transform"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        //_Transform ("Transform Matrix", float) = (1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)
    }
    SubShader
    {
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4x4 _Transform;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 transformed = mul(_Transform, float4(uv, 0, 1));
                uv = transformed.xy;
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}