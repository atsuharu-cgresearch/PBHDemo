using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using PBH;

namespace PBH.Precompute
{
    /// <summary>
    /// Meshクラスの入力形状から、シミュレーション用のオブジェクトを定義する
    /// </summary>
    public class BodyAssetGenerator : MonoBehaviour
    {
        [Header("入力データ")]
        public Mesh mesh;
        public GameObject vertexPrefab;

        [Header("出力設定")]
        public string fileNameIncludeDotJson = "Body.json";
        public string objName = "None";

        [Header("デバッグ描画")]
        public bool drawAreaConst = true;
        public int drawingAreaKey = -1;
        public bool drawShapeMatch = true;
        public int drawingShapeMatchKey = -1;

        // パーティクルデータ
        private Vector3[] verticesUnique;
        private List<Vector2> positionList = new List<Vector2>();

        // 選択状態
        private List<int> selectedIndexList = new List<int>();

        // 拘束データ
        private List<int> areaConstIndexList = new List<int>();
        private List<int> shapeMatchIndexList = new List<int>();
        private List<int> shapeMatchCountList = new List<int>();
        private List<int> edgeIndexList = new List<int>();

        // メッシュの生データ保存用
        private Vector3[] meshVertices;
        private int[] meshTriangles;
        private List<int> vToPReferenceList = new List<int>();

        public float gizmoSize = 0.005f;

        private void Start()
        {
            // 頂点の重複を削除してパーティクル（uniqueList）を生成する
            List<Vector3> uniqueList = new List<Vector3>();

            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < uniqueList.Count; j++)
                {
                    // 同一座標の頂点をマージ
                    float sqrDist = Vector3.SqrMagnitude(mesh.vertices[i] - uniqueList[j]);
                    if (sqrDist < 1e-6f)
                    {
                        vToPReferenceList.Add(j);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    vToPReferenceList.Add(uniqueList.Count);
                    uniqueList.Add(mesh.vertices[i]);
                }
            }

            verticesUnique = uniqueList.ToArray();
            Debug.Log($"重複なし頂点（パーティクル）数: {verticesUnique.Length}");

            // パーティクル位置リストに登録し、クリック判定用のPrefabを配置
            for (int i = 0; i < verticesUnique.Length; i++)
            {
                positionList.Add(verticesUnique[i]);

                if (vertexPrefab != null)
                {
                    GameObject vertexObj = Instantiate(vertexPrefab, verticesUnique[i], Quaternion.identity, this.transform);
                    var collider = vertexObj.GetComponent<VertexCollider>();
                    if (collider != null) collider.index = i;
                }
            }

            meshVertices = (Vector3[])mesh.vertices.Clone();
            meshTriangles = (int[])mesh.triangles.Clone();

            // ShapeMatchingクラスタの生成（三角面＋隣接三角面の頂点）
            GenerateTopologyBasedShapeMatching();
        }

        /// <summary>
        /// メッシュのトポロジーを解析し、隣接する三角面からShapeMatchingクラスタを生成する
        /// </summary>
        private void GenerateTopologyBasedShapeMatching()
        {
            shapeMatchIndexList.Clear();
            shapeMatchCountList.Clear();

            // メッシュのtriangles（頂点インデックス）をパーティクルインデックスに変換
            int[] pTriangles = new int[meshTriangles.Length];
            for (int i = 0; i < meshTriangles.Length; i++)
            {
                pTriangles[i] = vToPReferenceList[meshTriangles[i]];
            }

            int numTris = pTriangles.Length / 3;

            // 各辺（エッジ）がどの三角面に属しているかを記録する辞書
            // エッジは2つの頂点インデックスをソートしてlong型のキーにする
            Dictionary<long, List<int>> edgeToTriangles = new Dictionary<long, List<int>>();

            long GetEdgeKey(int v1, int v2)
            {
                if (v1 > v2) { int temp = v1; v1 = v2; v2 = temp; }
                return ((long)v1 << 32) | (uint)v2;
            }

            // 全ての三角面をループして辺情報を登録
            for (int t = 0; t < numTris; t++)
            {
                int v0 = pTriangles[t * 3 + 0];
                int v1 = pTriangles[t * 3 + 1];
                int v2 = pTriangles[t * 3 + 2];

                long[] edges = { GetEdgeKey(v0, v1), GetEdgeKey(v1, v2), GetEdgeKey(v2, v0) };

                foreach (long edge in edges)
                {
                    if (!edgeToTriangles.ContainsKey(edge))
                        edgeToTriangles[edge] = new List<int>();

                    edgeToTriangles[edge].Add(t);
                }
            }

            // 各三角面ごとにクラスタを生成
            for (int t = 0; t < numTris; t++)
            {
                int v0 = pTriangles[t * 3 + 0];
                int v1 = pTriangles[t * 3 + 1];
                int v2 = pTriangles[t * 3 + 2];

                // クラスタ内の頂点の重複を防ぐためにHashSetを使用
                HashSet<int> clusterVerts = new HashSet<int> { v0, v1, v2 };

                long[] edges = { GetEdgeKey(v0, v1), GetEdgeKey(v1, v2), GetEdgeKey(v2, v0) };

                // この三角面の各辺を共有している他の三角面を探す
                foreach (long edge in edges)
                {
                    foreach (int adjT in edgeToTriangles[edge])
                    {
                        // 隣接する三角面の3頂点をクラスタに追加（重複は自動で無視される）
                        clusterVerts.Add(pTriangles[adjT * 3 + 0]);
                        clusterVerts.Add(pTriangles[adjT * 3 + 1]);
                        clusterVerts.Add(pTriangles[adjT * 3 + 2]);
                    }
                }

                // 生成したクラスタをリストに追加
                shapeMatchCountList.Add(clusterVerts.Count);
                shapeMatchIndexList.AddRange(clusterVerts);
            }

            Debug.Log($"ShapeMatchingクラスタを {numTris} 個生成しました。");
        }

