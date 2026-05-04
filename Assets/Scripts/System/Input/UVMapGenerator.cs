using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    /// <summary>
    /// 目以外の、遮蔽物を使用しないメッシュに使用し、メッシュのUVマップを生成する
    /// </summary>
    public class UVMapGenerator : IUVMapGenerator
    {
        private TargetMesh target;

        private ComputeShader compute;
        private int kernelGenMap;
        private ComputeBuffer uvsBuffer;
        private ComputeBuffer trianglesBuffer;
        private RenderTexture mapTexture;

        public UVMapGenerator(TargetMesh tgt, int texSize)
        {
            target = tgt;

            InitCS(texSize);

            ExecuteCS();
        }

        private void InitCS(int texSize)
        {
            compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Input/UVMap"));

            kernelGenMap = compute.FindKernel("GenMap");

            // バッファの生成
            uvsBuffer = ComputeHelper.CreateStructuredBuffer(target.MeshUVs);
            trianglesBuffer = ComputeHelper.CreateStructuredBuffer(target.MeshTriangles);
            ComputeHelper.CreateRenderTexture(ref mapTexture, texSize, texSize, RenderTextureFormat.ARGBFloat);

            // バッファと変数のセット
            ComputeHelper.SetBuffer(compute, uvsBuffer, "UVs", kernelGenMap);
            ComputeHelper.SetBuffer(compute, trianglesBuffer, "Triangles", kernelGenMap);
            ComputeHelper.AssignTexture(compute, mapTexture, "UVMap", kernelGenMap);
            compute.SetInt("_TexSize", mapTexture.width);
            compute.SetInt("_NumTriangles", trianglesBuffer.count / 3);
        }

        private void ExecuteCS()
        {
            compute.Dispatch(kernelGenMap, Mathf.CeilToInt(mapTexture.width / 8f), Mathf.CeilToInt(mapTexture.width / 8f), 1);
        }

        // 引数で camPos が渡されてくるが、計算には使わずキャッシュした結果を返すだけ
        public RenderTexture Generate(Vector3 camPos)
        {
            return mapTexture;
        }

        public void Release()
        {
            ComputeHelper.Release(uvsBuffer, trianglesBuffer);
        }
    }
}
