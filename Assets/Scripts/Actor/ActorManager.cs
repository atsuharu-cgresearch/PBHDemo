using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRM;

/// <summary>
/// VRMにアタッチして、キャラクターやアニメーションの操作を管理する
/// </summary>
public class ActorManager : MonoBehaviour
{
    // アニメーションの再生・停止と顔のトラッキング
    private Animator animator;
    public Transform eyeTarget;
    [Range(0f, 1f)] public float eyeMoveWeight = 0.65f;
    [Range(0f, 1f)] public float headMoveWeight = 0.6f;
    [Range(0f, 1f)] public float bodyMoveWeight = 0.2f;
    // 最初は顔のトラッキングを停止しておく
    private bool isTrackingEnabled = false;

    // BlendShapeのパラメータを操作してまばたきさせる
    private VRMBlendShapeProxy blendShapeProxy;
    private bool isBlinking = false;
    [Range(0f, 1f)] public float blinkDuration = 0.3f;
    [Range(0f, 0.3f)] public float blinkKeepTime = 0.02f;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animatorコンポーネントを取得できませんでした。");
        }

        blendShapeProxy = GetComponent<VRMBlendShapeProxy>();
        if (blendShapeProxy == null)
        {
            Debug.LogWarning("VRMBlendShapeProxyコンポーネントを取得できませんでした。");
        }

        // 最初はアニメーションを停止しておく
        animator.speed = 0;
    }

    private void Update()
    {
        // マウスの左クリックでまばたきさせる
        if (Input.GetMouseButtonDown(0))
        {
            TriggerBlink();
        }
    }

    /// <summary>
    /// コルーチンを使ってまばたきを実行する
    /// </summary>
    private void TriggerBlink()
    {
        if (blendShapeProxy == null || isBlinking) return;

        StartCoroutine(BlinkRoutine());
    }

    /// <summary>
    /// まばたきのアニメーションを再生するコルーチン
    /// </summary>
    private IEnumerator BlinkRoutine()
    {
        isBlinking = true;

        var blinkKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink);

        float t = 0;

        // 目を閉じる
        while (t < blinkDuration)
        {
            t += Time.deltaTime;
            float value = Mathf.Clamp01(t / blinkDuration);
            // シェイプキーの値をメッシュに反映する
            blendShapeProxy.ImmediatelySetValue(blinkKey, value);
            // １フレーム待機
            yield return null;
        }

        // 目を閉じた状態
        yield return new WaitForSeconds(blinkKeepTime);

        // 目を開ける
        t = 0;
        while (t < blinkDuration)
        {
            t += Time.deltaTime;
            float value = 1.0f - Mathf.Clamp01(t / blinkDuration);
            blendShapeProxy.ImmediatelySetValue(blinkKey, value);
            yield return null;
        }

        // 念のためシェイプキーをリセット
        blendShapeProxy.ImmediatelySetValue(blinkKey, 0.0f);

        isBlinking = false;
    }

    // IK Passにチェックを入れると毎フレーム自動で呼ばれるメソッド
    private void OnAnimatorIK(int layerIndex)
    {
        if (eyeTarget != null)
        {
            // 追従モードオン
            if (isTrackingEnabled)
            {
                // ウェイトを設定（全体, 体, 頭, 目, clamp）
                animator.SetLookAtWeight(1.0f, bodyMoveWeight, headMoveWeight, eyeMoveWeight, 0.5f);

                // ターゲットの位置へ顔を向ける
                animator.SetLookAtPosition(eyeTarget.position);
            }
            // 追従モードオフ
            else
            {
                // ウェイトをすべて0にすることで、アニメーションに従うようになる
                animator.SetLookAtWeight(0f, 0f, 0f, 0f, 0f);
            }
        }
    }

    /// <summary>
    /// UIManager側からこれを呼び出して、アニメーションの再生・停止を行う
    /// </summary>
    public void SetAnimationEnabled(bool isEnabled)
    {
        if (animator != null)
        {
            // Animatorを無効化するとIKが使えなくなるので、再生スピードで操作する
            animator.speed = isEnabled ? 1f : 0f;
        }
    }

    public void SetTrackingEnabled(bool isEnabled)
    {
        isTrackingEnabled = isEnabled;
    }
}