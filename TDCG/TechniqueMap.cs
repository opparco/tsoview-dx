using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.IO;

namespace TDCG
{
    /// techmapを扱います。
public class TechniqueMap
{
    Dictionary<string, int> technique_map = new Dictionary<string, int>();

    /// techmapを読み込みます。
    public void Load(string path)
    {
        StreamReader file = new StreamReader(path);
        string line;
        while ((line = file.ReadLine()) != null)
        {
            string[] words = line.Split('\t');
            string name = words[0];
            int idx = int.Parse(words[1]);
            technique_map[name] = idx;
        }
        file.Close();
    }

    /// techmapを基にテクニック名に対応するindexを代入します。
    public void AssignTechniqueIndices(TSOFile tso)
    {
        foreach (TSOSubScript sub_script in tso.sub_scripts)
        {
            int idx;
            if (!technique_map.TryGetValue(sub_script.shader.technique_name, out idx))
            {
                Console.WriteLine("name not found in technique-map: " + sub_script.shader.technique_name);
                idx = 0;
            }
            sub_script.shader.technique_idx = idx;
        }
    }
}
}
