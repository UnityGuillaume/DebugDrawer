Shader "Unlit/DebugDrawerPixelScreenspace"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Overlay"}
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile PIXEL_COORD __

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 vcol : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vcol : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                
                
                //set depth to a bit in front of near plane
                o.vertex.z = 0.1f;
                #if PIXEL_COORD
                o.vertex.x = -1.0f + v.vertex.x/_ScreenParams.x * 2.0f;
                o.vertex.y = -1.0f + v.vertex.y/_ScreenParams.y * 2.0f;
                #else
                o.vertex.x = -1.0f + v.vertex.x * 2.0f;
                o.vertex.y = -1.0f + v.vertex.y * 2.0f;
                #endif
                
                o.vertex.w = 1.0f;
                
                o.vcol = v.vcol;
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 alpha = tex2D(_MainTex, i.uv);
                fixed4 col = i.vcol;
                col.a = alpha.a;
                return col;
            }
            ENDCG
        }
    }
}
