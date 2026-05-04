using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    public class SimulationResultRenderer
    {
        // 最終的なレンダリングのターゲットを管理しているクラス
        private TargetMesh target;

        // シミュレーション後のParticleBufferを入力し、RenderTextureに結果を描画するクラス
        private ParticleRenderer particleRenderer;

        // 描画結果
        private RenderTexture resultTexture;

        public SimulationResultRenderer()
        {

        }

        public void InitRenderer(InputSlot slot, ComputeBuffer particles, ParticleRange[] references, int textureSize)
        {
            target = slot.target;

            // パーティクルのバッファのうち、参照する範囲を

            SimulationObjectDefinition[] defs = new SimulationObjectDefinition[slot.elements.Count];
            for (int i = 0; i < slot.elements.Count; i++)
            {
                defs[i] = SimulationObjectDatabase.Load(slot.elements[i].type);
            }

            // 描画実行クラスを初期化
            particleRenderer = new ParticleRenderer(defs, particles, references);

            // 結果を描画するRenderTextureを初期化
            HelperFunction.CreateCameraTargetFloat4RT(ref resultTexture, textureSize);
        }

        public void Render(Transform2D textureTransform)
        {
            // RenderTextureにシミュレーション結果を描画
            particleRenderer.Render(ref resultTexture, textureTransform);

            // ターゲットのメッシュにRenderTextureを渡す
            target.RenderResult(resultTexture);
        }

        public void Release()
        {
            particleRenderer.Release();
        }
    }
}
