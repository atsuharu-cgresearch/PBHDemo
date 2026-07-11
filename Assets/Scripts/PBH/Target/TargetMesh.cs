using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBH
{
    /// <summary>
    /// メッシュの現在の状態の計算、保持
    /// シミュレーション結果を描画したUVテクスチャを受け取って描画
    /// </summary>
    public class TargetMesh : MonoBehaviour
    {
        // ローカル座標系（U-V-N）
        [Header("My Boneから見た位置と回転を入力")]
        [SerializeField] private Vector3 originUVN = Vector3.zero;
        [SerializeField] private Quaternion rotationUVN = Quaternion.identity;
        // [SerializeField] private Vector3 scale;

        // シェイプキーのコピー元
        [SerializeField] private SkinnedMeshRenderer srcSmr;
        [SerializeField] private Transform myBone;
        private SkinnedMeshRenderer smr;
        private BlendShapeBonesSelf blendShapeBoneSelf;

        // このメッシュがOccluderと組み合わせて使用されるとき、強制的にArea判定される範囲
        // まばたきした時にハイライトが完全に潰されてしまうのを防ぐ
        public Texture2D keepAreaTexture;

        // UVテクスチャ描画用
        private Material material;

        // このGameObject自体のTransformは変化しないので、ボーンのTransformを返す
        public Vector3 WorldOriginUVN => myBone.localToWorldMatrix.MultiplyPoint3x4(originUVN);
        public Quaternion WorldRotationUVN => myBone.rotation * rotationUVN;

        public Vector2[] MeshUVs { get; private set; }
        public int[] MeshTriangles { get; private set; }

        public bool drawSceneGizmo = true;

        private void Awake()
        {
            smr = GetComponent<SkinnedMeshRenderer>();

            CopyBlendShapes(srcSmr, smr);

            MeshUVs = (Vector2[])smr.sharedMesh.uv.Clone();
            MeshTriangles = (int[])smr.sharedMesh.triangles.Clone();

            blendShapeBoneSelf = new BlendShapeBonesSelf(smr, myBone);

            material = GetComponent<Renderer>().material;
        }

        private void Update()
        {
            CopyBlendShapes(srcSmr, smr);
        }

        /// <summary>
        /// VRMの管理対称の範囲外なので、手動でSkinnedMeshRendererのパラメータをコピーする
        /// </summary>
        private void CopyBlendShapes(SkinnedMeshRenderer src, SkinnedMeshRenderer dst)
        {
            int count = src.sharedMesh.blendShapeCount;

            for (int i = 0; i < count; i++)
            {
                float weight = src.GetBlendShapeWeight(i);
                dst.SetBlendShapeWeight(i, weight);
            }
        }

        /// <summary>
        /// ブレンドシェイプとボーンによる変形後のメッシュのデータを高速に取得する方法がないので、自分で計算する
        /// </summary>
        public ComputeBuffer CalculateBlendShapeBoneSelf(CommandBuffer cmd)
        {
            return blendShapeBoneSelf.Calculate(cmd);
        }

        public void RenderResult(RenderTexture rt)
        {
            material.SetTexture("_MainTex", rt);
        }

        private void OnDestroy()
        {
            blendShapeBoneSelf.ReleaseBuffers();
        }

        private void OnDrawGizmos()
        {
            if (drawSceneGizmo)
            {
                DrawTransform();
            }
            
        }

        private void DrawTransform()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(WorldOriginUVN, 0.002f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(WorldOriginUVN, WorldOriginUVN + WorldRotationUVN * Vector3.right * 0.05f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(WorldOriginUVN, WorldOriginUVN + WorldRotationUVN * Vector3.up * 0.05f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(WorldOriginUVN, WorldOriginUVN + WorldRotationUVN * Vector3.forward * 0.05f);
        }
    }
}
