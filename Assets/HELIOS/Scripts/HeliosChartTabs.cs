// HELIOS - tabs over a line chart (Sensor Trends: Vibration / Strain / Humidity): one series is shown at a time.
// The tab buttons are connected when the page starts; the chart keeps all its series (edit them on the chart).
using UnityEngine;
using UnityEngine.UI;

namespace Helios
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Chart Tabs")]
    public class HeliosChartTabs : MonoBehaviour
    {
        public HeliosChart chart;
        [Tooltip("One button per chart series, in the same order.")]
        public Button[] tabs = new Button[0];
        [Tooltip("The highlight shown on the selected tab (one per tab).")]
        public GameObject[] selectedLook = new GameObject[0];
        [Tooltip("Texts tinted with the selected / normal role (one per tab, optional).")]
        public HeliosThemed[] labels = new HeliosThemed[0];
        public int current;

        Color[] baseColors;
        bool wired;

        void Awake() { Wire(); Select(current); }

        void Wire()
        {
            if (wired) return;
            wired = true;
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i;
                if (tabs[i] != null) tabs[i].onClick.AddListener(() => Select(k));
            }
        }

        public void Select(int i)
        {
            current = i;
            for (int t = 0; t < selectedLook.Length; t++) if (selectedLook[t] != null) selectedLook[t].SetActive(t == i);
            for (int t = 0; t < labels.Length; t++) if (labels[t] != null) labels[t].SetRole(t == i ? HeliosRole.White : HeliosRole.TextMuted);
            if (chart == null || chart.series == null) return;
            if (baseColors == null || baseColors.Length != chart.series.Count)
            {
                baseColors = new Color[chart.series.Count];
                for (int s = 0; s < baseColors.Length; s++) baseColors[s] = chart.series[s].color;
            }
            for (int s = 0; s < chart.series.Count; s++)
            {
                var c = baseColors[s];
                c.a = s == i ? baseColors[s].a : 0f;
                chart.series[s].color = c;
            }
            chart.SetVerticesDirty();
        }
    }
}
