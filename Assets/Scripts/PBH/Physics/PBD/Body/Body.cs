using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    public struct ParticleData
    {
        public Vector2 position;
        public Vector2 predicted;
        public Vector2 velocity;
        public float mass;

        public ParticleData(Vector2 pos, float m)
        {
            position = pos;
            predicted = pos;
            velocity = new Vector2(0, 0);
            mass = m;
        }
    }

    /// <summary>
    /// オブジェクトのパーティクルが、ParticleBufferの何番目から何番目までに登録されているかを知りたいときに使う
    /// </summary>
    public struct ParticleRange
    {
        public int start;
        public int count;

        public ParticleRange(int start, int count)
        {
            this.start = start;
            this.count = count;
        }
    }

    /// <summary>
    /// 物理シミュレーションのオブジェクトのデータをまとめて持つクラス
    /// </summary>
    public class Body
    {
        public ComputeBuffer ParticleBuffer { get; private set; }
        public ComputeBuffer LocalPosBuffer { get; private set; }
        public ComputeBuffer ObjectIndexBuffer { get; private set; }
        public ComputeBuffer LayerBuffer { get; private set; }

        public AreaConstraint AreaConstraint { get; private set; }
        public ShapeMatchConstraint ShapeMatchConstraint { get; private set; }

        public CollisionSolver CollisionSolver { get; private set; }
        public TargetPosSolver TargetPosSolver { get; private set; }
        public TargetPosForce TargetPosForce { get; private set; }

        public ParticleRange[] ObjToParticles { get; private set; }

        /// <summary>
        /// データの中身はBodyCreatorクラスに生成させるので、コンストラクタはは完成したデータを受け取るだけ
        /// </summary>
        public Body(
            ComputeBuffer particles, ComputeBuffer localPositions, ComputeBuffer objIndices, ComputeBuffer layers,
            AreaConstraint areaConst, ShapeMatchConstraint shapeMatchConst,
            CollisionSolver collisionConst, TargetPosSolver targetPosConst, TargetPosForce targetPosForce,
            ParticleRange[] references)
        {
            ParticleBuffer = particles;
            LocalPosBuffer = localPositions;
            ObjectIndexBuffer = objIndices;
            LayerBuffer = layers;

            AreaConstraint = areaConst;
            ShapeMatchConstraint = shapeMatchConst;

            CollisionSolver = collisionConst;
            TargetPosSolver = targetPosConst;
            TargetPosForce = targetPosForce;

            ObjToParticles = references;
        }

        public void ReleaseBuffers()
        {
            ComputeHelper.Release(
                ParticleBuffer,
                LocalPosBuffer,
                ObjectIndexBuffer,
                LayerBuffer
                );

            if (AreaConstraint != null) AreaConstraint.Release();
            if (ShapeMatchConstraint != null) ShapeMatchConstraint.Release();

            if (TargetPosSolver != null) TargetPosSolver.ReleaseBuffers();
            if (TargetPosForce != null) TargetPosForce.ReleaseBuffers();
            if (CollisionSolver != null) CollisionSolver.ReleaseBuffers();
        }
    }
}