using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PBH;

/// <summary>
/// パーティクルの位置やコライダーの状態などを、RenderTextureに描画してデバッグするためのクラス
/// </summary>
public class SimulationRenderer : MonoBehaviour
{
    [Header("軸描画用")]
    public Shader axisShader;
    private Material axisMaterial;

    [Header("パーティクル描画用")]
    public Shader particleShader;
    private Material particleMaterial;

    [Header("目標位置描画用")]
    public Material targetMarkerMaterial;
    public Material colliderMarkerMaterial;

    [Header("SDF描画用")]
    public Shader sdfOverlayShader;
    private Material sdfOverlayMaterial;

    // UIが取得するためのRenderTexture
    public RenderTexture OutputTexture { get; private set; }

    private void Start()
    {
        if (axisShader != null) axisMaterial = new Material(axisShader);
        if (particleShader != null) particleMaterial = new Material(particleShader);
        if (sdfOverlayShader != null) sdfOverlayMaterial = new Material(sdfOverlayShader);

        // RenderTextureを生成
        OutputTexture = new RenderTexture(360, 360, 0);
        OutputTexture.Create();
    }

    private void OnDestroy()
    {
        if (OutputTexture != null) OutputTexture.Release();
    }

    /// <summary>
    /// Debuggerから毎フレーム呼ばれ、受け取ったデータを元に描画を行う
    /// </summary>
    public void RenderDebugView(ComputeBuffer buffer, ParticleRange[] ranges, Transform2D[] targetTransforms, Transform2D colliderTransform, RenderTexture colliderTexture)
    {
        if (OutputTexture == null) return;

        // 背景と軸の描画と画面のクリア
        if (axisMaterial != null)
        {
            Graphics.Blit(null, OutputTexture, axisMaterial);
        }

        // SDFの合成
        if (sdfOverlayMaterial != null && colliderTexture != null)
        {
            sdfOverlayMaterial.SetVector("_Pos", colliderTransform.pos);
            sdfOverlayMaterial.SetFloat("_Rot", colliderTransform.rot);
            sdfOverlayMaterial.SetFloat("_Scale", colliderTransform.scale);
            Graphics.Blit(colliderTexture, OutputTexture, sdfOverlayMaterial);
        }

        // パーティクルの描画
        if (particleMaterial != null && buffer != null)
        {
            RenderTexture.active = OutputTexture;
            particleMaterial.SetBuffer("_ParticleBuffer", buffer);

            // 1スロット内に複数のオブジェクトがある場合は、全て描画する
            foreach (var range in ranges)
            {
                // ParticleBufferの読み取りの開始インデックス
                particleMaterial.SetInt("_BufferOffset", range.start);
                // SetPassは描画の度に呼ぶ必要があるので注意
                particleMaterial.SetPass(0);
                // パーティクルをRenderTextureの1ピクセルとして描画
                Graphics.DrawProceduralNow(MeshTopology.Points, range.count);
            }
            RenderTexture.active = null;
        }

        // ターゲットマーカーの描画
        if (targetMarkerMaterial != null)
        {
            RenderTexture.active = OutputTexture;

            foreach (var target in targetTransforms)
            {
                targetMarkerMaterial.SetVector("_TargetPosition", target.pos);
                targetMarkerMaterial.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, 1);
            }
            
            RenderTexture.active = null;
        }

        // コライダーマーカーの描画
        if (colliderMarkerMaterial != null)
        {
            RenderTexture.active = OutputTexture;
            colliderMarkerMaterial.SetVector("_TargetPosition", colliderTransform.pos);
            colliderMarkerMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, 1);
            RenderTexture.active = null;
        }
    }
}
