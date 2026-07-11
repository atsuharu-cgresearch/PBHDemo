using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBH
{
    /// <summary>
    /// UVマップ/マスク生成モジュールの共通インターフェース
    /// </summary>
    public interface IUVMaskGenerator
    {
        /// <summary>
        /// UVマップ（またはマスク）を生成・取得する
        /// </summary>
        RenderTexture Generate(Vector3 camPos, CommandBuffer cmd);

        /// <summary>
        /// GPUメモリを解放する
        /// </summary>
        void Release();
    }
}
