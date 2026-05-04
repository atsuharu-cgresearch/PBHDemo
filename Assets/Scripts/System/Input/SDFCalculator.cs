using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace PBH
{
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

        public RenderTexture Calculate(RenderTexture input)
        {
            int width = input.width;
            int height = input.height;
            int groupsX = Mathf.CeilToInt(width / 8f);
            int groupsY = Mathf.CeilToInt(height / 8f);

            compute.SetInt("_TexSize", Mathf.Max(width, height));

            // ‹«ŠE•”•ª‚ğ‹æ•Ê
            compute.SetTexture(kInit, "_InputTex", input);
            compute.SetTexture(kInit, "_BufferWrite", bufferA);
            compute.Dispatch(kInit, groupsX, groupsY, 1);

            // JFA‚ÌƒƒCƒ“‚Ì•”•ª
            int maxSide = Mathf.Max(width, height);
            int steps = Mathf.CeilToInt(Mathf.Log(maxSide, 2));

            for (int i = 0; i < steps; i++)
            {
                int stepWidth = (int)Mathf.Pow(2, steps - 1 - i);
                compute.SetInt("_StepWidth", stepWidth);

                var read = (i % 2 == 0) ? bufferA : bufferB;
                var write = (i % 2 == 0) ? bufferB : bufferA;

                compute.SetTexture(kJFA, "_BufferRead", read);
                compute.SetTexture(kJFA, "_BufferWrite", write);
                compute.Dispatch(kJFA, groupsX, groupsY, 1);
            }

            // Œ‹‰Ê‚ğ‘‚«‚Ş
            compute.SetTexture(kWrite, "_InputTex", input);
            compute.SetTexture(kWrite, "_Result", sdfRT);
            compute.SetTexture(kWrite, "_BufferRead", (steps % 2 == 0) ? bufferA : bufferB);
            
            compute.Dispatch(kWrite, groupsX, groupsY, 1);

            return sdfRT;
        }

        public void Release()
        {
            
        }
    }
}
