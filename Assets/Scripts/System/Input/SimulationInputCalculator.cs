using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    public class SimulationInputCalculator
    {
        private InputSlot slot;

        // Simulatorの何番目に登録しているか
        public int[] objKeys;
        // 
        public int layerKey;
        // コライダーとコライダーのオフセット
        public RenderTexture colliderTexture;
        public Transform2D textureTransform;
        public Transform2D[] targetTransforms;

        // 入力のOccluderの有無によって使用するクラスを変更するため、インターフェースを使う
        private IUVMapGenerator uvMapGenerator;

        private SDFCalculator sdfCalculator;

        private DirectionalOffsetCalculator camDirCalculator;
        private DirectionalOffsetCalculator lightDirCalculator;

        public SimulationInputCalculator(InputSlot slot, int texSize, int index)
        {
            this.slot = slot;

            // このスロットが使用する、SimulatorのレイヤーID
            layerKey = index;

            // Occluderが設定されている場合は、マスク処理が必要なのでUVMaskGeneratorクラスを使用
            if (slot.occluder != null)
            {
                uvMapGenerator = new UVMaskGenerator(slot.target, slot.occluder, texSize);
            }
            // そうでない場合はUVマップを使用するので、UVMapGeneratorクラスを使用
            else
            {
                uvMapGenerator = new UVMapGenerator(slot.target, texSize);
            }

            sdfCalculator = new SDFCalculator(texSize);
            camDirCalculator = new DirectionalOffsetCalculator(slot.target, true);
            lightDirCalculator = new DirectionalOffsetCalculator(slot.target, false);

            targetTransforms = new Transform2D[slot.elements.Count];
        }

        /// <summary>
        /// シミュレータにオブジェクトを登録し、キーを取得する
        /// </summary>
        public void RegisterForPhysics(BodyCreator bodyCreator)
        {
            objKeys = new int[slot.elements.Count];

            for (int i = 0; i < slot.elements.Count; i++)
            {
                Vector4 initTransform = new Vector4(
                    slot.elements[i].transform.pos.x,
                    slot.elements[i].transform.pos.y,
                    slot.elements[i].transform.scale,
                    slot.elements[i].transform.rot
                    );

                bodyCreator.AddElement(
                    SimulationObjectDatabase.Load(slot.elements[i].type),
                    initTransform,
                    layerKey,
                    out objKeys[i]
                    );
            }
        }

        /// <summary>
        /// 現在のカメラ・ライト・メッシュの状態で、ハイライトが基準の状態になるようにリセットする
        /// </summary>
        public void Reset(Vector3 camPos, Vector3 lightPos)
        {
            camDirCalculator.Reset(camPos);
            lightDirCalculator.Reset(lightPos);
        }

        public void UpdatePhysicsInputs(ExternalDataPool dataPool, Vector3 camPos, Quaternion camRot, Vector3 lightPos)
        {
            // UVマップを生成
            RenderTexture uvMap = uvMapGenerator.Generate(camPos);

            // UVマップからSDFを生成
            RenderTexture sdf = sdfCalculator.Calculate(uvMap);
            // レンダリングやデバッグ時に必要なので保持しておく
            colliderTexture = sdf;

            // 実際の変化の大きさにどれくらい従うか
            // 0: ハイライトが全く変化しない、1: 実際の鏡面反射と同じ
            float responseV = slot.responseV;
            float responseH = slot.responseH;

            // 実際の変化を計算
            Vector2 camOffset = camDirCalculator.CalcOffset(camPos);
            Vector2 lightOffset = lightDirCalculator.CalcOffset(lightPos);

            Vector2 colliderOffset = new Vector2(responseH * camOffset.x, responseV * camOffset.y);
            Vector2 targetOffset = new Vector2(responseH * lightOffset.x, responseV * lightOffset.y);

            // curvatureは疑似的なメッシュの曲率
            // 値を大きくすると、反射する領域が広くなるので、相対的にハイライトの大きさが小さくなる
            float curvature = slot.curvature;

            // 衝突判定用のテクスチャをSimulatorにセット
            Transform2D colliderTransform = new Transform2D(colliderOffset, 0f, 1.0f / curvature);
            dataPool.SetCollider(sdf, colliderTransform, layerKey);
            // レンダリングやデバッグ時に必要なので保持しておく
            textureTransform = colliderTransform;

            // 目標位置をシミュレーターに登録
            for (int i = 0; i < objKeys.Length; i++)
            {
                // 初期状態のオフセット
                Transform2D initOffset = slot.elements[i].transform;
                // 反転モードならtargetOffsetに-1をかける
                Vector2 targetOffsetInverse = (slot.elements[i].inverse) ? -targetOffset : targetOffset;
                // オブジェクトごとの目標位置を計算してSimulatorにセット
                Transform2D targetPosTransform = new Transform2D(initOffset.pos + targetOffsetInverse, initOffset.rot, initOffset.scale);
                dataPool.SetTargetPosOffset(targetPosTransform, objKeys[i]);
                // レンダリングやデバッグ時に必要なので保持しておく
                targetTransforms[i] = targetPosTransform;
            }
        }

        public void Release()
        {
            uvMapGenerator.Release();
            sdfCalculator.Release();
        }
    }
}
