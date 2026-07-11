using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBH
{
    public class UVMaskGenerator : IUVMaskGenerator
    {
        private struct BarycentricData
        {
            public int id0, id1, id2;
            public float b0, b1, b2;
        }

        private TargetMesh target;
        private Occluder occluder;
        private int texSize;

        private ComputeShader compute;

        private int kernelCalcBarycentric;
        private int kernelGenMask;

        private ComputeBuffer targetUVsBuffer;
        private ComputeBuffer targetTrianglesBuffer;
        private ComputeBuffer targetWorldVerticesBuffer;

        private ComputeBuffer occluderTrianglesBuffer;
        private ComputeBuffer occluderWorldVerticesBuffer;

        private RenderTexture keepAreaTexture;

        // WebGL向けにバッファの数を節約したいので、バッファを使わずテクスチャ2枚に分ける
        private RenderTexture barycentricIDTexture;
        private RenderTexture barycentricWeightTexture;
        // private ComputeBuffer barycentricDataBuffer;
        private RenderTexture maskTexture;


        public UVMaskGenerator(TargetMesh tgt, Occluder occ, int texSize)
        {
            target = tgt;
            occluder = occ;
            this.texSize = texSize;

            InitKeepAreaTexture();

            InitCS();

            PrecomputeBarycentricData();
        }

        /// <summary>
        /// KeepAreaTextureの初期設定を行う
        /// MaskTextureの解像度と同じサイズにし、Texture2DからRenderTextureに変換する
        /// </summary>
        private void InitKeepAreaTexture()
        {
            // 入れ物を先に生成
            ComputeHelper.CreateRenderTexture(ref keepAreaTexture, texSize, texSize, RenderTextureFormat.ARGBFloat);

            Texture2D keepAreaTexOrg = target.keepAreaTexture;

            // ターゲットのメッシュがテクスチャを持っていないときは、空のテクスチャのまま使う
            if (keepAreaTexOrg == null) return;

            // Texture2DをリサイズしながらRenderTextureに書き込む
            Graphics.Blit(keepAreaTexOrg, keepAreaTexture);
        }

        private void InitCS()
        {
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Input/UVMask"));
            
            kernelCalcBarycentric = compute.FindKernel("CalcBarycentric");
            kernelGenMask = compute.FindKernel("GenMask");

            // バッファの生成
            // WorldVerticesBufferは、計算させた結果を取得するのでここでは生成しない
            targetUVsBuffer = ComputeHelper.CreateStructuredBuffer(target.MeshUVs);
            targetTrianglesBuffer = ComputeHelper.CreateStructuredBuffer(target.MeshTriangles);
            occluderTrianglesBuffer = ComputeHelper.CreateStructuredBuffer(occluder.MeshTriangles);
            ComputeHelper.CreateRenderTexture(ref maskTexture, texSize, texSize, RenderTextureFormat.ARGBFloat);
            ComputeHelper.CreateRenderTexture(ref barycentricIDTexture, texSize, texSize, RenderTextureFormat.ARGBFloat);
            ComputeHelper.CreateRenderTexture(ref barycentricWeightTexture, texSize, texSize, RenderTextureFormat.ARGBFloat);
            // barycentricDataBuffer = ComputeHelper.CreateStructuredBuffer<BarycentricData>(maskTexture.width * maskTexture.height);

            // バッファと変数のセット
            ComputeHelper.SetBuffer(compute, targetUVsBuffer, "TargetUVs", kernelCalcBarycentric);
            ComputeHelper.SetBuffer(compute, targetTrianglesBuffer, "TargetTriangles", kernelCalcBarycentric);
            ComputeHelper.SetBuffer(compute, occluderTrianglesBuffer, "OccluderTriangles", kernelGenMask);
            // ComputeHelper.SetBuffer(compute, barycentricDataBuffer, "BarycentricDataPixels", kernelCalcBarycentric, kernelGenMask);
            ComputeHelper.AssignTexture(compute, barycentricIDTexture, "_BarycentricIDTextureWrite", kernelCalcBarycentric);
            ComputeHelper.AssignTexture(compute, barycentricWeightTexture, "_BarycentricWeightTextureWrite", kernelCalcBarycentric);
            ComputeHelper.AssignTexture(compute, barycentricIDTexture, "_BarycentricIDTextureRead", kernelGenMask);
            ComputeHelper.AssignTexture(compute, barycentricWeightTexture, "_BarycentricWeightTextureRead", kernelGenMask);
            ComputeHelper.AssignTexture(compute, maskTexture, "UVMask", kernelGenMask);
            ComputeHelper.AssignTexture(compute, keepAreaTexture, "KeepAreaTexture", kernelGenMask);
            compute.SetInt("_TexSize", maskTexture.width);
            compute.SetInt("_NumTargetTriangles", targetTrianglesBuffer.count / 3);
            compute.SetInt("_NumOccluderTriangles", occluderTrianglesBuffer.count / 3);
        }

        /// <summary>
        /// マスクテクスチャのピクセルごとに、参照するポリゴンの3頂点のインデックスとバリセントリック座標を計算し保持しておく
        /// </summary>
        private void PrecomputeBarycentricData()
        {
            compute.Dispatch(kernelCalcBarycentric, Mathf.CeilToInt(maskTexture.width / 8f), Mathf.CeilToInt(maskTexture.height / 8f), 1);
        }

        public RenderTexture Generate(Vector3 camPos, CommandBuffer cmd)
        {
            // Targetの頂点のワールド座標を計算して取得する
            targetWorldVerticesBuffer = target.CalculateBlendShapeBoneSelf(cmd);

            // Occluderの頂点のワールド座標を計算して取得する
            occluderWorldVerticesBuffer = occluder.CalculateBlendShapeBoneSelf(cmd);

            // UVマップの画素ごとに交差判定を実行し、マスク済みUVマップを作成する
            // compute.SetBuffer(kernelGenMask, "TargetWorldVertices", targetWorldVerticesBuffer);
            // compute.SetBuffer(kernelGenMask, "OccluderWorldVertices", occluderWorldVerticesBuffer);
            // compute.SetVector("_ViewPos", camPos);
            cmd.SetComputeBufferParam(compute, kernelGenMask, "TargetWorldVertices", targetWorldVerticesBuffer);
            cmd.SetComputeBufferParam(compute, kernelGenMask, "OccluderWorldVertices", occluderWorldVerticesBuffer);
            cmd.SetComputeVectorParam(compute, "_ViewPos", camPos);

            // compute.Dispatch(kernelGenMask, Mathf.CeilToInt(maskTexture.width / 8f), Mathf.CeilToInt(maskTexture.height / 8f), 1);
            cmd.DispatchCompute(compute, kernelGenMask, Mathf.CeilToInt(maskTexture.width / 8f), Mathf.CeilToInt(maskTexture.height / 8f), 1);

            return maskTexture;
        }

        public void Release()
        {
            ComputeHelper.Release(
                targetUVsBuffer,
                targetTrianglesBuffer,
                occluderTrianglesBuffer
                // barycentricDataBuffer
                );
        }
    }
}
