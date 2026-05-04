using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

/// <summary>
/// Unityのシステム上でのボーンによるスキニングとブレンドシェイプの適用結果を取得するのが難しいので、自分で計算する
/// </summary>
public class BlendShapeBonesSelf
{
    private SkinnedMeshRenderer smr;
    private Transform myBone;

    private Matrix4x4 myBoneBindPoseInverse;
    private Matrix4x4 myBoneCurrentMatrix;

    private int shapeCount;
    private int vertexCount;
    private Vector3[][] blendShapeDeltas;

    private ComputeShader compute;

    private int kernelResetBlendShape;
    private int kernelApplyBlendShape;
    private int kernelApplyBone;

    private ComputeBuffer localVerticesBuffer;
    private ComputeBuffer worldVerticesBuffer;
    private ComputeBuffer trianglesBuffer;
    // ブレンドシェイプの項目毎にバッファを分ける
    private ComputeBuffer[] blendShapeDeltasBuffers;
    private ComputeBuffer blendShapeResultsBuffer;

    static readonly ProfilerMarker marker = new ProfilerMarker("MyMarkerBlendShapeBone");

    public BlendShapeBonesSelf(SkinnedMeshRenderer smr, Transform myBone)
    {
        this.smr = smr;

        this.myBone = myBone;

        // バインドポーズの逆行列を計算しておく
        // このメッシュは最初から原点にあるとは限らないので、ボーンの行列はワールド空間ではなくメッシュのローカル空間基準で計算する
        Matrix4x4 boneLocalBindPose = smr.transform.worldToLocalMatrix * myBone.localToWorldMatrix;
        myBoneBindPoseInverse = boneLocalBindPose.inverse;

        // ブレンドシェイプの、項目・頂点ごとの移動ベクトルを取得する
        shapeCount = smr.sharedMesh.blendShapeCount;
        vertexCount = smr.sharedMesh.vertexCount;

        blendShapeDeltas = new Vector3[shapeCount][];
        for (int i = 0; i < shapeCount; i++)
        {
            // 各シェイプの頂点数分の配列を確保
            blendShapeDeltas[i] = new Vector3[vertexCount];

            // 頂点ごとの移動ベクトルを取得
            smr.sharedMesh.GetBlendShapeFrameVertices(i, 0, blendShapeDeltas[i], null, null);
        }

        // ComputeShader関係の初期化
        InitCS();
    }

    /// <summary>
    /// ComputeShader関係の初期化処理
    /// </summary>
    private void InitCS()
    {
        compute = Object.Instantiate(Resources.Load<ComputeShader>("ComputeShader/Input/BlendShapeBoneSelf"));

        kernelResetBlendShape = compute.FindKernel("ResetBlendShape");
        kernelApplyBlendShape = compute.FindKernel("ApplyBlendShape");
        kernelApplyBone = compute.FindKernel("ApplyBone");

        // バッファの生成
        localVerticesBuffer = ComputeHelper.CreateStructuredBuffer(smr.sharedMesh.vertices);
        worldVerticesBuffer = ComputeHelper.CreateStructuredBuffer<Vector3>(smr.sharedMesh.vertices.Length);
        trianglesBuffer = ComputeHelper.CreateStructuredBuffer(smr.sharedMesh.triangles);
        blendShapeResultsBuffer = ComputeHelper.CreateStructuredBuffer<Vector3>(smr.sharedMesh.vertices.Length);
        blendShapeDeltasBuffers = new ComputeBuffer[shapeCount];
        for (int i = 0; i < shapeCount; i++)
        {
            blendShapeDeltasBuffers[i] = ComputeHelper.CreateStructuredBuffer(blendShapeDeltas[i]);
        }

        ComputeHelper.SetBuffer(compute, localVerticesBuffer, "LocalVertices", kernelApplyBone);
        ComputeHelper.SetBuffer(compute, worldVerticesBuffer, "WorldVertices", kernelApplyBone);
        ComputeHelper.SetBuffer(compute, blendShapeResultsBuffer, "BlendShapeResults", kernelResetBlendShape, kernelApplyBlendShape, kernelApplyBone);

        compute.SetInt("_NumVertices", vertexCount);

    }

    public ComputeBuffer Calculate()
    {
        using (marker.Auto())
        {
            // ブレンドシェイプの加算結果をリセット
            compute.Dispatch(kernelResetBlendShape, Mathf.CeilToInt(vertexCount / 64f), 1, 1);

            // ブレンドシェイプの各シェイプのウェイトをチェックし、0より大きい場合はローカル座標に加算する
            for (int i = 0; i < shapeCount; i++)
            {
                float weight = smr.GetBlendShapeWeight(i) / 100f; // GetBlendShapeWeightは0～100を返すので、正規化する

                if (weight > 0)
                {
                    compute.SetBuffer(kernelApplyBlendShape, "BlendShapeDeltas", blendShapeDeltasBuffers[i]);
                    compute.SetFloat("_BlendShapeWeight", weight);

                    compute.Dispatch(kernelApplyBlendShape, Mathf.CeilToInt(vertexCount / 64f), 1, 1);
                }
            }

            // 現在のボーン行列を計算
            myBoneCurrentMatrix = myBone.localToWorldMatrix * myBoneBindPoseInverse;

            // ボーンの影響を加算
            compute.SetMatrix("_BoneMatrix", myBoneCurrentMatrix);

            compute.Dispatch(kernelApplyBone, Mathf.CeilToInt(worldVerticesBuffer.count / 64f), 1, 1);
        }

        return worldVerticesBuffer;
    }

    public void ReleaseBuffers()
    {
        ComputeHelper.Release(localVerticesBuffer, worldVerticesBuffer, trianglesBuffer, blendShapeResultsBuffer);

        if (blendShapeDeltasBuffers != null)
        {
            for (int i = 0; i < blendShapeDeltasBuffers.Length; i++)
            {
                ComputeHelper.Release(blendShapeDeltasBuffers[i]);
            }
        }
    }
}
