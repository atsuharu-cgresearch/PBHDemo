using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    /// <summary>
    /// UVマップ/マスク生成モジュールの共通インターフェース
    /// </summary>
    public interface IUVMapGenerator
    {
        /// <summary>
        /// UVマップ（またはマスク）を生成・取得する
        /// </summary>
        RenderTexture Generate(Vector3 camPos);

        /// <summary>
        /// GPUメモリを解放する
        /// </summary>
        void Release();
    }
}
