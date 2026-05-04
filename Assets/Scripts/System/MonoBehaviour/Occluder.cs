using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    /// <summary>
    /// SkinnedMeshRendererを監視し、現在のまぶたの形状を計算・保持するクラス
    /// </summary>
    public class Occluder : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer srcSmr;
        [SerializeField] private Transform myBone;
        private SkinnedMeshRenderer smr;
        private BlendShapeBonesSelf blendShapeBoneSelf;

        public Vector2[] MeshUVs { get; private set; }
        public int[] MeshTriangles { get; private set; }

        private void Awake()
        {
            smr = GetComponent<SkinnedMeshRenderer>();

            CopyBlendShapes(srcSmr, smr);

            MeshUVs = (Vector2[])smr.sharedMesh.uv.Clone();
            MeshTriangles = (int[])smr.sharedMesh.triangles.Clone();

            blendShapeBoneSelf = new BlendShapeBonesSelf(smr, myBone);
        }

        private void Update()
        {
            CopyBlendShapes(srcSmr, smr);
        }

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
        public ComputeBuffer CalculateBlendShapeBoneSelf()
        {
            return blendShapeBoneSelf.Calculate();
        }

        private void OnDestroy()
        {
            blendShapeBoneSelf.ReleaseBuffers();
        }
    }
}
