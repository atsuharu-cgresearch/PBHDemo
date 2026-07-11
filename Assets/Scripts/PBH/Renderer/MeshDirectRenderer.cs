using UnityEngine;
using UnityEngine.Rendering;

namespace PBH
{
    /// <summary>
    /// CommandBufferを使ってメッシュを描画し、RenderTextureに結果を描き込む
    /// </summary>
    public class MeshDirectRenderer
    {
        private Mesh[] meshes;
        private Material[] materials;

        private CommandBuffer cmd;

        public MeshDirectRenderer(Mesh[] meshArray, Material[] materialArray)
        {
            meshes = meshArray;

            materials = materialArray;

            cmd = new CommandBuffer { name = "DrawHighlightMeshes" };
        }

        public void DrawMesh(ref RenderTexture rt, Vector2 pos, float rotationZ, float camSize)
        {
            cmd.Clear();

            cmd.SetRenderTarget(rt);
            cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.BeginSample("PBD_Direct_DrawMesh");

            Matrix4x4 ortho = Matrix4x4.Ortho(-camSize, camSize, -camSize, camSize, -100f, 100f);
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(ortho, false); // trueにすると自動で上下反転してくれるが今回は必要ない

            Quaternion rotation = Quaternion.Euler(0, 0, rotationZ);
            Matrix4x4 cameraTRS = Matrix4x4.TRS(new Vector3(pos.x, pos.y, -1f), rotation, Vector3.one);

            Matrix4x4 viewMatrix = cameraTRS.inverse;

            cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);

            for (int i = 0; i < meshes.Length; i++)
            {
                cmd.DrawMesh(meshes[i], Matrix4x4.identity, materials[i], 0, 0);
            }

            cmd.EndSample("PBD_Direct_DrawMesh");
            Graphics.ExecuteCommandBuffer(cmd);

            cmd.Clear();
        }

        public void Release()
        {
            if (cmd != null)
            {
                cmd.Release();
                cmd = null;
            }
        }
    }
}