        private void Update()
        {
            // マウスクリックで外周パーティクルを選択
            if (Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    var vertexCollider = hit.collider.gameObject.GetComponent<VertexCollider>();
                    if (vertexCollider != null)
                    {
                        int index = vertexCollider.index;
                        if (!selectedIndexList.Contains(index))
                        {
                            selectedIndexList.Add(index);
                        }
                    }
                }
            }

            // スペースキーで面積拘束クラスタを追加
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddBody();
            }

            // エンターキーでエクスポート
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Export();
            }
        }

        private void AddBody()
        {
            if (selectedIndexList.Count < 3)
            {
                Debug.LogWarning("面積拘束を作成するには、最低3つのパーティクルを選択してください。");
                return;
            }

            // 面積拘束を追加
            List<int> newAreaConstIndexList = new List<int>();

            for (int i = 0; i < selectedIndexList.Count; i++)
            {
                int id0 = i;
                int skip = 1;

                while (true)
                {
                    if (skip > selectedIndexList.Count / 3) break;

                    int id1 = (id0 + skip + selectedIndexList.Count) % selectedIndexList.Count;
                    int id2 = (id0 + skip * 2 + selectedIndexList.Count) % selectedIndexList.Count;

                    newAreaConstIndexList.Add(selectedIndexList[id0]);
                    newAreaConstIndexList.Add(selectedIndexList[id1]);
                    newAreaConstIndexList.Add(selectedIndexList[id2]);

                    skip++;
                }
            }

            // 描画用のエッジインデックス
            List<int> newEdgeIndexList = new List<int>();
            for (int i = 0; i < selectedIndexList.Count; i++)
            {
                newEdgeIndexList.Add(selectedIndexList[i]);
                newEdgeIndexList.Add((i + 1 < selectedIndexList.Count) ? selectedIndexList[i + 1] : selectedIndexList[0]);
            }

            this.areaConstIndexList.AddRange(newAreaConstIndexList);
            this.edgeIndexList.AddRange(newEdgeIndexList);

            Debug.Log($"面積拘束を追加しました。現在の選択をクリアします。");
            selectedIndexList.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (mesh == null || positionList.Count == 0) return;

            DrawSelectedVertices();
            DrawParticles();
            if (drawAreaConst) DrawAreaConstraints();
            if (drawShapeMatch) DrawShapeMatch();
        }

        private void DrawSelectedVertices()
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;

            for (int i = 0; i < selectedIndexList.Count; i++)
            {
                Handles.Label(verticesUnique[selectedIndexList[i]] + Vector3.up * 0.05f, i.ToString(), style);
            }
        }

        private void DrawParticles()
        {
            Gizmos.color = Color.green;
            // メッシュの頂点を描画
            for (int i = 0; i < positionList.Count; i++)
            {
                Gizmos.DrawSphere(positionList[i], gizmoSize);
            }
            // メッシュの三角形の辺を描画
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector3 p0 = mesh.vertices[mesh.triangles[i + 0]];
                Vector3 p1 = mesh.vertices[mesh.triangles[i + 1]];
                Vector3 p2 = mesh.vertices[mesh.triangles[i + 2]];

                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p0);
            }
        }

        private void DrawAreaConstraints()
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < areaConstIndexList.Count; i += 3)
            {
                if (drawingAreaKey < 0 || i == drawingAreaKey * 3)
                {
                    Vector3 p0 = positionList[areaConstIndexList[i + 0]];
                    Vector3 p1 = positionList[areaConstIndexList[i + 1]];
                    Vector3 p2 = positionList[areaConstIndexList[i + 2]];

                    Gizmos.DrawLine(p0, p1);
                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawLine(p2, p0);
                }
            }
        }

        private void DrawShapeMatch()
        {
            Gizmos.color = Color.cyan;
            int pOffset = 0;
            for (int i = 0; i < shapeMatchCountList.Count; i++)
            {
                int count = shapeMatchCountList[i];

                if (drawingShapeMatchKey < 0 || i == drawingShapeMatchKey)
                {
                    // クラスタ内の頂点を全て結んで描画する（視覚化用）
                    for (int j = 0; j < count; j++)
                    {
                        Vector3 p0 = positionList[shapeMatchIndexList[pOffset + j]];
                        Vector3 p1 = positionList[shapeMatchIndexList[(j <= count - 2) ? pOffset + j + 1 : pOffset]];
                        Gizmos.DrawLine(p0, p1);
                    }
                }
                pOffset += count;
            }
        }
#endif

        private void Export()
        {
            var wrapper = new SimulationObjectDefinition();
            wrapper.type = objName;

            // 互換性のため Vector3 を Vector2 にキャストして格納（元のコードの仕様）
            List<Vector2> exportParticles = new List<Vector2>();
            foreach (var p in positionList) exportParticles.Add(p);

            wrapper.particles = exportParticles.ToArray();
            wrapper.areaConstIndices = areaConstIndexList.ToArray();
            wrapper.shapeMatchIndices = shapeMatchIndexList.ToArray();
            wrapper.shapeMatchCounts = shapeMatchCountList.ToArray();
            wrapper.edgeIndices = edgeIndexList.ToArray();

            wrapper.meshVertices = meshVertices;
            wrapper.meshTriangles = meshTriangles;
            wrapper.vToPReferences = vToPReferenceList.ToArray();

            string json = JsonUtility.ToJson(wrapper, true);
            string allPath = Path.Combine(Application.dataPath, fileNameIncludeDotJson);
            File.WriteAllText(allPath, json);
            Debug.Log("書き出し完了。出力先：" + allPath);
        }
    }
}
