using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    /// <summary>
    /// システム全体の管理を行うクラス
    /// </summary>
    public partial class HighlightSystem : MonoBehaviour
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
                int[] keys = simInputCalculators[i].objKeys;
                ParticleRange[] references = new ParticleRange[keys.Length];
                for (int j = 0; j < keys.Length; j++)
                {
                    // シミュレーション結果を参照するための情報を取得する
                    references[j] = simulator.DataPool.GetParticleReference(keys[j]);
                }

                simResultRenderers[i] = new SimulationResultRenderer(input.slots[i], simulator.DataPool.ParticleBuffer, references, input.rendererRTSize);
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

        private void Update()
        {
            if (!input.isActive || input.fixedUpdate) return;

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
            simulator.Execute(Mathf.Clamp(Time.deltaTime, 1 / 500f, 1 / 50f));


            // 結果を取得してレンダリング
            for (int i = 0; i < input.slots.Count; i++)
            {
                Transform2D textureTransform = simInputCalculators[i].textureTransform;

                simResultRenderers[i].Render(textureTransform);
            }
        }

        private void FixedUpdate()
        {
            if (!input.isActive || !input.fixedUpdate) return;

            // Rキーでリセット
            // if (Input.GetKeyDown(KeyCode.R)) ResetHighlight();

            // 現在のカメラ・光源の位置と姿勢を取得
            Vector3 camPos = input.cameraTransform.position;
            Quaternion camRot = input.cameraTransform.rotation;
            Vector3 lightPos = input.lightTransform.position;
            Quaternion lightRot = input.lightTransform.rotation;

            // シミュレーションに入力するデータを更新する
            for (int i = 0; i < input.slots.Count; i++)
            {
                simInputCalculators[i].UpdatePhysicsInputs(simulator.DataPool, camPos, camRot, lightPos);
            }

            // シミュレーションを実行
            simulator.Execute(Time.fixedDeltaTime);

            // 結果を取得してレンダリング
            for (int i = 0; i < input.slots.Count; i++)
            {
                Transform2D textureTransform = simInputCalculators[i].textureTransform;
 
                simResultRenderers[i].Render(textureTransform);
            }
        }

        private void OnDestroy()
        {
            // 各コンポーネントが使用していたメモリを全て解放する

            simulator.ReleaseBuffers();

            for (int i = 0; i < numSlots; i++)
            {
                simInputCalculators[i].Release();

                simResultRenderers[i].Release();
            }
        }
    }
}
