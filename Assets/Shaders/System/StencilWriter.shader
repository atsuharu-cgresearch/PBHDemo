Shader "MyShader/StencilWriter"
{
    SubShader
    {
        // 髪の毛(2000)より後、かつ顔(2450)より前に実行されるように設定
        Tags { 
            "Queue"="Geometry+400" 
            "RenderType"="Opaque" 
        }

        // 色は一切塗らず、深度（Zバッファ）も更新しない
        ColorMask 0
        ZWrite Off
        Cull Off
        
        // ★重要：手前に髪の毛があっても、強制的に白目の形の「1」を書き込む
        ZTest Always

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return 0;
            }
            ENDCG
        }
    }
}