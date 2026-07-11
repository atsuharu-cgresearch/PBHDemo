using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBH
{
    public enum HighlightType
    {
        Circle,
        circle_double_001,
        circle_002,
        circle_003,
        circle_double_002,
        CircleOuterRefined32,
        Snowman,
        snowman_002,
        original,
        original002,
        original003,
        star,
    }

    public static class SimulationObjectDatabase
    {
        private static Dictionary<HighlightType, SimulationObjectDefinition> cache;

        /// <summary>
        /// HighlightTypeをキーとして、対応するSimulationObjectDefinitionを取得する
        /// </summary>
        public static SimulationObjectDefinition Load(HighlightType type)
        {
            if (cache == null) LoadAll();

            return cache[type];
        }

        /// <summary>
        /// jsonファイルから、事前に作成したSimulationObjectDefinitionを取得して、辞書に定義する
        /// </summary>
        private static void LoadAll()
        {
            cache = new Dictionary<HighlightType, SimulationObjectDefinition>();

            var jsons = Resources.LoadAll<TextAsset>("Json/SimObjDefs");

            foreach (var j in jsons)
            {
                var def = JsonUtility.FromJson<SimulationObjectDefinition>(j.text);
                var type = Enum.Parse<HighlightType>(def.type);
                cache[type] = def;
            }
        }
    }
}
