using UnityEngine;

/// <summary>
/// Optional marker. Put it on the root of a multi-part model (e.g. a "Table" with
/// legs and a top) so double-clicking any child isolates the whole model instead
/// of just the part that was clicked.
/// </summary>
[DisallowMultipleComponent]
public class IsolateGroup : MonoBehaviour { }
