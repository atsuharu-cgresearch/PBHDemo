using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    /// <summary>
    /// 2DのTransformクラスは用意されていないので定義しておく
    /// </summary>
    [System.Serializable]
    public struct Transform2D
    {
        public Vector2 pos;
        public float rot;
        public float scale;

        public Transform2D(Vector2 p, float r, float s)
        {
            pos = p;
            rot = r;
            scale = s;
        }
    }

    public static class HelperFunction
    {
        #region RenderTexture関係
        /// <summary>
        /// カメラから深度を書き込むためのRenderTextureを作成
        /// </summary>
        public static void CreateCameraTargetDepthRT(ref RenderTexture rt, int width, int height)
        {
            rt = new RenderTexture(width, height, 24, RenderTextureFormat.Depth);
            rt.name = "DepthRT";
            rt.filterMode = FilterMode.Point;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.anisoLevel = 0;
            rt.Create();
        }

        /// <summary>
        /// カメラからカラー情報をARGBFloat形式で書き込むためのRenderTextureを作成
        /// </summary>
        public static void CreateCameraTargetFloat4RT(ref RenderTexture rt, int size)
        {
            rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat);
            rt.name = "Float4RT";
            rt.filterMode = FilterMode.Point;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.anisoLevel = 0;
            rt.Create();
        }
        #endregion

        #region 座標関係
        

        public static void DirToLatiLong(Vector3 direction, out float latitudeDeg, out float longitudeDeg)
        {
            direction.Normalize();

            latitudeDeg = Mathf.Asin(direction.y) * Mathf.Rad2Deg;

            longitudeDeg = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        }
        #endregion
    }
}
