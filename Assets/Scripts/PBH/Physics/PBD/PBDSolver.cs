using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBH
{
    public class PBDSolver
    {
        private Body body;
        private ExternalDataPool dataPool;

        private ComputeShader compute;

        private int kPredict;
        private int kUpdate;

        private Parameter parameter;

        [System.Serializable]
        public class Parameter
        {
            public int numIterations = 3;
            public int numSubsteps = 3;
            public float damping = 0.99f;
            public Vector3 gravity = Physics.gravity; // 動作確認用

            public float stiffnessArea = 0.03f;
            public float stiffnessShapeMatch = 0.03f;
            public float stiffnessCollision = 0.1f;
            public float stiffnessTargetPos = 0.001f;
        }

        public PBDSolver(Parameter parameter, Body body, ExternalDataPool dataPool)
        {
            this.body = body;
            this.parameter = parameter;
            this.dataPool = dataPool;

            InitComputeShader();
        }

        /// <summary>
        /// ComputeShaderまわりの初期化
        /// </summary>
        private void InitComputeShader()
        {
            compute = Resources.Load<ComputeShader>("ComputeShader/Physics/PBDSolver");

            kPredict = compute.FindKernel("CS_Predict");
            kUpdate = compute.FindKernel("CS_Update");

            ComputeHelper.SetBuffer(compute, body.ParticleBuffer, "_Particles", kPredict, kUpdate);
        }

        /// <summary>
        /// PBDの1ステップの処理を実行
        /// 今回はSubstep法を採用する
        /// </summary>
        public void Step(float dt)
        {
            // GPU上の実行時間を計測するための準備
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "PBD_Simulation";
            cmd.BeginSample("PBD_Step");

            // １サブステップの微小時間
            float subDt = dt / parameter.numSubsteps;

            // 各拘束条件ソルバーに、入力データを渡して更新する
            if (body.CollisionSolver != null)
            {
                body.CollisionSolver.SetSDFArray(dataPool.SDFArray);
                body.CollisionSolver.SetColliderTransforms(dataPool.ColliderTransforms);
            }
            if (body.TargetPosSolver != null)
            {
                body.TargetPosSolver.SetOffsets(dataPool.TargetPosTransforms);
            }
            if (body.TargetPosForce != null)
            {
                body.TargetPosForce.SetOffsets(dataPool.TargetPosTransforms);
            }

            compute.SetInt("_NumParticles", body.ParticleBuffer.count);
            compute.SetFloat("_Dt", subDt);
            compute.SetFloat("_Damping", parameter.damping);
            compute.SetVector("_Gravity", parameter.gravity);

            body.CollisionSolver.Bind(body.ParticleBuffer, body.LayerBuffer);
            body.TargetPosSolver.Bind(body.ParticleBuffer, body.LocalPosBuffer, body.ObjectIndexBuffer);
            body.TargetPosForce.Bind(body.ParticleBuffer, body.LocalPosBuffer, body.ObjectIndexBuffer, dt);

            // substepping
            for (int i = 0; i < parameter.numSubsteps; i++)
            {
                Substep(cmd);
            }

            // 計測終了
            cmd.EndSample("PBD_Step");
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        /// <summary>
        /// サブステップ法の1ステップの処理
        /// </summary>
        private void Substep(CommandBuffer cmd)
        {
            int threadGroups = Mathf.CeilToInt(body.ParticleBuffer.count / 64f);

            cmd.BeginSample("PBD_Substep");
            
            // 目標位置への移動を力ベースで実装する場合はこれ
            // 慣性で揺れてLive2Dっぽくなる
            // float k_TargetPosForce = parameter.stiffnessTargetPos;
            // body.TargetPosForce.ApplyForce(k_TargetPosForce);

            cmd.BeginSample("PBD_Predict");
            // compute.Dispatch(kPredict, threadGroups, 1, 1);
            cmd.DispatchCompute(compute, kPredict, threadGroups, 1, 1);
            cmd.EndSample("PBD_Predict");

            // 各拘束条件ソルバーの実行
            for (int i = 0; i < parameter.numIterations; i++)
            {
                if (body.TargetPosSolver != null)
                {
                    float k_TargetPos = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.stiffnessTargetPos), 1f / (parameter.numIterations * parameter.numSubsteps));
                    body.TargetPosSolver.ConstrainPositions(k_TargetPos, cmd);
                }

                if (body.AreaConstraint != null)
                {
                    float k_Area = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.stiffnessArea), 1f / (parameter.numIterations * parameter.numSubsteps));
                    body.AreaConstraint.ConstrainPositions(k_Area, cmd);
                }

                if (body.ShapeMatchConstraint != null)
                {
                    float k_ShapeMatch = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.stiffnessShapeMatch), 1f / (parameter.numIterations * parameter.numSubsteps));
                    body.ShapeMatchConstraint.ConstrainPositions(k_ShapeMatch, cmd);
                }

                if (body.CollisionSolver != null)
                {
                    float k_Collision = 1 - Mathf.Pow(1 - Mathf.Clamp01(parameter.stiffnessCollision), 1f / (parameter.numIterations * parameter.numSubsteps));
                    body.CollisionSolver.ConstrainPositions(k_Collision, cmd);
                }
            }

            cmd.BeginSample("PBD_Update");
            // compute.Dispatch(kUpdate, threadGroups, 1, 1);
            cmd.DispatchCompute(compute, kUpdate, threadGroups, 1, 1);
            cmd.EndSample("PBD_Update");

            cmd.EndSample("PBD_Substep");
        }
    }
}