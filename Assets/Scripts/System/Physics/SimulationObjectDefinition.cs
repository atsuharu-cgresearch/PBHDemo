using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    public class SimulationObjectDefinition
    {
        public string type;
        public Vector2[] particles;
        public int[] distConstIndices;
        public int[] areaConstIndices;
        public int[] shapeMatchIndices;
        public int[] shapeMatchCounts;

        public int[] edgeIndices;

        public Vector3[] meshVertices;
        public int[] meshTriangles;
        public int[] vToPReferences;
    }
}
