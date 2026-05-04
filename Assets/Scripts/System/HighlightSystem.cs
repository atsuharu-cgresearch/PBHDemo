using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    public class HighlightSystem : MonoBehaviour
    {
        // パラメータ・設定の入力を管理するクラス
        private HighlightInput input;

        // PBDシミュレーションを管理するクラス
        private Simulator simulator;

        // シミュレーターへ入力するデータの計算
        private SimulationInputCalculator[] simInputCalculators;
        // シミュレーション結果の取得と描画
        private SimulationResultRenderer[] simResultRenderers;

        // 現在扱っているオブジェクトの数
        private int numSlots;

        private void Start()
        {
            input = GetComponent<HighlightInput>();

            InitComponents();

            ResetHighlight();
        }

        private void InitComponents()
        {
            // コンポーネントにシミュレーターの参照を渡すので先にインスタンス化する必要がある
            simulator = new Simulator();

            numSlots = input.slots.Count;

            simInputCalculators = new SimulationInputCalculator[numSlots];
            for (int i = 0; i < numSlots; i++)
            {
                simInputCalculators[i] = new SimulationInputCalculator(input.slots[i], input.colliderRTSize, i);

                // シミュレーターにシミュレーションさせるオブジェクトのデータを登録する
                simInputCalculators[i].RegisterForPhysics(simulator.BodyCreator);
            }

            // シミュレーターを初期化する
            // inputCalculatorがシミュレーターに登録した後で初期化する必要がある
            simulator.Initialize(input.solverParameter, input.colliderRTSize);

            simResultRenderers = new SimulationResultRenderer[numSlots];
            for (int i = 0; i < numSlots; i++)
            {
                simResultRenderers[i] = new SimulationResultRenderer();

                int[] keys = simInputCalculators[i].objKeys;
                ParticleRange[] references = new ParticleRange[keys.Length];
                for (int j = 0; j < keys.Length; j++)
                {
                    // シミュレーション結果を参照するための情報を取得する
                    references[j] = simulator.DataPool.GetParticleReference(keys[j]);
                }

                simResultRenderers[i].InitRenderer(input.slots[i], simulator.DataPool.ParticleBuffer, references, input.rendererRTSize);
            }
        }

        /// <summary>
        /// 現在のカメラとライトの位置関係とハイライトの状態が基準になるように記憶する
        /// </summary>
        private void ResetHighlight()
        {
            for (int i = 0; i < input.slots.Count; i++)
            {
                simInputCalculators[i].Reset(input.cameraTransform.position, input.lightTransform.position);
            }
        }

        private void FixedUpdate()
        {
            if (!input.isActive) return;

            // Rキーでリセット
            // if (Input.GetKeyDown(KeyCode.R)) ResetHighlight();

            Vector3 camPos = input.cameraTransform.position;
            Quaternion camRot = input.cameraTransform.rotation;
            Vector3 lightPos = input.lightTransform.position;
            Quaternion lightRot = input.lightTransform.rotation;


            for (int i = 0; i < input.slots.Count; i++)
            {
                simInputCalculators[i].UpdatePhysicsInputs(simulator.DataPool, camPos, camRot, lightPos);
            }

            // シミュレーションを実行
            // deltaTimeに0に近い値が入ったり、極端に大きな値が入るとシミュレーションが破綻するため、適当にClampする
            // float dtClamp = Mathf.Clamp(Time.deltaTime, 1 / 144f, 1 / 50f);
            simulator.Execute(Time.fixedDeltaTime);


            // 結果を取得してレンダリング
            for (int i = 0; i < input.slots.Count; i++)
            {
                Transform2D textureTransform = simInputCalculators[i].textureTransform;
 
                simResultRenderers[i].Render(textureTransform);
            }
        }

        #region デバッグ用メソッド
        /// <summary>
        /// （デバッグ用）指定したスロットのパーティクルバッファと範囲情報を取得する
        /// </summary>
        public bool TryGetDebugData(int slotIndex, out ComputeBuffer buffer, out ParticleRange[] ranges, out Transform2D[] targetTransforms, out Transform2D colliderTransform, out RenderTexture colliderTexture)
        {
            buffer = null;
            ranges = null;
            targetTransforms = null;
            colliderTransform = new Transform2D();
            colliderTexture = null;

            // 初期化前や範囲外のアクセスを防ぐ
            if (simulator == null || simulator.DataPool == null) return false;
            if (slotIndex < 0 || slotIndex >= numSlots) return false;

            buffer = simulator.DataPool.ParticleBuffer;

            int[] keys = simInputCalculators[slotIndex].objKeys;
            ranges = new ParticleRange[keys.Length];
            targetTransforms = new Transform2D[keys.Length];
            for (int j = 0; j < keys.Length; j++)
            {
                ranges[j] = simulator.DataPool.GetParticleReference(keys[j]);
                targetTransforms[j] = simInputCalculators[slotIndex].targetTransforms[j];
            }

            colliderTransform = simInputCalculators[slotIndex].textureTransform;

            colliderTexture = simInputCalculators[slotIndex].colliderTexture;

            return true;
        }

        public bool TryGetTargetData(int slotIndex, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            if (slotIndex < 0 || slotIndex >= numSlots) return false;

            pos = input.slots[slotIndex].target.WorldOriginUVN;
            rot = input.slots[slotIndex].target.WorldRotationUVN;

            return true;
        }

        public int GetSlotCount()
        {
            return numSlots;
        }

        public TargetMesh GetTargetMesh(int slotIndex)
        {
            return input.slots[slotIndex].target;
        }
        #endregion

        private void OnDestroy()
        {
            simulator.ReleaseBuffers();

            for (int i = 0; i < numSlots; i++)
            {
                simInputCalculators[i].Release();

                simResultRenderers[i].Release();
            }
        }
    }
}
