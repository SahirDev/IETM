// IETM data model (S1000D / ATA iSpec 2200 inspired) for the Landing Gear IETM POC.
// Loaded from Resources/IETM/landing_gear_ietm.json with JsonUtility.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IETM
{
    [Serializable]
    public class PublicationInfo
    {
        public string title;
        public string subtitle;
        public string pmc;
        public string issue;
        public string issueDate;
        public string applicability;
        public string dataStatus;
    }

    [Serializable]
    public class FigureInfo
    {
        public string title;
        public string icn;
        public string figureNumber;
    }

    [Serializable]
    public class IPDItem
    {
        public string itemNo;
        public string isn;
        public string cage;
        public string partNumber;
        public string description;
        public int indent = 1;
        public string upa;
        public string smr;
        public string[] nodes;
        public string info;
        public string remarks;

        // Runtime (not serialized from JSON)
        [NonSerialized] public IPDItem parent;
        [NonSerialized] public List<IPDItem> children = new List<IPDItem>();
        [NonSerialized] public int index;

        public bool IsGroup => children != null && children.Count > 0;

        /// <summary>Item number as shown in callouts: "010" -> "10".</summary>
        public string CalloutNo
        {
            get
            {
                var t = (itemNo ?? "").TrimStart('0');
                return string.IsNullOrEmpty(t) ? itemNo : t;
            }
        }

        /// <summary>IPC style indented nomenclature: ". . LINK - STEERING".</summary>
        public string IndentedDescription
        {
            get
            {
                var dots = "";
                for (int i = 1; i < indent; i++) dots += ". ";
                return dots + description;
            }
        }

        public void CollectLeaves(List<IPDItem> into)
        {
            if (!IsGroup) { into.Add(this); return; }
            foreach (var c in children) c.CollectLeaves(into);
        }

        public bool IsDescendantOf(IPDItem other)
        {
            var p = parent;
            while (p != null) { if (p == other) return true; p = p.parent; }
            return false;
        }
    }

    [Serializable]
    public class Paragraph
    {
        public string heading;
        public string text;
        public string[] focus;
        [Tooltip("Optional file under StreamingAssets (e.g. IETM/32-20-00_Fig1_IPC.pdf) opened by the card's button.")]
        public string link;
        public string linkLabel;
    }

    [Serializable]
    public class ProcStep
    {
        public string title;
        public string text;
        public string warning;
        public string caution;
        public string[] focus;
        /// <summary>
        /// highlight | isolate | remove | restore |
        /// operate (isolate the focus parts in their own colours, the rest ghosted) |
        /// detach (remove the focus parts and lay them out, still visible) | install (put detached parts back)
        /// </summary>
        public string action;
        /// <summary>cycle = play gear retraction / extension in a loop while the step is shown | retract | extend | (empty)</summary>
        public string animate;
        /// <summary>iso | front | side | top | (empty = keep)</summary>
        public string view;
        /// <summary>extended | retracted | (empty = keep)</summary>
        public string gear;
        /// <summary>animate = spin: IPD items whose parts turn about the model's main axis while the step is shown (engine rotors).</summary>
        public string[] spin;
    }

    [Serializable]
    public class DataModule
    {
        public string id;
        public string group;
        /// <summary>description | ipd | procedure | document (a file under StreamingAssets, see 'file')</summary>
        public string type;
        /// <summary>document modules: file under StreamingAssets that is downloaded / opened when clicked.</summary>
        public string file;
        public string dmc;
        public string title;
        public string infoName;
        public string[] focus;
        public Paragraph[] paragraphs;
        public ProcStep[] steps;

        public string FullTitle => title + " - " + infoName;
    }

    /// <summary>
    /// Designed exploded-view travel of one model node (extended pose, 100 % explode).
    /// 'offset' is in the glTF parent space of the node (metres, glTF axes); it is converted to Unity
    /// (x mirrored, as glTFast does) and to world space through the node's parent at run time.
    /// </summary>
    [Serializable]
    public class ExplodeOffset
    {
        public string node;
        public float[] offset;
    }

    [Serializable]
    public class IETMDatabase
    {
        public PublicationInfo publication;
        public FigureInfo figure;
        public List<IPDItem> items = new List<IPDItem>();
        public List<DataModule> modules = new List<DataModule>();
        [Tooltip("Mechanically laid-out exploded view (collision-free). Empty = simple radial explode.")]
        public List<ExplodeOffset> explode = new List<ExplodeOffset>();
        [Tooltip("Where each part is laid out when a procedure removes it (disassembly order, collision-free).")]
        public List<ExplodeOffset> removal = new List<ExplodeOffset>();
        [Tooltip("Corner of the L-shaped removal path (first move, then on to the laid-out position). Missing = automatic: first along the main axis of the removal.")]
        public List<ExplodeOffset> removalVia = new List<ExplodeOffset>();
        [Tooltip("true = every part without its own corner gets an automatic L path; false = only parts listed in removalVia move in an L, the rest straight.")]
        public bool removalLPath = false;

        [NonSerialized] public Dictionary<string, IPDItem> byItemNo = new Dictionary<string, IPDItem>();

        public static IETMDatabase Load(TextAsset json)
        {
            if (json == null)
            {
                Debug.LogError("[IETM] No data file assigned / found in Resources/IETM.");
                return new IETMDatabase { publication = new PublicationInfo { title = "No data" }, figure = new FigureInfo() };
            }
            var db = JsonUtility.FromJson<IETMDatabase>(json.text);
            db.BuildTree();
            return db;
        }

        /// <summary>Builds parent/child relations from the IPC indent levels.</summary>
        public void BuildTree()
        {
            byItemNo.Clear();
            var stack = new List<IPDItem>();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                it.index = i;
                it.children = new List<IPDItem>();
                if (it.nodes == null) it.nodes = new string[0];
                while (stack.Count > 0 && stack[stack.Count - 1].indent >= it.indent) stack.RemoveAt(stack.Count - 1);
                it.parent = stack.Count > 0 ? stack[stack.Count - 1] : null;
                if (it.parent != null) it.parent.children.Add(it);
                stack.Add(it);
                if (!string.IsNullOrEmpty(it.itemNo)) byItemNo[it.itemNo] = it;
            }
        }

        public IPDItem Find(string itemNo)
        {
            if (string.IsNullOrEmpty(itemNo)) return null;
            byItemNo.TryGetValue(itemNo, out var it);
            return it;
        }

        public IEnumerable<IPDItem> Leaves()
        {
            foreach (var it in items) if (!it.IsGroup) yield return it;
        }
    }

    [Serializable]
    public class CartLine
    {
        public IPDItem item;
        public int qty;
    }
}
