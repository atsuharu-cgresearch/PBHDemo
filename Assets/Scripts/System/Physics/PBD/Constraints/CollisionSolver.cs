using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBH
{
    public class CollisionSolver
    {
        private ComputeShader compute;
        private int kMain;
        private int threadGroups;

        private Texture2DArray sdfTex2DArray;
        private ComputeBuffer colliderTransformBuffer;

        public readonly int MAX_LAYERS = 16;

        public CollisionSolver(int colliderTexSize)
        {
            compute = Resources.Load<ComputeShader>("ComputeShader/Physics/CollisionConstraint");

            kMain = compute.FindKernel("CS_Main");

            sdfTex2DArray = new Texture2DArray(
                    colliderTexSize,
                    colliderTexSize,
                    MAX_LAYERS,
                    TextureFormat.RGBAFloat,
                    false);

            colliderTransformBuffer = ComputeHelper.CreateStructuredBuffer<Vector4>(MAX_LAYERS);
        }

        public void SetSDFArray(RenderTexture[] rtArray)
        {
            for (int i = 0; i < rtArray.Length; i++)
            {
                if (rtArray[i] == null) continue;

                if (rtArray[i].width != sdfTex2DArray.width)
                {
                    Debug.LogWarning("テクスチャのサイズが一致しません");
                    continue;
                }

                if (i >= sdfTex2DArray.depth)
                {
                    Debug.LogWarning("テクスチャの数がレイヤー数を超えています");
                    return;
                }

                // コピーを実行
                Graphics.CopyTexture(rtArray[i], 0, sdfTex2DArray, i);
            }
        }

        public void SetColliderTransforms(Transform2D[] transforms)
        {
            colliderTransformBuffer.SetData(transforms, 0, 0, transforms.Length);
        }

        public void Bind(ComputeBuffer particles, ComputeBuffer references)
        {
            compute.SetBuffer(kMain, "_Particles", particles);
            compute.SetBuffer(kMain, "_References", references);
            compute.SetBuffer(kMain, "_Transforms", colliderTransformBuffer);
            ComputeHelper.AssignTexture(compute, sdfTex2DArray, "_SDFArray", kMain);

            compute.SetInt("_NumParticles", particles.count);
            compute.SetInt("_TexSize", sdfTex2DArray.width);

            threadGroups = Mathf.CeilToInt(particles.count / 64f);
        }

        public void ConstrainPositions(float k, CommandBuffer cmd)
        {
            // 動的に変更されるパラメータは引数で受け取ってここで設定する
            compute.SetFloat("_K", k);

            // 拘束条件を解いて、パーティクルの位置を修正
            cmd.BeginSample("PBD_Collision");
            // compute.Dispatch(kMain, threadGroups, 1, 1);
            cmd.DispatchCompute(compute, kMain, threadGroups, 1, 1);
            cmd.EndSample("PBD_Collision");
        }

        public void ReleaseBuffers()
        {
            ComputeHelper.Release(colliderTransformBuffer);
        }
    }
}
