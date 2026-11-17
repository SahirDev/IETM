// Marks a model node as (part of) an IPD item. Added to the model by the IETM scene builder.
// Change "Item No" in the Inspector to remap a part to a different IPD item.
using UnityEngine;

namespace IETM
{
    [DisallowMultipleComponent]
    public class IETMPart : MonoBehaviour
    {
        [Tooltip("IPD item number this node belongs to (e.g. 230). Several nodes can share one item.")]
        public string itemNo;

        [Tooltip("For reference only: nomenclature from the IPD data at build time.")]
        public string ipdDescription;

        [Tooltip("For reference only: part number from the IPD data at build time.")]
        public string partNumber;
    }
}
