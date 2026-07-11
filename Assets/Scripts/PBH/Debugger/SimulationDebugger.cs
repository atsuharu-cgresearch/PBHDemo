using UnityEngine;
using PBH;

/// <summary>
/// デバッグ関連のサブモジュールを統括するマネージャークラス。
/// UIへの表示は行わず、データの管理と各モジュールへのデータ受け渡しを担当する。
/// </summary>
public class SimulationDebugger : MonoBehaviour
{
    public GameObject debugPanel;

    public HighlightSystem targetSystem;

    // デバッグ用のクラス
    public SimulationRenderer simulationRenderer;
    public SimulationGizmoDrawer gizmoDrawer;
    public FPSCounter fpsCounter;

    public OrbitCamera orbitCamera;

    private int currentSlotIndex = 0;

    private bool wasPanelActive = false;

    // UIが取得するためのプロパティ
    public RenderTexture DebugTexture => simulationRenderer != null ? simulationRenderer.OutputTexture : null;
    public int CurrentSlotIndex => currentSlotIndex;

    private void Update()
    {
        if (targetSystem == null) return;

        // 現在のパネルの開閉状態を取得
        bool isPanelActive = debugPanel != null && debugPanel.activeSelf;

        // パネルが開いている時の処理
        if (isPanelActive)
        {
            // パネルが開いている状態を記録
            wasPanelActive = true;

            // Rendererへのデータ受け渡し
            if (simulationRenderer != null)
            {
                if (targetSystem.TryGetDebugData(currentSlotIndex, out var buffer, out var ranges, out var targetTransforms, out var colliderTransform, out var colliderTexture))
                {
                    simulationRenderer.RenderDebugView(buffer, ranges, targetTransforms, colliderTransform, colliderTexture);
                }
            }

            // GizmoとCameraへのデータ受け渡し
            if (gizmoDrawer != null)
            {
                if (targetSystem.TryGetTargetData(currentSlotIndex, out var pos, out var rot))
                {
                    gizmoDrawer.SetGizmoData(pos, rot);
                    if (orbitCamera != null) orbitCamera.SetOverrideFocus(pos);
                }
                else
                {
                    gizmoDrawer.ClearData();
                    if (orbitCamera != null) orbitCamera.ClearOverrideFocus();
                }
            }
        }

        // パネルが閉じている時の処理
        else
        {
            // 前のフレームでパネルが開いていれば
            if (wasPanelActive)
            {
                // ギズモの描画をオフにする
                if (gizmoDrawer != null) gizmoDrawer.ClearData();

                // カメラのピボットの上書きを解除し、元のターゲットに戻す
                if (orbitCamera != null) orbitCamera.ClearOverrideFocus();

                // リセット完了を記録し、次回以降このブロックを通らないようにする
                wasPanelActive = false;
            }
        }
        
    }

    // UIボタンなどから呼ばれる操作メソッド
    public void NextSlot()
    {
        if (targetSystem == null) return;
        int maxSlots = targetSystem.GetSlotCount();
        if (maxSlots > 0)
        {
            currentSlotIndex = (currentSlotIndex + 1) % maxSlots;
            Debug.Log($"デバッグ対象スロットを [{currentSlotIndex}] に変更しました");
        }
    }

    public void PrevSlot()
    {
        if (targetSystem == null) return;
        int maxSlots = targetSystem.GetSlotCount();
        if (maxSlots > 0)
        {
            currentSlotIndex--;
            if (currentSlotIndex < 0) currentSlotIndex = maxSlots - 1;
            Debug.Log($"デバッグ対象スロットを [{currentSlotIndex}] に変更しました");
        }
    }

    // UIが表示用に情報を引き出すためのメソッド
    /// <summary>
    /// 現在のスロット情報を文字列として返す
    /// </summary>
    public string GetSlotInfoText()
    {
        if (targetSystem == null) return "No System";

        int maxSlots = targetSystem.GetSlotCount();
        TargetMesh currentMesh = targetSystem.GetTargetMesh(currentSlotIndex);
        string meshName = (currentMesh != null) ? currentMesh.gameObject.name : "Unknown";

        // return $"{currentSlotIndex + 1} / {maxSlots}\n({meshName})";
        return $"{currentSlotIndex + 1} / {maxSlots}";
    }

    /// <summary>
    /// FPSのテキスト情報を返す
    /// </summary>
    public string GetFpsText()
    {
        return fpsCounter != null ? fpsCounter.FpsText : "";
    }
}