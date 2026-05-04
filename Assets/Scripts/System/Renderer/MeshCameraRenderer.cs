using UnityEngine;
using UnityEngine.Profiling;

namespace PBH
{
    /// <summary>
    /// Cameraコンポーネントを使用して、メッシュをRenderTextureに描画するクラス
    /// （CommandBuffer方式とのパフォーマンス比較・計測用）
    /// </summary>
    public class MeshCameraRenderer
    {
        private Mesh[] meshes;
        private Material[] materials;

        private Camera renderCamera;
        private GameObject cameraObj;

        // メインカメラや他のカメラに映り込まないようにするための専用レイヤー（31番）
        private const int RENDER_LAYER = 31;

        public MeshCameraRenderer(Mesh[] meshArray, Material[] materialArray)
        {
            meshes = meshArray;
            materials = materialArray;

            // --- Camera用GameObjectの初期化 ---
            cameraObj = new GameObject("SimulationRenderCamera");
            cameraObj.hideFlags = HideFlags.HideAndDontSave; // ヒエラルキーを汚さないように隠す

            renderCamera = cameraObj.AddComponent<Camera>();
            renderCamera.enabled = false; // 自動描画を無効化し、Render()で手動実行する
            renderCamera.orthographic = true;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.nearClipPlane = -100f;
            renderCamera.farClipPlane = 100f;
            renderCamera.cullingMask = 1 << RENDER_LAYER; // 指定したレイヤーのみ描画するように制限
        }

        public void DrawMesh(ref RenderTexture rt, Vector2 pos, float rotationZ, float camSize)
        {
            // 1. ターゲットテクスチャとサイズの設定
            renderCamera.targetTexture = rt;
            renderCamera.orthographicSize = camSize;

            // 2. カメラのTransform（位置と回転）を更新
            cameraObj.transform.position = new Vector3(pos.x, pos.y, -1f);
            cameraObj.transform.rotation = Quaternion.Euler(0, 0, rotationZ);

            Profiler.BeginSample("PBD_Camera_Render");

            // 3. メッシュを描画キューに登録
            for (int i = 0; i < meshes.Length; i++)
            {
                // Graphics.DrawMeshを使って、このカメラ専用のレイヤーにメッシュを投げる
                Graphics.DrawMesh(meshes[i], Matrix4x4.identity, materials[i], RENDER_LAYER, renderCamera);
            }

            // 4. 手動で描画を実行！
            renderCamera.Render();
            Profiler.EndSample();
        }

        public void Release()
        {
            // カメラオブジェクトを破棄してメモリリークを防ぐ
            if (cameraObj != null)
            {
                Object.Destroy(cameraObj);
                cameraObj = null;
            }
        }
    }
}