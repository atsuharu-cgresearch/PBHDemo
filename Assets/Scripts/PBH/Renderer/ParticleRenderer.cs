using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace PBH
{
    /// <summary>
    /// ParticleBufferの位置情報をもとに、オブジェクトをメッシュとしてレンダリングする
    /// </summary>
    public class ParticleRenderer
    {
        private int meshCount;

        private MeshDirectRenderer meshDirectRenderer;

        public ParticleRenderer(SimulationObjectDefinition[] defs, ComputeBuffer particles, ParticleRange[] references)
        {
            meshCount = defs.Length;

            Mesh[] meshArray = new Mesh[meshCount];
            Material[] materialArray = new Material[meshCount];

            for (int i = 0; i < meshCount; i++)
            {
                meshArray[i] = CreateMesh(defs[i]);

                materialArray[i] = CreateMaterial(particles, references[i]);
            }

            // 描画実行クラスのインスタンスを作成
            meshDirectRenderer = new MeshDirectRenderer(meshArray, materialArray);
        }

        private Mesh CreateMesh(SimulationObjectDefinition def)
        {
            Mesh mesh = new Mesh();

            // 頂点座標
            mesh.SetVertices(def.meshVertices);

            // 三角面のインデックス
            mesh.SetTriangles(def.meshTriangles, 0);

            // 頂点シェーダーで、各頂点が参照するパーティクルのインデックスをUV2に持たせる
            Vector2[] uv2 = new Vector2[def.vToPReferences.Length];
            for (int j = 0; j < uv2.Length; j++)
            {
                uv2[j] = new Vector2(def.vToPReferences[j], 0);
            }
            mesh.SetUVs(1, uv2);

            // 念のため入れておく
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }

        private Material CreateMaterial(ComputeBuffer particles, ParticleRange reference)
        {
            Material mat = new Material(Shader.Find("PBH/ParticleRendererMesh"));

            mat.SetBuffer("_Particles", particles);
            mat.SetInt("_Start", reference.start);

            return mat;
        }

        
        public void Render(ref RenderTexture rt, Transform2D textureTransform)
        {
            Vector2 camPos = textureTransform.pos;

            float camSize = textureTransform.scale / 2;

            float camRot = textureTransform.rot;

            meshDirectRenderer.DrawMesh(ref rt, camPos, camRot, camSize);
            // meshCameraRenderer.DrawMesh(ref rt, camPos, camRot, camSize);
        }

        public void Release()
        {
            meshDirectRenderer.Release();
        }
    }
}