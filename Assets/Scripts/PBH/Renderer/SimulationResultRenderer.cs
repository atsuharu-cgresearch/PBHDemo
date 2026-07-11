using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    public class SimulationResultRenderer
    {
        // ハイライトを描画するターゲットのオブジェクトを管理しているクラス
        private TargetMesh target;
        // シミュレーション結果をテクスチャに描画するクラス
        private ParticleRenderer particleRenderer;
        // 結果を描画するテクスチャ
        private RenderTexture resultTexture;

        public SimulationResultRenderer(InputSlot slot, ComputeBuffer particles, ParticleRange[] references, int textureSize)
        {
            target = slot.target;

            // 描画するパーティクルの初期配置を取得しておく
            // SimulationObjectDefinitionに含まれる三角面を構成するパーティクルインデックスを使って、メッシュと同様に描画する
            SimulationObjectDefinition[] defs = new SimulationObjectDefinition[slot.elements.Count];
            for (int i = 0; i < slot.elements.Count; i++)
            {
                defs[i] = SimulationObjectDatabase.Load(slot.elements[i].type);
            }

            // 描画実行クラスを初期化
            particleRenderer = new ParticleRenderer(defs, particles, references);

            // RenderTextureを初期化
            HelperFunction.CreateCameraTargetFloat4RT(ref resultTexture, textureSize);
        }

        public void Render(Transform2D textureTransform)
        {
            // RenderTextureにシミュレーション結果を描画
            particleRenderer.Render(ref resultTexture, textureTransform);

            // ターゲットのオブジェクトに渡す
            target.RenderResult(resultTexture);
        }

        public void Release()
        {
            particleRenderer.Release();
        }
    }
}
