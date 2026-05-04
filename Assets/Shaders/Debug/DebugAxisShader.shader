Shader "MyShader/DebugAxisShader"
{
    Properties
    {
        // 軸の色
        _XAxisColor ("X Axis Color (Green)", Color) = (1,0,0,1)
        _YAxisColor ("Y Axis Color (Red)", Color) = (0,1,0,1)
        // 線の太さ
        _Thickness ("Line Thickness", Range(0, 0.01)) = 0.002
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            float4 _XAxisColor;
            float4 _YAxisColor;
            float _Thickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // UV座標をそのままフラグメントシェーダーに渡す
                o.uv = v.uv; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 背景色は透明（または黒）
                fixed4 col = fixed4(0, 0, 0, 1); 

                // --- ダイヤグラムで解説した軸の描画ロジック ---

                // X軸 (緑色の横線): UV.y が 0.5 のピクセル
                if (abs(i.uv.y - 0.5) < _Thickness)
                {
                    col = _XAxisColor;
                }

                // Y軸 (赤色の縦線): UV.x が 0.5 のピクセル
                if (abs(i.uv.x - 0.5) < _Thickness)
                {
                    // 重なった場合はY軸の色を優先
                    col = _YAxisColor; 
                }

                return col;
            }
            ENDCG
        }
    }
}