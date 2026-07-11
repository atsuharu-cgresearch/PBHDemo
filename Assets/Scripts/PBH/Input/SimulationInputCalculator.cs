using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        private IUVMaskGenerator uvMaskGenerator;

        private SDFCalculator sdfCalculator;

        private DirectionalOffsetCalculator reflDirOffsetCalculator;
        private DirectionalOffsetCalculator lightDirOffsetCalculator;

        public SimulationInputCalculator(InputSlot slot, int texSize, int index)
        {
            this.slot = slot;

            // このスロットが使用する、SimulatorのレイヤーID
            layerKey = index;

            // Occluderが設定されている場合は、マスク処理が必要なのでUVMaskGeneratorクラスを使用
            if (slot.occluder != null)
            {
                uvMaskGenerator = new UVMaskGenerator(slot.target, slot.occluder, texSize);
            }
            // そうでない場合はUVマップを使用するので、UVMapGeneratorクラスを使用
            else
            {
                uvMaskGenerator = new UVMapGenerator(slot.target, texSize);
            }

            sdfCalculator = new SDFCalculator(texSize);
            reflDirOffsetCalculator = new DirectionalOffsetCalculator(slot.target, true);
            lightDirOffsetCalculator = new DirectionalOffsetCalculator(slot.target, false);

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
            reflDirOffsetCalculator.Reset(camPos);

            lightDirOffsetCalculator.Reset(lightPos);
        }

        

        /// <summary>
        /// 現在の視点位置を入力し、衝突判定用テクスチャを計算する
        /// </summary>
        private RenderTexture UpdateColliderTexture(Vector3 camPos)
        {
            // PIXでGPU処理の実行時間を計測するための準備
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Input";

            // 目のメッシュのUVマップのうち、まぶたに隠れていない部分のみを抽出する
            cmd.BeginSample("Input_Gen_Mask");
            RenderTexture uvMask = uvMaskGenerator.Generate(camPos, cmd);
            cmd.EndSample("Input_Gen_Mask");

            // マスクから衝突判定用テクスチャを作成する
            cmd.BeginSample("Input_Calc_SDF");
            RenderTexture result = sdfCalculator.Calculate(uvMask, cmd);
            cmd.EndSample("Input_Calc_SDF");

            // 計測終了
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();

            return result;
        }

        private Transform2D CalcColliderTransform(float respH, float respV, float curv,　Vector3 camPos)
        {
            // 反射ベクトルの基準状態からの差分を取得
            Vector2 reflDirOffset = reflDirOffsetCalculator.CalcOffset(camPos);

            // 入力パラメータを加味して最終的なTransformを計算する
            return new Transform2D(
                new Vector2(respH * reflDirOffset.x, respV * reflDirOffset.y),
                0f,
                1.0f / curv
                );
        }

        private Transform2D CalcTargetTransform(float respH, float respV, Transform2D initTrans, Vector3 lightPos)
        {
            // 光源方向ベクトルの基準状態からの差分を取得
            Vector2 lightOffset = -1 * lightDirOffsetCalculator.CalcOffset(lightPos);

            return new Transform2D(
                initTrans.pos + new Vector2(respH * lightOffset.x, respV * lightOffset.y),
                initTrans.rot,
                initTrans.scale
                );
        }

        /// <summary>
        /// シミュレーターへ毎フレーム入力する必要があるデータを計算し、入力する
        /// </summary>
        public void UpdatePhysicsInputs(ExternalDataPool dataPool, Vector3 camPos, Quaternion camRot, Vector3 lightPos)
        {
            // 衝突判定用テクスチャの作成
            RenderTexture collTex = UpdateColliderTexture(camPos);

            // 鏡面反射にどれだけ従うかのパラメータ（0～1）
            float responseV = Mathf.Clamp01(slot.responseV);
            float responseH = Mathf.Clamp01(slot.responseH);
            // curvatureは疑似的なメッシュの曲率パラメータ
            // 値を大きくすると、反射する領域が広くなるので、相対的にハイライトの大きさが小さくなる
            float curvature = slot.curvature;

            // 衝突判定用テクスチャをシミュレーション空間に入力する際の2Dアフィン変換を計算
            Transform2D collTrans2D = CalcColliderTransform(responseH, responseV, curvature, camPos);

            // テクスチャとTransformをシミュレーターにセット
            dataPool.SetCollider(collTex, collTrans2D, layerKey);

            // レンダリングやデバッグ時に必要なので保持しておく
            colliderTexture = collTex;
            textureTransform = collTrans2D;

            // 目標位置をシミュレーターに登録
            for (int i = 0; i < objKeys.Length; i++)
            {
                // 初期状態のオフセットパラメータ
                Transform2D initTrans = slot.elements[i].transform;

                // 目標位置をシミュレーション空間に入力する際の2Dアフィン変換を計算
                Transform2D targetTrans2D = CalcTargetTransform(responseH, responseV, initTrans, lightPos);

                // Transformをシミュレーターにセット
                dataPool.SetTargetPosOffset(targetTrans2D, objKeys[i]);

                // レンダリングやデバッグ時に必要なので保持しておく
                targetTransforms[i] = targetTrans2D;
            }
        }

        /// <summary>
        /// メモリ解放
        /// </summary>
        public void Release()
        {
            uvMaskGenerator.Release();
            sdfCalculator.Release();
        }
    }
}
