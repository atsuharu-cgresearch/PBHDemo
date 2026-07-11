using UnityEngine;

namespace PBH
{
    /// <summary>
    /// 指定されたワールド座標・回転に対して、Gameビュー上に3Dギズモを描画する専用クラス
    /// GLクラスを使用し、常に1ピクセル幅の線として描画する
    /// </summary>
    public class SimulationGizmoDrawer : MonoBehaviour
    {
        public bool showGizmo = true;
        [Range(0.01f, 1.0f)] public float gizmoSize = 0.1f;

        private bool hasData = false;
        private Vector3 targetPos;
        private Quaternion targetRot;

        // GL描画用のマテリアル
        private Material lineMaterial;

        private void Awake()
        {
            // GL描画で色を付けるための、Unity内部のシンプルなシェーダーを取得
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;

            // 常に手前に描画
            lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        public void SetGizmoData(Vector3 worldPos, Quaternion worldRot)
        {
            targetPos = worldPos;
            targetRot = worldRot;
            hasData = true;
        }

        public void ClearData()
        {
            hasData = false;
        }

        
        private void OnRenderObject()
        {
            if (!showGizmo || !hasData) return;

            // 描画用のマテリアルをセット
            lineMaterial.SetPass(0);

            // 行列を保存し、線の描画を開始
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity); // ワールド座標系で描画
            GL.Begin(GL.LINES);

            // X軸（ローカルの右）: 赤色
            GL.Color(Color.red);
            GL.Vertex(targetPos);
            GL.Vertex(targetPos + targetRot * Vector3.right * gizmoSize);

            // Y軸（ローカルの上）: 緑色
            GL.Color(Color.green);
            GL.Vertex(targetPos);
            GL.Vertex(targetPos + targetRot * Vector3.up * gizmoSize);

            // Z軸（ローカルの法線）: 青色
            GL.Color(Color.blue);
            GL.Vertex(targetPos);
            GL.Vertex(targetPos + targetRot * Vector3.forward * gizmoSize);

            // 描画終了
            GL.End();
            GL.PopMatrix();
        }
    }
}