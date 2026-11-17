// Put on a HELIOS shell button or card: HeliosShell wires its click from these values at Play.
using UnityEngine;

namespace IETM
{
    /// <summary>Put on any shell button / card: HeliosShell wires the click from these values.</summary>
    public class ShellTarget : MonoBehaviour
    {
        public ShellAction action;
        [Tooltip("System page id (PageSystem), IETM module id (OpenIetm), filter id (Filter), StreamingAssets file (Download) or a label (NotInPoc).")]
        public string argument;
        [Tooltip("Filter group of a system card ('airframe', 'powerplant', ...). Empty = always shown.")]
        public string group;
        [Tooltip("Display name (toasts, IETM breadcrumb).")]
        public string label;
    }

}
