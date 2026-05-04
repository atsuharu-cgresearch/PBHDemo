Shader "MyShader/HighlightTop"
{
    Properties {
        _MainTex ("Texture", 2D) = "clear" {}
        _Color ("Normal Color", Color) = (1, 1, 1, 1)
        _ThroughColor ("Through Color", Color) = (0.5, 0.5, 1, 0.5)
    }

    SubShader {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        ZWrite Off

        // ZTest Always  // 深度テストを無視

        Stencil {
            Ref 1
            Comp Equal
        }

        // ==========================================
        // パス1：髪の毛の「奥」にある時（透かし用）
        // ==========================================
        Pass {
            ZTest Greater 
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            
            sampler2D _MainTex;
            float4 _ThroughColor; // 髪越し専用カラー

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 髪越し用の色をテクスチャに乗算
                fixed4 col = tex2D(_MainTex, i.uv) * _ThroughColor;
                return col;
                // return fixed4(1,0,0,1);
            }
            ENDCG
        }

        // ==========================================
        // パス2：手前にある時（通常用）
        // ==========================================
        Pass {
            ZTest LEqual 
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            
            sampler2D _MainTex;
            float4 _Color; // 通常時専用カラー

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 通常時の色をテクスチャに乗算
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
                // return fixed4(1,0,0,1);
            }
            ENDCG
        }
    }
}