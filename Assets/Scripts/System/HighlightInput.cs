using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PBH;

namespace PBH
{
    /// <summary>
    /// 1つのオブジェクトの中に入れるハイライト毎のパラメータを管理するクラス
    /// </summary>
    [System.Serializable]
    public class InputElement
    {
        public HighlightType type = 0;
        public bool inverse = false;
        public Transform2D transform = new Transform2D(new Vector2(0, 0), 0, 1);
    }

    /// <summary>
    /// ハイライトを入れるオブジェクト毎のパラメータを管理するクラス
    /// </summary>
    [System.Serializable]
    public class InputSlot
    {
        public TargetMesh target;
        public Occluder occluder;
        public List<InputElement> elements;
        // [Range(0, 1)] public float response = 0.3f;
        [Range(0f, 0.5f)] public float responseV = 0.3f;
        [Range(0f, 0.5f)] public float responseH = 0.3f;
        [Range(0.1f, 2.0f)] public float curvature = 1.0f;
    }

    public class HighlightInput : MonoBehaviour
    {
        public Transform cameraTransform;
        public Transform lightTransform;
        public PBDSolver.Parameter solverParameter;

        public bool isActive;
        public List<InputSlot> slots;
        public int depthRTSize;
        public int colliderRTSize;
        public int rendererRTSize;

        private readonly int[] validIterations = { 24, 12, 8, 6, 4, 3, 2, 1 };

        // UIManager側から実行し、パラメータ等の変更を適用する
        public float GetResponseH(int index)
        {
            if (slots != null && index >= 0 && index < slots.Count) return slots[index].responseH;
            return 0f;
        }

        public float GetResponseV(int index)
        {
            if (slots != null && index >= 0 && index < slots.Count) return slots[index].responseV;
            return 0f;
        }

        public void SetResponseH(int index, float value)
        {
            if (slots != null && index >= 0 && index < slots.Count) slots[index].responseH = value;
        }

        public void SetResponseV(int index, float value)
        {
            if (slots != null && index >= 0 && index < slots.Count) slots[index].responseV = value;
        }

        public void SetStiffness(float stiffness)
        {
            stiffness = Mathf.Clamp01(stiffness);
            // Debug.Log(stiffness);

            // iterationとsubstepが掛けて24になるようにする
            int index = Mathf.RoundToInt(stiffness * (validIterations.Length - 1));
            index = Mathf.Clamp(index, 0, validIterations.Length - 1);

            int ite = validIterations[index];
            int sub = 24 / ite;

            solverParameter.numIterations = ite;
            solverParameter.numSubsteps = sub;
        }
    }
}
