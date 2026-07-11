using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    public class BodyCreator
    {
        public struct ObjectData
        {
            public SimulationObjectDefinition def;
            public Vector4 initTransform;
            public int layer;
        }

        // シミュレーションを実行するオブジェクトをここに登録しておく
        private List<ObjectData> objectDataList = new List<ObjectData>();

        // 1つのスレッドグループに64スレッドを割り当てるので、別のオブジェクトが同じスレッドグループ内に共存しないように64の倍数でパディングする
        private const int PADDING_MULTIPLE = 64;

        /// <summary>
        /// ObjectDataのリストを、1つのバッファにまとめるための補助クラス
        /// </summary>
        private class AggregateData
        {
            public List<Vector2> positionsInit = new List<Vector2>();
            public List<Vector2> positionsLocal = new List<Vector2>();
            public List<int> myObjectIndices = new List<int>();
            public List<int> myLayerIndices = new List<int>();

            public List<int> areaIndices = new List<int>();
            public List<AreaConstraint.Cluster> areaClusters = new List<AreaConstraint.Cluster>();
            
            public List<int> shapeMatchIndices = new List<int>();
            public List<ShapeMatchConstraint.Cluster> shapeMatchClusters = new List<ShapeMatchConstraint.Cluster>();

            public List<ParticleRange> objToParticles = new List<ParticleRange>();

            public AggregateData(List<ObjectData> objectDataList)
            {
                int pOffset = 0;

                for (int i = 0; i < objectDataList.Count; i++)
                {
                    var def = objectDataList[i].def;
                    int pCount = def.particles.Length;

                    for (int j = 0; j < pCount; j++)
                    {
                        positionsInit.Add(LocalToWorld(def.particles[j], objectDataList[i].initTransform));
                        positionsLocal.Add(def.particles[j]);
                        myObjectIndices.Add(i);
                        myLayerIndices.Add(objectDataList[i].layer);
                    }

                    if (def.areaConstIndices.Length > 0)
                    {
                        int clusterCount = def.areaConstIndices.Length / 3;
                        
                        for (int j = 0; j < clusterCount; j++)
                        {
                            int currentPStart = areaIndices.Count;

                            int id0 = def.areaConstIndices[j * 3 + 0] + pOffset;
                            int id1 = def.areaConstIndices[j * 3 + 1] + pOffset;
                            int id2 = def.areaConstIndices[j * 3 + 2] + pOffset;

                            areaIndices.Add(id0);
                            areaIndices.Add(id1);
                            areaIndices.Add(id2);

                            Vector2 p0 = positionsInit[id0];
                            Vector2 p1 = positionsInit[id1];
                            Vector2 p2 = positionsInit[id2];
                            float restArea = 0.5f * Cross2D(p1 - p0, p2 - p0);

                            areaClusters.Add(new AreaConstraint.Cluster
                            {
                                restArea = restArea,
                                pStart = currentPStart
                            });
                        }
                    }

                    if (def.shapeMatchIndices.Length > 0)
                    {
                        int clusterCount = def.shapeMatchCounts.Length;

                        int paddedClusterCount = Mathf.CeilToInt(clusterCount / (float)PADDING_MULTIPLE) * PADDING_MULTIPLE;

                        int offset = 0;

                        for (int j = 0; j < paddedClusterCount; j++)
                        {
                            int currentPStart = shapeMatchIndices.Count;

                            if (j < clusterCount)
                            {
                                int count = def.shapeMatchCounts[j];

                                shapeMatchClusters.Add(new ShapeMatchConstraint.Cluster
                                {
                                    objPOffset = pOffset,
                                    objPCount = pCount,
                                    pStart = currentPStart,
                                    pCount = count
                                });

                                for (int k = 0; k < count; k++)
                                {
                                    shapeMatchIndices.Add(def.shapeMatchIndices[offset] + pOffset);
                                    offset++;
                                }
                            }
                            
                            else
                            {
                                shapeMatchClusters.Add(new ShapeMatchConstraint.Cluster
                                {
                                    objPOffset = pOffset,
                                    objPCount = pCount,
                                    pStart = currentPStart,
                                    pCount = 0
                                });
                            }
                        }
                    }

                    objToParticles.Add(new ParticleRange(pOffset, pCount));
                    pOffset += pCount;
                }
            }

            private Vector2 LocalToWorld(Vector2 pLocal, Vector4 transform)
            {
                Vector2 origin = new Vector2(transform.x, transform.y);
                float scale = transform.z;
                float angle = transform.w;

                Vector2 p = pLocal * scale;
                float c = Mathf.Cos(angle);
                float s = Mathf.Sin(angle);

                return new Vector2(
                    c * p.x - s * p.y + origin.x,
                    s * p.x + c * p.y + origin.y
                );
            }

            private float Cross2D(Vector2 v0, Vector2 v1)
            {
                return v0.x * v1.y - v0.y * v1.x;
            }
        }

        /// <summary>
        /// 外部のクラスがこのメソッドを使ってobjectDataListにオブジェクトを登録する
        /// </summary>
        public void AddElement(SimulationObjectDefinition def, Vector4 initTransform, int layer, out int keyGetParticles)
        {
            objectDataList.Add(new ObjectData
            {
                def = def,
                initTransform = initTransform,
                layer = layer
            });

            keyGetParticles = objectDataList.Count - 1;
        }

        public Body CreateBody(int colliderTexSize)
        {
            if (objectDataList.Count == 0) return null;

            var aggregate = new AggregateData(objectDataList);

            int numParticles = aggregate.positionsInit.Count;
            ParticleData[] particles = new ParticleData[numParticles];
            for (int i = 0; i < numParticles; i++)
            {
                float mass = 1f;
                particles[i] = new ParticleData(aggregate.positionsInit[i], mass);
            }

            ComputeBuffer particleBuffer = ComputeHelper.CreateStructuredBuffer(particles);
            ComputeBuffer objIndexBuffer = ComputeHelper.CreateStructuredBuffer(aggregate.myObjectIndices.ToArray());
            ComputeBuffer layerBuffer = ComputeHelper.CreateStructuredBuffer(aggregate.myLayerIndices.ToArray());
            ComputeBuffer localPosBuffer = ComputeHelper.CreateStructuredBuffer(aggregate.positionsLocal.ToArray());

            var areaConstraint = aggregate.areaIndices.Count > 0 ?
                new AreaConstraint(particleBuffer, aggregate.areaIndices.ToArray(), aggregate.areaClusters.ToArray()) : null;
            var shapeConstraint = aggregate.shapeMatchIndices.Count > 0 ?
                new ShapeMatchConstraint(particleBuffer, particles, aggregate.shapeMatchIndices.ToArray(), aggregate.shapeMatchClusters.ToArray()) : null;
            var collisionConstraint = new CollisionSolver(colliderTexSize);
            var targetPosConst = new TargetPosSolver();
            var targetPosForce = new TargetPosForce();

            return new Body(
                particleBuffer, localPosBuffer, objIndexBuffer, layerBuffer,
                areaConstraint, shapeConstraint,
                collisionConstraint, targetPosConst, targetPosForce,
                aggregate.objToParticles.ToArray()
            );
        }
    }
}