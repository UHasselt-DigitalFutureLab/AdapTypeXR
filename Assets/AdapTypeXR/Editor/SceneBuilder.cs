using System.IO;
using AdapTypeXR.Controllers;
using AdapTypeXR.Presenters;
using AdapTypeXR.Services;
using AdapTypeXR.Simulation;
using AdapTypeXR.Typography;
using AdapTypeXR.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AdapTypeXR.Editor
{
    /// <summary>
    /// Builds the MainReadingScene programmatically.
    ///
    /// Invoke via: AdapTypeXR > Build Simulation Scene
    ///
    /// Creates a complete, ready-to-run simulation scene:
    ///  - Directional light + ambient lighting
    ///  - First-person camera with mouse look
    ///  - 3D book with three world-space text pages
    ///  - Screen-space researcher control panel
    ///  - Session management and mock eye tracking
    ///  - EventSystem for UI interaction
    ///
    /// After running, press Play to start the simulation immediately.
    /// Press Tab in Play mode to toggle the researcher panel.
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/AdapTypeXR/Scenes/MainReadingScene.unity";

        // ── Layout Constants ───────────────────────────────────────────────

        // Camera starts at standing eye height, looking forward.
        private static readonly Vector3 CameraPosition = new(0f, 1.7f, 0f);

        // Book sits 1.2 m in front of the camera, tilted -8° to face the reader naturally.
        private static readonly Vector3 BookPosition  = new(0f, 1.35f, 1.2f);
        private static readonly Vector3 BookRotation  = new(-8f, 0f, 0f);

        // Single page canvas: 148 × 400 units at scale 0.001 = 0.148 m × 0.400 m.
        private const float CanvasScale  = 0.001f;
        private const float CanvasWidth  = 148f;
        private const float CanvasHeight = 400f;

        // Half-book offset: each page canvas is shifted left/right from the book centre.
        private const float PageOffsetX = 0.087f;   // world-space metres

        // Book physical dimensions (world space).
        private const float CoverW = 0.360f;
        private const float CoverH = 0.430f;
        private const float CoverD = 0.022f;
        private const float SpineW = 0.028f;

        // ── Menu Entry Point ───────────────────────────────────────────────

        [MenuItem("AdapTypeXR/Build Simulation Scene")]
        public static void BuildSimulationScene()
        {
            if (!ConfirmBuild()) return;

            EnsureScenesDirectory();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Lighting ───────────────────────────────────────────────────
            var light = CreateDirectionalLight();

            // Set ambient lighting to a soft warm white to avoid harsh shadows on text.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f);
            RenderSettings.ambientIntensity = 1f;

            // ── Camera ─────────────────────────────────────────────────────
            var mainCamera = CreateMainCamera();

            // ── Book ───────────────────────────────────────────────────────
            var book = CreateBook(mainCamera);

            // ── Session Management ─────────────────────────────────────────
            var sessionManager = CreateSessionManager(mainCamera);

            // ── Researcher UI ──────────────────────────────────────────────
            CreateResearcherUI(mainCamera);

            // ── Event System ───────────────────────────────────────────────
            CreateEventSystem();

            // ── Save ───────────────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[SceneBuilder] MainReadingScene created at {ScenePath}. Press Play to run.");
            EditorUtility.DisplayDialog(
                "Scene Built",
                $"MainReadingScene created at:\n{ScenePath}\n\n" +
                "Press Play to start the simulation.\n\n" +
                "Controls:\n" +
                "  ← → Arrow keys: Turn pages\n" +
                "  N: Next condition\n" +
                "  P: Pause / Resume\n" +
                "  Tab: Toggle researcher panel\n" +
                "  F: Reset camera\n" +
                "  Right-click + drag: Look around",
                "Got it");
        }

        // ── Scene Object Builders ──────────────────────────────────────────

        private static GameObject CreateDirectionalLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.intensity = 0.8f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            return go;
        }

        private static Camera CreateMainCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = CameraPosition;

            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 50f;

            go.AddComponent<AudioListener>();
            go.AddComponent<SimulationCameraController>();

            // Mock eye tracker: gaze is derived from camera + mouse position.
            var eyeTracker = go.AddComponent<MockEyeTrackingService>();
            go.AddComponent<GazeDebugVisualizer>();

            return cam;
        }

        private static GameObject CreateBook(Camera mainCamera)
        {
            var book = new GameObject("Book");
            book.transform.position = BookPosition;
            book.transform.eulerAngles = BookRotation;
            book.AddComponent<BookPresenter>();

            // 3-D hardcover geometry (purely visual, no colliders that block gaze).
            CreateBookGeometry(book.transform);

            // Left page — decorative title page, NOT a BookPage (never shows poem text).
            CreateTitlePage(book.transform, mainCamera);

            // Right pages — interactive BookPages auto-discovered by tag.
            // Two pages cover the two-stanza split of the Frost poem.
            for (int i = 0; i < 2; i++)
                CreateTextPage(book.transform, i, mainCamera);

            return book;
        }

        // ── Book Geometry ──────────────────────────────────────────────────

        /// <summary>
        /// Builds the physical book shape from Unity primitives:
        /// hardcover body, spine crease, left/right page-stack edges.
        /// All geometry sits behind the text canvases (positive local Z).
        /// </summary>
        private static void CreateBookGeometry(Transform parent)
        {
            // Shared materials.
            var coverMat = MakeMaterial(new Color(0.13f, 0.22f, 0.14f));   // dark forest green
            var spineMat  = MakeMaterial(new Color(0.09f, 0.15f, 0.10f));   // deeper green
            var pageMat   = MakeMaterial(new Color(0.94f, 0.91f, 0.84f));   // aged cream
            var edgeMat   = MakeMaterial(new Color(0.85f, 0.82f, 0.75f));   // page-edge shadow

            // ── Hardcover body ─────────────────────────────────────────────
            var cover = MakePrimitive(PrimitiveType.Cube, "Cover", parent,
                Vector3.zero, new Vector3(CoverW, CoverH, CoverD), coverMat);
            // The cover sits behind everything at z = CoverD/2 (centre of cube).

            // ── Spine crease (thin dark strip down the centre) ─────────────
            MakePrimitive(PrimitiveType.Cube, "Spine_Crease", parent,
                new Vector3(0f, 0f, -(CoverD * 0.5f + 0.001f)),
                new Vector3(SpineW, CoverH + 0.002f, 0.004f), spineMat);

            // ── Page-block surfaces (the cream face you read on) ───────────
            // Left page surface
            MakePrimitive(PrimitiveType.Quad, "PageFace_Left", parent,
                new Vector3(-PageOffsetX, 0f, -(CoverD * 0.5f + 0.002f)),
                new Vector3(CoverW * 0.5f - SpineW * 0.5f - 0.003f, CoverH - 0.018f, 1f),
                pageMat);

            // Right page surface
            MakePrimitive(PrimitiveType.Quad, "PageFace_Right", parent,
                new Vector3(+PageOffsetX, 0f, -(CoverD * 0.5f + 0.002f)),
                new Vector3(CoverW * 0.5f - SpineW * 0.5f - 0.003f, CoverH - 0.018f, 1f),
                pageMat);

            // ── Page-stack edge strips (visible thickness of pages) ────────
            float edgeX   = CoverW * 0.5f - 0.005f;
            float edgeH   = CoverH - 0.022f;
            float edgeD   = CoverD - 0.006f;

            MakePrimitive(PrimitiveType.Cube, "PageEdge_Left", parent,
                new Vector3(-edgeX, 0f, 0f), new Vector3(0.008f, edgeH, edgeD), edgeMat);
            MakePrimitive(PrimitiveType.Cube, "PageEdge_Right", parent,
                new Vector3(+edgeX, 0f, 0f), new Vector3(0.008f, edgeH, edgeD), edgeMat);

            // ── Top/bottom cover lip (shows hardcover extends beyond pages) ─
            float lipY = CoverH * 0.5f;
            MakePrimitive(PrimitiveType.Cube, "CoverLip_Top", parent,
                new Vector3(0f, +lipY, 0f), new Vector3(CoverW, 0.012f, CoverD), coverMat);
            MakePrimitive(PrimitiveType.Cube, "CoverLip_Bottom", parent,
                new Vector3(0f, -lipY, 0f), new Vector3(CoverW, 0.012f, CoverD), coverMat);
        }

        // ── Page Builders ──────────────────────────────────────────────────

        /// <summary>
        /// Left page — static decorative title page.
        /// Not tagged BookPage; the BookPresenter ignores it.
        /// Shows the poem title and author in a typographically styled canvas.
        /// </summary>
        private static void CreateTitlePage(Transform bookParent, Camera mainCamera)
        {
            var page = new GameObject("TitlePage_Left");
            page.transform.SetParent(bookParent, false);
            page.transform.localPosition = new Vector3(-PageOffsetX, 0f, -(CoverD * 0.5f + 0.004f));
            page.transform.localScale = Vector3.one * CanvasScale;

            var canvas = page.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;

            var rt = page.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

            var scaler = page.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 300f;

            // Title text.
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(page.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.55f);
            titleRt.anchorMax = new Vector2(1f, 0.95f);
            titleRt.offsetMin = new Vector2(14f, 0f);
            titleRt.offsetMax = new Vector2(-14f, 0f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "The Road\nNot Taken";
            titleTmp.fontSize = 28f;
            titleTmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
            titleTmp.color = new Color(0.12f, 0.10f, 0.08f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.enableWordWrapping = true;

            // Rule line (thin horizontal divider).
            var ruleGo = new GameObject("Rule");
            ruleGo.transform.SetParent(page.transform, false);
            var ruleRt = ruleGo.AddComponent<RectTransform>();
            ruleRt.anchorMin = new Vector2(0.1f, 0.52f);
            ruleRt.anchorMax = new Vector2(0.9f, 0.53f);
            ruleRt.offsetMin = ruleRt.offsetMax = Vector2.zero;
            var ruleImg = ruleGo.AddComponent<UnityEngine.UI.Image>();
            ruleImg.color = new Color(0.35f, 0.28f, 0.18f, 0.6f);

            // Author text.
            var authorGo = new GameObject("Author");
            authorGo.transform.SetParent(page.transform, false);
            var authorRt = authorGo.AddComponent<RectTransform>();
            authorRt.anchorMin = new Vector2(0f, 0.42f);
            authorRt.anchorMax = new Vector2(1f, 0.52f);
            authorRt.offsetMin = new Vector2(14f, 0f);
            authorRt.offsetMax = new Vector2(-14f, 0f);
            var authorTmp = authorGo.AddComponent<TextMeshProUGUI>();
            authorTmp.text = "Robert Frost · 1916";
            authorTmp.fontSize = 13f;
            authorTmp.fontStyle = FontStyles.Italic;
            authorTmp.color = new Color(0.30f, 0.24f, 0.16f);
            authorTmp.alignment = TextAlignmentOptions.Center;

            // Page number.
            var pageNumGo = new GameObject("PageNumber");
            pageNumGo.transform.SetParent(page.transform, false);
            var pageNumRt = pageNumGo.AddComponent<RectTransform>();
            pageNumRt.anchorMin = new Vector2(0f, 0.02f);
            pageNumRt.anchorMax = new Vector2(1f, 0.08f);
            pageNumRt.offsetMin = pageNumRt.offsetMax = Vector2.zero;
            var pageNumTmp = pageNumGo.AddComponent<TextMeshProUGUI>();
            pageNumTmp.text = "i";
            pageNumTmp.fontSize = 11f;
            pageNumTmp.color = new Color(0.45f, 0.38f, 0.28f);
            pageNumTmp.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Right-side interactive text page.
        /// Tagged "BookPage" so BookPresenter auto-discovers and populates it.
        /// </summary>
        private static void CreateTextPage(Transform bookParent, int index, Camera mainCamera)
        {
            var page = new GameObject($"Page_{index}");
            page.transform.SetParent(bookParent, false);
            page.transform.localPosition = new Vector3(+PageOffsetX, 0f, -(CoverD * 0.5f + 0.004f));
            page.transform.localScale = Vector3.one * CanvasScale;
            page.tag = "BookPage";

            // Collider for gaze raycast detection.
            var col = page.AddComponent<BoxCollider>();
            col.size = new Vector3(CanvasWidth, CanvasHeight, 2f);

            var canvas = page.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;

            var rt = page.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

            var scaler = page.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 300f;

            page.AddComponent<GraphicRaycaster>();

            // Text area fills the page with a small margin.
            var textGo = new GameObject("TextArea");
            textGo.transform.SetParent(page.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 24f);
            textRt.offsetMax = new Vector2(-10f, -16f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = index == 0 ? "Loading…" : "";
            tmp.fontSize = 18f;
            tmp.color = new Color(0.08f, 0.07f, 0.05f);
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.lineSpacing = 8f;

            textGo.AddComponent<TextRendererController>();
            textGo.AddComponent<TypographyAnimator>();

            // Page number label at bottom.
            var numGo = new GameObject("PageNumber");
            numGo.transform.SetParent(page.transform, false);
            var numRt = numGo.AddComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0f, 0f);
            numRt.anchorMax = new Vector2(1f, 0.06f);
            numRt.offsetMin = new Vector2(10f, 4f);
            numRt.offsetMax = new Vector2(-10f, -2f);
            var numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = (index + 1).ToString();
            numTmp.fontSize = 11f;
            numTmp.color = new Color(0.45f, 0.38f, 0.28f);
            numTmp.alignment = TextAlignmentOptions.Center;

            page.SetActive(index == 0);
        }

        private static GameObject CreateSessionManager(Camera mainCamera)
        {
            var go = new GameObject("SessionManager");

            go.AddComponent<ReadingSessionController>();
            go.AddComponent<SimulationBootstrapper>();

            return go;
        }

        private static void CreateResearcherUI(Camera mainCamera)
        {
            // Screen-space overlay canvas — always visible regardless of camera direction.
            var canvasGo = new GameObject("ResearcherUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Panel root — toggleable via Tab key.
            var panel = CreateUIPanel(canvasGo.transform, "Panel",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(10f, -10f), new Vector2(270f, -10f),
                new Vector2(0f, 1f), new Vector2(280f, 340f));

            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            // Title
            CreateLabel(panel.transform, "Title", "AdapTypeXR — Researcher Panel",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -30f), new Vector2(-10f, -5f), 14f, FontStyle.Bold);

            // Status
            var statusText = CreateLabel(panel.transform, "StatusText", "No active session",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -55f), new Vector2(-10f, -35f), 12f);

            // Condition
            var conditionText = CreateLabel(panel.transform, "ConditionText", "Condition: —",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -75f), new Vector2(-10f, -57f), 11f);

            // Page
            var pageText = CreateLabel(panel.transform, "PageText", "Page: —",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -93f), new Vector2(-10f, -77f), 11f);

            // Separator
            CreateSeparator(panel.transform, -100f);

            // Buttons
            float btnY = -115f;
            var startBtn = CreateButton(panel.transform, "StartBtn", "Start Demo Session",
                new Vector2(10f, btnY - 28f), new Vector2(-10f, btnY), Color.white);

            btnY -= 38f;
            var pauseBtn = CreateButton(panel.transform, "PauseResumeBtn", "Pause",
                new Vector2(10f, btnY - 28f), new Vector2(-10f, btnY), new Color(1f, 0.8f, 0.2f));

            btnY -= 38f;
            var nextCondBtn = CreateButton(panel.transform, "NextConditionBtn", "Next Condition  [N]",
                new Vector2(10f, btnY - 28f), new Vector2(-10f, btnY), new Color(0.4f, 0.9f, 0.5f));

            btnY -= 38f;
            var endBtn = CreateButton(panel.transform, "EndSessionBtn", "End Session",
                new Vector2(10f, btnY - 28f), new Vector2(-10f, btnY), new Color(1f, 0.4f, 0.4f));

            // Separator
            CreateSeparator(panel.transform, btnY - 40f);

            // Shortcuts
            var shortcutsText = CreateLabel(panel.transform, "ShortcutsText", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(10f, btnY - 100f), new Vector2(-10f, btnY - 50f), 10f);
            shortcutsText.color = new Color(0.7f, 0.7f, 0.7f);

            // Wire ResearcherControlPanel
            var panelController = canvasGo.AddComponent<ResearcherControlPanel>();

            // Set private serialized fields via SerializedObject.
            var so = new SerializedObject(panelController);
            so.FindProperty("_startButton").objectReferenceValue = startBtn;
            so.FindProperty("_pauseResumeButton").objectReferenceValue = pauseBtn;
            so.FindProperty("_nextConditionButton").objectReferenceValue = nextCondBtn;
            so.FindProperty("_endSessionButton").objectReferenceValue = endBtn;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_conditionText").objectReferenceValue = conditionText;
            so.FindProperty("_pageText").objectReferenceValue = pageText;
            so.FindProperty("_shortcutsText").objectReferenceValue = shortcutsText;
            so.FindProperty("_panelRoot").objectReferenceValue = panel;
            so.ApplyModifiedProperties();
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        // ── UI Helper Methods ──────────────────────────────────────────────

        private static GameObject CreateUIPanel(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Vector2 pivot, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            rt.sizeDelta = sizeDelta;

            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.75f);
            img.raycastTarget = true;

            return go;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            float fontSize, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;

            return tmp;
        }

        private static Button CreateButton(
            Transform parent, string name, string label,
            Vector2 offsetMin, Vector2 offsetMax, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.AddComponent<Image>();
            img.color = new Color(colour.r * 0.25f, colour.g * 0.25f, colour.b * 0.25f, 0.9f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(colour.r * 0.25f, colour.g * 0.25f, colour.b * 0.25f, 0.9f);
            colors.highlightedColor = new Color(colour.r * 0.4f, colour.g * 0.4f, colour.b * 0.4f, 1f);
            colors.pressedColor = new Color(colour.r * 0.15f, colour.g * 0.15f, colour.b * 0.15f, 1f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            btn.colors = colors;
            btn.targetGraphic = img;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(4f, 2f);
            textRt.offsetMax = new Vector2(-4f, -2f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 11f;
            tmp.color = colour;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;

            return btn;
        }

        private static void CreateSeparator(Transform parent, float yOffset)
        {
            var go = new GameObject("Separator");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(10f, yOffset - 1f);
            rt.offsetMax = new Vector2(-10f, yOffset);

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.15f);
        }

        // ── Primitive / Material Helpers ───────────────────────────────────

        private static GameObject MakePrimitive(
            PrimitiveType type, string name, Transform parent,
            Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // Remove collider — geometry is decorative, must not block gaze raycasts.
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            return go;
        }

        private static Material MakeMaterial(Color colour)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader.name == "Hidden/InternalErrorShader")
                mat = new Material(Shader.Find("Standard"));
            mat.color = colour;
            return mat;
        }

        // ── Utilities ──────────────────────────────────────────────────────

        private static bool ConfirmBuild()
        {
            if (!File.Exists(ScenePath)) return true;

            return EditorUtility.DisplayDialog(
                "Overwrite Scene?",
                $"A scene already exists at:\n{ScenePath}\n\nOverwrite it?",
                "Overwrite", "Cancel");
        }

        private static void EnsureScenesDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/AdapTypeXR/Scenes"))
                AssetDatabase.CreateFolder("Assets/AdapTypeXR", "Scenes");
        }
    }
}
