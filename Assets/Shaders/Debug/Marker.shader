Shader "MyShader/Marker"
{
    Properties
    {
        _MarkerColor ("Marker Color", Color) = (1, 0, 0, 1) // 赤色がおすすめ
        _MarkerSize ("Marker Size (World)", Float) = 0.1
        _LineThickness ("Line Thickness", Range(0.001, 0.5)) = 0.02
        
        // 座標変換用
        _SimAreaCenter ("Simulation Center (XY)", Vector) = (0, 0, 0, 0)
        _SimAreaSize ("Simulation Area Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5 // SV_VertexIDはWebGL2以降の機能なので、コンパイルターゲットを限定しておく
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _MarkerColor;
            float _MarkerSize;
            float _LineThickness;
            
            float2 _SimAreaCenter;
            float _SimAreaSize;

            // ★C#から毎フレーム受け取る目標位置
            float2 _TargetPosition; 

            v2f vert (uint vertexID : SV_VertexID)
            {
                v2f o;

                // 1. Quadの基準頂点（-0.5 〜 0.5）
                float2 quadVertices[6] = {
                    float2(-0.5, -0.5), float2(0.5, -0.5), float2(-0.5, 0.5),
                    float2(-0.5,  0.5), float2(0.5, -0.5), float2(0.5,  0.5)
                };
                float2 localPos = quadVertices[vertexID];

                // 2. 目標位置に移動し、サイズを掛ける
                float2 worldPos = _TargetPosition + (localPos * _MarkerSize);

                // 3. UI空間 (-1.0 ~ 1.0) に変換
                float2 normalizedPos = (worldPos - _SimAreaCenter) / _SimAreaSize; 
                float2 clipCenter = normalizedPos * 2.0;

                // o.pos = float4(clipCenter.x, -clipCenter.y, 0.0, 1.0);
                // RenderTextureに描画する際、上下が反転するが、DrawProcedualNowを使って描画する場合、Unityが自動で反転してくれないので手動で反転する
                o.pos = float4(clipCenter.x, clipCenter.y * _ProjectionParams.x, 0.0, 1.0);
                o.uv = localPos; // -0.5 〜 0.5 の範囲をそのままフラグメントに渡す
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // i.uv は中心が (0,0) で、端が -0.5 と 0.5

                // X軸方向の線（横線）: Y座標が太さの範囲内なら 1、それ以外は 0
                float lineX = step(abs(i.uv.y), _LineThickness);
                
                // Y軸方向の線（縦線）: X座標が太さの範囲内なら 1、それ以外は 0
                float lineY = step(abs(i.uv.x), _LineThickness);

                // 縦か横、どちらかに色が付いていれば十字になる
                float alpha = max(lineX, lineY);

                // 十字形以外の部分は描画をスキップ（透明にする）
                if (alpha < 0.5) discard;

                return _MarkerColor;
            }
            ENDCG
        }
    }
}