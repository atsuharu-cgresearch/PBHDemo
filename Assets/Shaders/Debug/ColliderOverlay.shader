Shader "MyShader/ColliderOverlay"
{
    Properties
    {
        _MainTex ("SDF Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 0.3) 
        
        // Transform2Dのデータ
        _Pos ("Position Offset", Vector) = (0, 0, 0, 0)
        _Rot ("Rotation (Radians)", Float) = 0
        _Scale ("Scale", Float) = 1.0
    }
    SubShader
    {
        // 軸や背景の上に重ねて描画するため、透過（Transparent）設定
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always

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
            float4 _Color;
            float4 _Pos;
            float _Rot;
            float _Scale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // フルスクリーンのUV (0~1)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                uv -= _Pos.xy;
                uv -= 0.5;
                
                // スケール
                uv /= max(_Scale, 1e-6);

                // 回転
                float s = sin(-_Rot);
                float c = cos(-_Rot);
                float2x2 rotMat = float2x2(c, -s, s, c);
                uv = mul(rotMat, uv);

                uv += 0.5;

                // ------------------------------------
                // UVが 0~1 の範囲外に出た場合は透明にする場合
                /*if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return fixed4(0, 0, 0, 0);
                }*/
                // 端の色を延長する場合
                uv = clamp(uv, 0.0, 1.0);
                // ------------------------------------

                // 計算したUVを使って、SDFテクスチャをサンプリング
                fixed4 col = tex2D(_MainTex, uv);
                
                return col * _Color;
                // return fixed4(_Color.rgb, col.r * _Color.a);
            }
            ENDCG
        }
    }
}