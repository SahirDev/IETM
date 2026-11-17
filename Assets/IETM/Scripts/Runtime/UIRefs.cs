// Named references inside a UI template (table row, tree row, callout ...).
// The runtime fills a cloned template through these keys, so you can freely
// restyle / move / rename the template's children in the Editor.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IETM
{
    public class UIRefs : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string key;
            public Component value;
        }

        [Tooltip("Keys are used by the IETM code. Change the referenced objects freely; keep the keys.")]
        public List<Entry> entries = new List<Entry>();

        public T Get<T>(string key) where T : Component
        {
            foreach (var e in entries)
            {
                if (e.key != key || e.value == null) continue;
                if (e.value is T t) return t;
                return e.value.GetComponent<T>();
            }
            return null;
        }

        public RectTransform Rect(string key) => Get<RectTransform>(key);

        public UIRefs Set(string key, Component c)
        {
            foreach (var e in entries) if (e.key == key) { e.value = c; return this; }
            entries.Add(new Entry { key = key, value = c });
            return this;
        }
    }
}
