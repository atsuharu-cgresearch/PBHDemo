using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    // デバッグ用のメソッドだけ切り出してここに記述している
    public partial class HighlightSystem
    {
        /// <summary>
        /// 指定したスロットのパーティクルバッファと範囲情報を取得する
        /// </summary>
        public bool TryGetDebugData(int slotIndex, out ComputeBuffer buffer, out ParticleRange[] ranges, out Transform2D[] targetTransforms, out Transform2D colliderTransform, out RenderTexture colliderTexture)
        {
            buffer = null;
            ranges = null;
            targetTransforms = null;
            colliderTransform = new Transform2D();
            colliderTexture = null;

            // 初期化前や範囲外のアクセスを防ぐ
            if (simulator == null || simulator.DataPool == null) return false;
            if (slotIndex < 0 || slotIndex >= numSlots) return false;

            buffer = simulator.DataPool.ParticleBuffer;

            int[] keys = simInputCalculators[slotIndex].objKeys;
            ranges = new ParticleRange[keys.Length];
            targetTransforms = new Transform2D[keys.Length];
            for (int j = 0; j < keys.Length; j++)
            {
                ranges[j] = simulator.DataPool.GetParticleReference(keys[j]);
                targetTransforms[j] = simInputCalculators[slotIndex].targetTransforms[j];
            }

            colliderTransform = simInputCalculators[slotIndex].textureTransform;

            colliderTexture = simInputCalculators[slotIndex].colliderTexture;

            return true;
        }

        /// <summary>
        /// インデックスを指定して、オブジェクトの現在の位置と姿勢を取得する
        /// </summary>
        public bool TryGetTargetData(int slotIndex, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            if (slotIndex < 0 || slotIndex >= numSlots) return false;

            pos = input.slots[slotIndex].target.WorldOriginUVN;
            rot = input.slots[slotIndex].target.WorldRotationUVN;

            return true;
        }

        public int GetSlotCount()
        {
            return numSlots;
        }

        public TargetMesh GetTargetMesh(int slotIndex)
        {
            return input.slots[slotIndex].target;
        }
    }

}