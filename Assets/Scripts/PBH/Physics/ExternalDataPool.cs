using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    /// <summary>
    /// Simulatorクラスから分離して、Input、Simulator、Renderer間でやり取りされるデータを管理する
    /// </summary>
    public class ExternalDataPool
    {
        // SimulationInputCalculatorが書き込んで、Simulatorへ入力するデータ
        public RenderTexture[] SDFArray { get; private set; }
        public Transform2D[] ColliderTransforms { get; private set; }
        public Transform2D[] TargetPosTransforms { get; private set; }

        
        // Simulatorが出力し、SimulationResultRendererへ入力するデータ
        public ComputeBuffer ParticleBuffer { get; private set; }
        // Rendererからキーで参照するための配列
        private ParticleRange[] particleReferences;


        public ExternalDataPool(int maxLayers, int maxObjects)
        {
            SDFArray = new RenderTexture[maxLayers];
            ColliderTransforms = new Transform2D[maxLayers];
            TargetPosTransforms = new Transform2D[maxObjects];
        }

        // Input Setter
        public void SetCollider(RenderTexture collider, Transform2D transform, int index)
        {
            SDFArray[index] = collider;
            ColliderTransforms[index] = transform;
        }

        public void SetTargetPosOffset(Transform2D transform, int index)
        {
            TargetPosTransforms[index] = transform;
        }

        // Output Setter
        // Simulatorが初期化時に呼び出す
        public void SetSimulationOutputs(ComputeBuffer buffer, ParticleRange[] references)
        {
            ParticleBuffer = buffer;
            particleReferences = references;
        }

        // Output Getter
        public ParticleRange GetParticleReference(int key)
        {
            return particleReferences[key];
        }
    }
}