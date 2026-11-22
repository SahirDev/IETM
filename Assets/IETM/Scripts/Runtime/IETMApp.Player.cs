// Procedure player: Operation, Disassembly and Assembly data modules.
// - Both side panels collapse while one of these modules is open (restored when you leave it).
// - A music-player style bar sits at the bottom of the 3D view: Previous · Pause/Continue · Replay · Next,
//   with the current step's instruction (title, warning / caution, text) shown above the buttons.
// - While a part moves, its name label (same as the mouse-over label) follows it.
// Everything is created at runtime from code under the IETM viewport overlay; icons: Resources/IETM/Player.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public partial class IETMApp
    {
        [Header("Procedure player")]
        [Tooltip("Collapse the publication and data module panels while Operation / Disassembly / Assembly is open.")]
        public bool collapsePanelsInProcedures = true;
        [Tooltip("Width of the player (instruction + buttons), bottom-right of the 3D view. Shown only while the right panel is collapsed.")]
        [Range(360f, 900f)] public float playerWidth = 480f;
        [Tooltip("Seconds a moving part keeps its name label after it stops.")]
        [Range(0f, 3f)] public float movingLabelLinger = 0.8f;

        // state
        bool procPaused;
        bool panelsSavedForProc, savedLeftOpen, savedRightOpen;

        // UI
        RectTransform player;
        Text playerStep, playerTitle, playerNote, playerText;
        RectTransform playerProgress;
        Image playerToggleIcon;
        Button playerPrev, playerNext;
        Sprite spPlay, spPause;

        class MovingLabel { public Image box; public Text text; public RectTransform dot; public float lastMoving = -99f; }
        readonly List<MovingLabel> movingLabels = new List<MovingLabel>();

        /// <summary>Animation time step: zero while the procedure player is paused.</summary>
        float AnimDt => procPaused ? 0f : Time.unscaledDeltaTime;

        // ------------------------------------------------------------------ hooks (called from IETMApp)

        /// <summary>After a module opens: panels, player visibility, pause state.</summary>
        void OnModuleOpenedForPlayer()
        {
            procPaused = false;
            bool anim = InAnimatedProcedure;
            if (collapsePanelsInProcedures)
            {
                if (anim && !panelsSavedForProc)
                {
                    savedLeftOpen = leftOpen; savedRightOpen = rightOpen; panelsSavedForProc = true;
                    leftOpen = false; rightOpen = false; UpdatePanelHandles();
                }
                else if (!anim && panelsSavedForProc)
                {
                    leftOpen = savedLeftOpen; rightOpen = savedRightOpen; panelsSavedForProc = false;
                    UpdatePanelHandles();
                }
            }
            RefreshPlayer();
        }

        /// <summary>Fresh session: forget the saved panel state, unpause.</summary>
        void ResetPlayer()
        {
            procPaused = false;
            panelsSavedForProc = false;
            foreach (var l in movingLabels) if (l.box) l.box.gameObject.SetActive(false);
            RefreshPlayer();
        }

        // ------------------------------------------------------------------ commands

        void PlayerPrev() { StepPrev(); }
        void PlayerNext() { StepNext(); }

        void TogglePause()
        {
            if (!InAnimatedProcedure) return;
            procPaused = !procPaused;
            RefreshPlayer();
            Toast(procPaused ? "Paused" : "Continue", Theme.Accent2);
        }

        /// <summary>Plays the current step's movement again from its start.</summary>
        void ReplayStep()
        {
            if (!InAnimatedProcedure) return;
            procPaused = false;
            ApplyStep();
            var s = currentModule.steps[procStep];
            if (s.focus != null && (s.action == "detach" || s.action == "install" || s.action == "remove"))
            {
                var leaves = new List<IPDItem>();
                foreach (var f in s.focus) { var it = db.Find(f); if (it != null) leaves.AddRange(model.LeavesOf(it)); }
                model.RestartRemovals(leaves);
            }
            RefreshPlayer();
        }

        // ------------------------------------------------------------------ UI

        void BuildPlayer()
        {
            if (player != null || view == null || view.overlay == null) return;
            spPlay = Resources.Load<Sprite>("IETM/Player/player_play");
            spPause = Resources.Load<Sprite>("IETM/Player/player_pause");

            var p = UIKit.Panel(view.overlay, Theme.Panel.A(0.94f), 14, "Procedure Player");
            p.Border(Theme.Border);
            player = p.rectTransform;
            player.Anchor(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 80), new Vector2(playerWidth, 10));
            p.VLayout(6, 16, 16, 12, 10, TextAnchor.UpperCenter, true, false);
            var fit = p.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // instruction: STEP n OF m · title
            var head = UIKit.Rect("Header", player); head.LE(-1, 22);
            head.HLayout(8, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, true);
            var chip = UIKit.Panel(head, Theme.Accent.A(0.22f), 6, "Step Chip", false); chip.LE(92, 22);
            playerStep = UIKit.Label(chip.transform, "STEP 1 OF 1", 11, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold, "Step");
            playerStep.rectTransform.Stretch();
            playerTitle = UIKit.Label(head, "", 14, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, "Title");
            playerTitle.horizontalOverflow = HorizontalWrapMode.Wrap; playerTitle.LE(-1, 22, 1);

            playerNote = UIKit.Label(player, "", 12, Theme.Warn, TextAnchor.UpperLeft, FontStyle.Bold, "Warning");
            playerNote.horizontalOverflow = HorizontalWrapMode.Wrap; playerNote.verticalOverflow = VerticalWrapMode.Overflow;
            playerText = UIKit.Label(player, "", 13, Theme.Text, TextAnchor.UpperLeft, FontStyle.Normal, "Instruction");
            playerText.horizontalOverflow = HorizontalWrapMode.Wrap; playerText.verticalOverflow = VerticalWrapMode.Overflow;
            playerText.lineSpacing = 1.1f;

            // progress
            var bar = UIKit.Panel(player, Theme.Border.A(0.8f), 2, "Progress", false); bar.LE(-1, 4);
            playerProgress = UIKit.Panel(bar.transform, Theme.Accent, 2, "Fill", false).rectTransform;
            playerProgress.Stretch();

            // buttons
            var row = UIKit.Rect("Controls", player); row.LE(-1, 52);
            row.HLayout(18, 0, 0, 0, 0, TextAnchor.MiddleCenter, false, false);
            playerPrev = PlayerButton(row, "Previous", "IETM/Player/player_prev", 40, false, "Previous step  (←)", PlayerPrev, out _);
            PlayerButton(row, "Pause", "IETM/Player/player_pause", 52, true, "Pause / continue  (Space)", TogglePause, out playerToggleIcon);
            PlayerButton(row, "Replay", "IETM/Player/player_replay", 40, false, "Replay this step", ReplayStep, out _);
            playerNext = PlayerButton(row, "Next", "IETM/Player/player_next", 40, false, "Next step  (→)", PlayerNext, out _);

            player.gameObject.SetActive(false);
        }

        Button PlayerButton(RectTransform parent, string name, string icon, float size, bool accent, string tip, UnityEngine.Events.UnityAction onClick, out Image iconImg)
        {
            Color baseC = accent ? Theme.Accent : Theme.Panel3;
            var bg = UIKit.Panel(parent, baseC, Mathf.RoundToInt(size * 0.5f), name);
            bg.LE(size, size, -1, -1, size, size);
            bg.Border(accent ? Theme.Accent.A(0.6f) : Theme.Border);
            var b = bg.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var cb = b.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.disabledColor = new Color(1f, 1f, 1f, 0.35f); cb.colorMultiplier = 1.4f;
            b.colors = cb;
            b.onClick.AddListener(onClick);
            iconImg = UIKit.Panel(bg.transform, accent ? Color.white : Theme.Text, 0, "Icon", false);   // follows Light mode on plain buttons
            iconImg.sprite = Resources.Load<Sprite>(icon);
            iconImg.preserveAspect = true;
            float s = size * 0.46f;
            iconImg.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s, s));
            bg.gameObject.AddComponent<UITooltip>().text = tip;
            return b;
        }

        void RefreshPlayer()
        {
            if (view == null) return;
            BuildPlayer();
            if (player == null) return;
            bool show = PlayerShouldShow;
            if (player.gameObject.activeSelf != show) player.gameObject.SetActive(show);
            if (!show) return;

            var steps = currentModule.steps;
            int n = steps.Length, i = Mathf.Clamp(procStep, 0, n - 1);
            var s = steps[i];
            float w = playerWidth;
            if (view.overlay.rect.width > 0f) w = Mathf.Min(playerWidth, view.overlay.rect.width - 40f);
            player.sizeDelta = new Vector2(w, player.sizeDelta.y);

            playerStep.text = $"STEP {i + 1} OF {n}";
            playerTitle.text = s.title;
            string note = "";
            if (!string.IsNullOrEmpty(s.warning)) note = Prefixed("WARNING", s.warning);
            if (!string.IsNullOrEmpty(s.caution)) note += (note.Length > 0 ? "\n" : "") + $"<color={Theme.ToHex(Theme.Warn)}>{Prefixed("CAUTION", s.caution)}</color>";
            playerNote.color = !string.IsNullOrEmpty(s.warning) ? Theme.Danger : Theme.Warn;
            playerNote.text = note;
            playerNote.gameObject.SetActive(note.Length > 0);
            playerText.text = s.text;

            playerProgress.anchorMin = Vector2.zero; playerProgress.anchorMax = new Vector2((i + 1f) / n, 1f);
            playerProgress.offsetMin = playerProgress.offsetMax = Vector2.zero;

            if (playerPrev) playerPrev.interactable = i > 0;
            if (playerNext) playerNext.interactable = i < n - 1;
            if (playerToggleIcon) playerToggleIcon.sprite = procPaused ? spPlay : spPause;
            if (playerToggleIcon && playerToggleIcon.transform.parent.TryGetComponent<UITooltip>(out var tip))
                tip.text = procPaused ? "Continue  (Space)" : "Pause  (Space)";
        }

        /// <summary>"WARNING:  text", unless the data text already starts with WARNING / CAUTION.</summary>
        static string Prefixed(string word, string text)
        {
            text = text.Trim();
            return text.StartsWith(word, System.StringComparison.OrdinalIgnoreCase) ? text : word + ":  " + text;
        }

        // ------------------------------------------------------------------ layout of the 3D view tools during procedures
        RectTransform toolbarRt, gearSegRt;
        Vector2 tbAnchorMin, tbAnchorMax, tbPivot, tbPos;

        /// <summary>Disassembly / assembly (parts are laid out): the Extended / Retracted switch is hidden.</summary>
        bool InLayoutProcedure
        {
            get
            {
                if (currentModule == null || currentModule.type != "procedure" || currentModule.steps == null) return false;
                foreach (var s in currentModule.steps) if (s.action == "detach" || s.action == "install") return true;
                return false;
            }
        }

        /// <summary>Per frame. While the player is shown: the 3D toolbar (Fit, Isolate, Explode...) moves to the
        /// bottom-left and the player sits in the bottom-right corner, leaving the middle of the view free.
        /// Disassembly / assembly also hide the Extended / Retracted switch. Everything is restored afterwards.</summary>
        void UpdateProcedureLayout()
        {
            if (view == null || view.overlay == null) return;
            if (toolbarRt == null)
            {
                toolbarRt = view.overlay.Find("Toolbar") as RectTransform;
                if (toolbarRt != null) { tbAnchorMin = toolbarRt.anchorMin; tbAnchorMax = toolbarRt.anchorMax; tbPivot = toolbarRt.pivot; tbPos = toolbarRt.anchoredPosition; }
            }
            if (gearSegRt == null && view.gearExtended != null) gearSegRt = view.gearExtended.transform.parent as RectTransform;

            if (gearSegRt != null)
            {
                bool seg = !InLayoutProcedure;
                if (gearSegRt.gameObject.activeSelf != seg) gearSegRt.gameObject.SetActive(seg);
            }

            bool playerOn = player != null && player.gameObject.activeSelf;
            if (toolbarRt != null)
            {
                var min = playerOn ? Vector2.zero : tbAnchorMin;
                var max = playerOn ? Vector2.zero : tbAnchorMax;
                var piv = playerOn ? Vector2.zero : tbPivot;
                var pos = playerOn ? new Vector2(16f, tbPos.y) : tbPos;
                if (toolbarRt.anchorMin != min || toolbarRt.pivot != piv || toolbarRt.anchoredPosition != pos)
                { toolbarRt.anchorMin = min; toolbarRt.anchorMax = max; toolbarRt.pivot = piv; toolbarRt.anchoredPosition = pos; }
            }
            if (playerOn)
            {
                // bottom corner when both fit side by side, otherwise just above the toolbar
                float w = view.overlay.rect.width, y = tbPos.y;
                if (toolbarRt != null && 16f + toolbarRt.rect.width + 16f + player.rect.width + 16f > w)
                    y = tbPos.y + toolbarRt.rect.height + 12f;
                var p = new Vector2(-16f, y);
                if (player.anchoredPosition != p) player.anchoredPosition = p;
            }
        }

        /// <summary>In Operation / Disassembly / Assembly while the right (data module) panel is collapsed:
        /// that panel already shows the steps, so the bar hides while it is open and comes back
        /// once it has slid away.</summary>
        bool PlayerShouldShow => InAnimatedProcedure && !rightOpen && rightT < 0.15f;

        /// <summary>Per frame: follow the right panel (opened / collapsed with its edge arrow).</summary>
        void UpdatePlayerVisibility()
        {
            if (player == null && InAnimatedProcedure) RefreshPlayer();
            UpdateProcedureLayout();
            if (player == null) return;
            bool show = PlayerShouldShow;
            if (player.gameObject.activeSelf != show) { RefreshPlayer(); UpdateProcedureLayout(); }
        }

        // ------------------------------------------------------------------ moving-part labels

        void UpdateMovingLabels()
        {
            int used = 0;
            if (InAnimatedProcedure && model != null && viewerCamera != null && view.overlay != null)
            {
                var s = currentModule.steps[Mathf.Clamp(procStep, 0, currentModule.steps.Length - 1)];
                // disassembly / assembly only: in Operation the names stay on mouse-over
                bool moves = s.action == "detach" || s.action == "install" || s.action == "remove";
                if (moves && s.focus != null)
                {
                    foreach (var f in s.focus)
                    {
                        if (used >= 6) break;
                        var it = db.Find(f);
                        if (it == null) continue;
                        var leaves = model.LeavesOf(it);
                        if (leaves.Count == 0) continue;
                        bool moving = !model.holdRemovals && model.IsMoving(leaves);
                        var lab = MovingLabelAt(used);
                        if (moving) lab.lastMoving = Time.unscaledTime;
                        float age = Time.unscaledTime - lab.lastMoving;
                        if (age > movingLabelLinger || !model.TryGetBounds(leaves, out var b)) continue;
                        if (!PlaceMovingLabel(lab, it, b.center, age)) continue;
                        used++;
                    }
                }
            }
            for (int i = used; i < movingLabels.Count; i++)
                if (movingLabels[i].box && movingLabels[i].box.gameObject.activeSelf) { movingLabels[i].box.gameObject.SetActive(false); movingLabels[i].dot.gameObject.SetActive(false); }
        }

        MovingLabel MovingLabelAt(int i)
        {
            while (movingLabels.Count <= i)
            {
                var o = view.overlay;
                var dot = UIKit.Panel(o, Theme.Warn, 5, "Moving Part Dot", false);
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = Vector2.zero;
                dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                dot.rectTransform.sizeDelta = new Vector2(10, 10);
                dot.Border(Theme.Bg, 2f);
                var box = UIKit.Panel(o, Theme.Bg.A(0.92f), 7, "Moving Part Label", false);
                box.rectTransform.anchorMin = box.rectTransform.anchorMax = Vector2.zero;
                box.rectTransform.pivot = Vector2.zero;
                box.Border(Theme.Warn.A(0.6f));
                var t = UIKit.Label(box.transform, "", 12, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Text");
                t.rectTransform.Stretch(10, 0, 10, 0);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                box.gameObject.SetActive(false); dot.gameObject.SetActive(false);
                movingLabels.Add(new MovingLabel { box = box, text = t, dot = dot.rectTransform });
            }
            return movingLabels[i];
        }

        bool PlaceMovingLabel(MovingLabel lab, IPDItem it, Vector3 world, float age)
        {
            var sp = viewerCamera.WorldToScreenPoint(world);
            if (sp.z <= 0f) return false;
            var o = view.overlay;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(o, sp, null, out var local)) return false;
            var r = o.rect;
            Vector2 at = local - r.min;
            if (at.x < 0 || at.y < 0 || at.x > r.width || at.y > r.height) return false;

            string txt = $"<color={Theme.ToHex(Theme.Warn)}><b>{it.CalloutNo}</b></color>   {it.description}   <color={Theme.ToHex(Theme.Muted)}>{it.partNumber}</color>";
            if (lab.text.text != txt) lab.text.text = txt;
            float w = lab.text.preferredWidth + 22f;
            var box = lab.box.rectTransform;
            box.sizeDelta = new Vector2(w, 28);
            Vector2 p = at + new Vector2(14, 10);
            p.x = Mathf.Clamp(p.x, 4, r.width - w - 4);
            p.y = Mathf.Clamp(p.y, 4, r.height - 32);
            box.anchoredPosition = p;
            lab.dot.anchoredPosition = at;

            float a = age <= 0.01f ? 1f : Mathf.Clamp01(1f - age / Mathf.Max(0.01f, movingLabelLinger));
            lab.box.color = Theme.Bg.A(0.92f * a);
            lab.text.color = Theme.Text.A(a);
            lab.dot.GetComponent<Image>().color = Theme.Warn.A(a);
            if (!lab.box.gameObject.activeSelf) { lab.box.gameObject.SetActive(true); lab.dot.gameObject.SetActive(true); }
            return true;
        }
    }
}
