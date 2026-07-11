using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    
    public class DirectionalOffsetCalculator
    {
        // メッシュ
        private TargetMesh target;
        // 基準となるローカル方向
        private Vector3 localDirRef = new Vector3(1, 0, 0);
        // 鏡面反射モードかどうか
        private bool useReflection;

        public DirectionalOffsetCalculator(TargetMesh tgt, bool useReflection)
        {
            target = tgt;
            this.useReflection = useReflection;
        }

        /// <summary>
        /// TargetMeshから見た、対象座標へのローカル方向ベクトルを計算
        /// </summary>
        private Vector3 CalcLocalDir(Vector3 worldPos)
        {
            Vector3 targetOrigin = target.WorldOriginUVN;
            Quaternion targetRotation = target.WorldRotationUVN;
            return Vector3.Normalize(Quaternion.Inverse(targetRotation) * (worldPos - targetOrigin));
        }

        /// <summary>
        /// worldPosからTargetMeshへ向かうベクトルを、メッシュの法線で反射した方向を返す
        /// </summary>
        private Vector3 CalcReflectionLocalDir(Vector3 worldPos)
        {
            Vector3 localDir = CalcLocalDir(worldPos);

            // ローカル空間の法線 = (0, 0, 1)
            Vector3 localNormal = new Vector3(0, 0, 1);

            // 第1引数は入射ベクトルなので-1をかける
            return Vector3.Reflect(-localDir, localNormal);
        }

        /// <summary>
        /// 基準方向からのズレをUV空間のオフセットとして計算する
        /// </summary>
        public Vector2 CalcOffset(Vector3 currentWorldPos)
        {
            Vector3 localDirCurr = new Vector3();
            if (!useReflection) localDirCurr = CalcLocalDir(currentWorldPos);
            // 反射モードなら、現在の反射方向を使う
            else localDirCurr = CalcReflectionLocalDir(currentWorldPos);

            // 現在の状態での方向を緯度と経度に分解
            HelperFunction.DirToLatiLong(localDirCurr, out float latiDegCurr, out float longDegCurr);
            // 基準の状態での方向を緯度と経度に分解
            HelperFunction.DirToLatiLong(localDirRef, out float latiDegRef, out float longDegRef);

            // 変化の差分を取って正規化
            float sign = !useReflection ? -1f : 1f;
            float diffU = sign * Mathf.Sin(Mathf.Deg2Rad * Mathf.DeltaAngle(0f, longDegCurr - longDegRef));
            float diffV = sign * Mathf.Sin(Mathf.Deg2Rad * Mathf.DeltaAngle(0f, latiDegCurr - latiDegRef));

            return new Vector2(diffU, diffV);
        }

        /// <summary>
        /// 現在の位置を基準として記憶する
        /// </summary>
        public void Reset(Vector3 worldPos)
        {
            if (!useReflection) localDirRef = CalcLocalDir(worldPos);

            // 反射モードの場合は、リセット時も反射方向を記憶する
            else localDirRef = CalcReflectionLocalDir(worldPos);
        }
    }
}
