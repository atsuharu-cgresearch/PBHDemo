Shader "MyShader/PointShader"
{
    Properties
    {
        _ParticleColor ("Particle Color", Color) = (1, 1, 1, 1)
        _SimAreaCenter ("Simulation Center (XY)", Vector) = (0, 0, 0, 0)
        _SimAreaSize ("Simulation Area Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "Assets/Resources/hlsl/Struct_ParticleData.hlsl"

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            StructuredBuffer<ParticleData> _ParticleBuffer;
            float4 _ParticleColor;
            float2 _SimAreaCenter;
            float _SimAreaSize;

            // ★ ここにC#から受け取るオフセット用の変数を追加（Propertiesブロックへの記述は不要です）
            int _BufferOffset; 

            v2f vert (uint vertexID : SV_VertexID)
            {
                v2f o;

                // ★ 頂点IDにオフセットを足して、バッファの正しい位置から読み取る
                float2 particleWorldPos = _ParticleBuffer[vertexID + _BufferOffset].x;

                // --- 以下の計算は既存のまま ---
                float2 normalizedPos = (particleWorldPos - _SimAreaCenter) / _SimAreaSize; 
                float2 clipCenter = normalizedPos * 2.0;

                // o.pos = float4(clipCenter, 0.0, 1.0);
                // RenderTextureに描画する際、上下が反転するが、DrawProcedualNowを使って描画する場合、Unityが自動で反転してくれないので手動で反転する
                o.pos = float4(clipCenter.x, clipCenter.y * _ProjectionParams.x, 0.0, 1.0);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 単純に色を返すだけ（円形にくり抜く処理は不要です）
                return _ParticleColor;
            }
            ENDCG
        }
    }
}
