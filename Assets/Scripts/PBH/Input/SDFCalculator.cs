using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBH
{
    /// <summary>
    /// 2値画像をもとにSDFを生成する（実際には境界からの距離ではなく、境界までの最短移動経路のxy成分をRGチャンネルに保存する）
    /// </summary>
    public class SDFCalculator
    {
        private ComputeShader compute;

        private int kInit;
        private int kJFA;
        private int kWrite;

        private RenderTexture bufferA;
        private RenderTexture bufferB;

        private RenderTexture sdfRT;

        public SDFCalculator(int texSize)
        {
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Input/SDF"));

            kInit = compute.FindKernel("KInit");
            kJFA = compute.FindKernel("KJFA");
            kWrite = compute.FindKernel("kWrite");

            ComputeHelper.CreateRenderTexture(ref bufferA, texSize, texSize, RenderTextureFormat.ARGBFloat);
            ComputeHelper.CreateRenderTexture(ref bufferB, texSize, texSize, RenderTextureFormat.ARGBFloat);
            ComputeHelper.CreateRenderTexture(ref sdfRT, texSize, texSize, RenderTextureFormat.ARGBFloat);
        }

        public RenderTexture Calculate(RenderTexture input, CommandBuffer cmd)
        {
            int width = input.width;
            int height = input.height;
            int groupsX = Mathf.CeilToInt(width / 8f);
            int groupsY = Mathf.CeilToInt(height / 8f);

            // compute.SetInt("_TexSize", Mathf.Max(width, height));
            cmd.SetComputeIntParam(compute, "_TexSize", Mathf.Max(width, height));

            // 境界部分を区別
            // compute.SetTexture(kInit, "_InputTex", input);
            // compute.SetTexture(kInit, "_BufferWrite", bufferA);
            // compute.Dispatch(kInit, groupsX, groupsY, 1);
            cmd.SetComputeTextureParam(compute, kInit, "_InputTex", input);
            cmd.SetComputeTextureParam(compute, kInit, "_BufferWrite", bufferA);
            cmd.DispatchCompute(compute, kInit, groupsX, groupsY, 1);

            // JFAのメインの部分
            int maxSide = Mathf.Max(width, height);
            int steps = Mathf.CeilToInt(Mathf.Log(maxSide, 2));

            for (int i = 0; i < steps; i++)
            {
                int stepWidth = (int)Mathf.Pow(2, steps - 1 - i);
                // compute.SetInt("_StepWidth", stepWidth);
                cmd.SetComputeIntParam(compute, "_StepWidth", stepWidth);

                var read = (i % 2 == 0) ? bufferA : bufferB;
                var write = (i % 2 == 0) ? bufferB : bufferA;

                // compute.SetTexture(kJFA, "_BufferRead", read);
                // compute.SetTexture(kJFA, "_BufferWrite", write);
                // compute.Dispatch(kJFA, groupsX, groupsY, 1);
                cmd.SetComputeTextureParam(compute, kJFA, "_BufferRead", read);
                cmd.SetComputeTextureParam(compute, kJFA, "_BufferWrite", write);
                cmd.DispatchCompute(compute, kJFA, groupsX, groupsY, 1);
            }

            // 結果を書き込む
            // compute.SetTexture(kWrite, "_InputTex", input);
            // compute.SetTexture(kWrite, "_Result", sdfRT);
            // compute.SetTexture(kWrite, "_BufferRead", (steps % 2 == 0) ? bufferA : bufferB);
            // compute.Dispatch(kWrite, groupsX, groupsY, 1);
            cmd.SetComputeTextureParam(compute, kWrite, "_InputTex", input);
            cmd.SetComputeTextureParam(compute, kWrite, "_Result", sdfRT);
            cmd.SetComputeTextureParam(compute, kWrite, "_BufferRead", (steps % 2 == 0) ? bufferA : bufferB);
            cmd.DispatchCompute(compute, kWrite, groupsX, groupsY, 1);

            return sdfRT;
        }

        public void Release()
        {
            
        }
    }
}
