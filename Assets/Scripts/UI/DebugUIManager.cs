using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PBH;

/// <summary>
/// デバッグ情報を表示するUIパネルをまとめる。各ボタンが押された時の処理もここに記述しておく
/// </summary>
public class DebugUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject debugPanel;

    // パネル開閉ボタンのテキスト
    public TextMeshProUGUI toggleButtonText;

    [Header("Managers")]
    public SimulationDebugger simulationDebugger;
    public HighlightInput highlightInput;
    public ActorManager actorManager;

    [Header("UI Components (Debug View)")]
    public TextMeshProUGUI debugTargetText;
    public TextMeshProUGUI fpsText;
    public RawImage simulationRawImage;

    [Header("Sliders")]
    public Slider responseHSlider;
    public TextMeshProUGUI responseHValueText;
    public Slider responseVSlider;
    public TextMeshProUGUI responseVValueText;
    public Slider stiffnessSlider;
    public TextMeshProUGUI stiffnessText;

    private bool initDebugTargetText = false;

    private void Start()
    {
        // 起動時に現在のパネルの状態に合わせてテキストを初期化しておく
        UpdateToggleButtonText();

        // スライダーの初期値を現在のスロットに同期させる
        SyncSlidersWithCurrentSlot();
    }

    private void Update()
    {
        if (!initDebugTargetText)
        {
            debugTargetText.text = simulationDebugger.GetSlotInfoText();
            initDebugTargetText = true;
        }

        // パネルが表示されている時だけUIを更新する
        if (debugPanel != null && debugPanel.activeSelf)
        {
            WriteFPS();
            DrawSimulationImage();
        }
    }

    /// <summary>
    /// パネルの表示・非表示
    /// </summary>
    public void ToggleDebugPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeSelf);

            UpdateToggleButtonText();
        }
    }

    /// <summary>
    /// 状態に応じてボタンのテキストを書き換える
    /// </summary>
    private void UpdateToggleButtonText()
    {
        if (toggleButtonText != null && debugPanel != null)
        {
            if (debugPanel.activeSelf)
            {
                toggleButtonText.text = "Control & Infomation >";
            }
            else
            {
                toggleButtonText.text = "Control & Infomation <";
            }
        }
    }

    // ボタンからActorManagerやDebuggerのメソッドを直接呼び出すようにすると、ActorManager側を変更した時に毎回参照を設定し直す必要があり面倒なので、
    // UIManaagerからそれぞれのメソッドを呼び出すようにする
    /// <summary>
    /// アニメーションの再生・停止
    /// </summary>
    public void ToggleAnimationEnabled(bool isEnabled)
    {
        if (actorManager != null)
        {
            actorManager.SetAnimationEnabled(isEnabled);
        }
    }

    /// <summary>
    /// 顔の追従の切り替え
    /// </summary>
    public void ToggleTrackingEnabled(bool isEnabled)
    {
        if (actorManager != null)
        {
            actorManager.SetTrackingEnabled(isEnabled);
        }
    }

    /// <summary>
    /// デバッグ対象のインデックスを１つ戻す
    /// </summary>
    public void PrevIndex()
    {
        if (simulationDebugger != null)
        {
            simulationDebugger.PrevSlot();
            debugTargetText.text = simulationDebugger.GetSlotInfoText();
            SyncSlidersWithCurrentSlot();
        }
    }

    /// <summary>
    /// デバッグ対象のインデックスを１つ進める
    /// </summary>
    public void NextIndex()
    {
        if (simulationDebugger != null)
        {
            simulationDebugger.NextSlot();
            debugTargetText.text = simulationDebugger.GetSlotInfoText();
            SyncSlidersWithCurrentSlot();
        }
    }

    /// <summary>
    /// Debuggerから情報を取得してUIのTextで表示する
    /// </summary>
    private void WriteFPS()
    {
        if (simulationDebugger != null && fpsText != null)
        {
            fpsText.text = simulationDebugger.GetFpsText();
        }
    }

    /// <summary>
    /// Debuggerから情報を取得して、シミュレーションの状態を描画したRenderTextureをUIのRawImageで表示する
    /// </summary>
    private void DrawSimulationImage()
    {
        if (simulationDebugger != null && simulationRawImage != null)
        {
            RenderTexture targetRT = simulationDebugger.DebugTexture;

            // 毎回代入すると無駄な処理になるので、Textureが設定されていない（または変わった）時だけ代入する
            if (simulationRawImage.texture != targetRT)
            {
                simulationRawImage.texture = targetRT;
            }
        }
    }

    /// <summary>
    /// 現在のスロットの値を読み取り、UIスライダーの位置を合わせる
    /// </summary>
    private void SyncSlidersWithCurrentSlot()
    {
        if (highlightInput == null || simulationDebugger == null) return;

        int index = simulationDebugger.CurrentSlotIndex;

        if (responseHSlider != null)
        {
            // プログラムからvalueを変えるとOnValueChangedが呼ばれるため、テキストも自動で更新される
            responseHSlider.value = highlightInput.GetResponseH(index);
        }

        if (responseVSlider != null)
        {
            responseVSlider.value = highlightInput.GetResponseV(index);
        }
    }

    /// <summary>
    /// UIのResponseHスライダーが動かされた時に呼ばれる
    /// </summary>
    public void OnResponseHSliderChanged(float value)
    {
        if (highlightInput != null && simulationDebugger != null)
        {
            // 現在のスロットに値を書き込む
            highlightInput.SetResponseH(simulationDebugger.CurrentSlotIndex, value);
            // 数値テキストを更新 (小数点第2位まで)
            if (responseHValueText != null) responseHValueText.text = value.ToString("F2");
        }
    }

    /// <summary>
    /// UIのResponseVスライダーが動かされた時に呼ばれる
    /// </summary>
    public void OnResponseVSliderChanged(float value)
    {
        if (highlightInput != null && simulationDebugger != null)
        {
            highlightInput.SetResponseV(simulationDebugger.CurrentSlotIndex, value);
            if (responseVValueText != null) responseVValueText.text = value.ToString("F2");
        }
    }

    public void OnStiffnessSliderChanged(float value)
    {
        if (highlightInput != null)
        {
            // highlightInputに値を渡してパラメータを変更してもらう
            highlightInput.SetStiffness(value);
            // 表示中のテキストを更新する
            if (stiffnessText != null) stiffnessText.text = value.ToString("F2");
        }
    }
}