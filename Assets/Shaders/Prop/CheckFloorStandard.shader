Shader "MyShader/CheckerFloorStandard"
{
    Properties
    {
        // インスペクターで設定できるようにプロパティを定義
        _Color1 ("Color 1", Color) = (1, 1, 1, 1)      // 1色目（デフォルト白）
        _Color2 ("Color 2", Color) = (0.1, 0.1, 0.1, 1) // 2色目（デフォルト黒/ダークグレー）
        _Scale ("Checker Scale", Float) = 2.0          // チェックの細かさ
        _Glossiness ("Smoothness", Range(0,1)) = 0.5   // 表面の滑らかさ
        _Metallic ("Metallic", Range(0,1)) = 0.0       // 金属感
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // スタンダードライティングモデルを使用し、影をフルサポート
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            // 床用にワールド座標を取得（オブジェクトが変わっても模様が繋がるようにするため）
            float3 worldPos;
            
            // （もしUV座標を使いたい場合はこちらを有効化します）
            // float2 uv_MainTex; 
        };

        // Propertiesで定義した変数をCGプログラム側で受け取る
        fixed4 _Color1;
        fixed4 _Color2;
        float _Scale;
        half _Glossiness;
        half _Metallic;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // ワールド座標のXとZ（水平面）を取得し、スケールを掛ける
            float2 pos = IN.worldPos.xz * _Scale;
            
            // UVを使いたい場合は上の行をコメントアウトし、下の行のコメントを外します
            // float2 pos = IN.uv_MainTex * _Scale;

            // --- チェック模様の計算ロジック ---
            // 1. floorで切り捨ててマス目ごとの整数（グリッド）にする
            float2 grid = floor(pos);
            
            // 2. XとYの合計を2で割った余りを計算し、0か1のフラグを作る
            // (fracは小数部分を返す関数。0.5を掛けてfracし、2倍することで0または1に近づける)
            float checker = frac((grid.x + grid.y) * 0.5) * 2.0;
            
            // 3. 浮動小数点誤差を防ぐため、0.5を閾値に完全に0.0か1.0に分ける
            checker = step(0.5, checker);

            // 0か1のchecker値を使って、Color1とColor2をブレンド（切り替え）する
            fixed4 c = lerp(_Color1, _Color2, checker);

            // 最終的な色と、スタンダードシェーダーの質感を代入
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}