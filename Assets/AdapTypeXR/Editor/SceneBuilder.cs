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

        // Book is 1.5 m in front of the camera, slightly below eye level — comfortable reading angle.
        private static readonly Vector3 BookPosition = new(0f, 1.4f, 1.5f);

        // World-space canvas: 300 × 420 "units" at scale 0.001 = 0.3 m × 0.42 m.
        private const float CanvasScale = 0.001f;
        private const float CanvasWidth = 300f;
        private const float CanvasHeight = 420f;

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
            // Root book object.
            var book = new GameObject("Book");
            book.transform.position = BookPosition;
            book.AddComponent<BookPresenter>();

            // Visual background — a dark quad behind the pages to frame them.
            CreateBookBackground(book.transform);

            // Three pages (enough for a demo passage; BookPresenter auto-discovers by tag).
            for (int i = 0; i < 3; i++)
                CreatePage(book.transform, i, mainCamera);

            return book;
        }

        private static void CreateBookBackground(Transform parent)
        {
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BookBackground";
            bg.transform.SetParent(parent, false);
            bg.transform.localPosition = new Vector3(0f, 0f, 0.002f); // Slightly behind pages.
            bg.transform.localScale = new Vector3(0.34f, 0.45f, 1f);

            // Cream/off-white book material.
            var renderer = bg.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader.name == "Hidden/InternalErrorShader")
                mat = new Material(Shader.Find("Standard")); // Fallback for non-URP projects.
            mat.color = new Color(0.95f, 0.93f, 0.87f);
            renderer.sharedMaterial = mat;

            // Remove collider — background should not intercept gaze rays.
            Object.DestroyImmediate(bg.GetComponent<Collider>());
        }

        private static void CreatePage(Transform bookParent, int index, Camera mainCamera)
        {
            // Page root — tagged for auto-discovery by BookPresenter.
            var page = new GameObject($"Page_{index}");
            page.transform.SetParent(bookParent, false);
            page.transform.localPosition = Vector3.zero;
            page.tag = "BookPage";

            // Thin box collider for gaze raycast detection.
            var col = page.AddComponent<BoxCollider>();
            col.size = new Vector3(CanvasWidth * CanvasScale, CanvasHeight * CanvasScale, 0.002f);

            // World-space canvas for TMP text rendering.
            var canvas = page.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;

            var canvasRt = page.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            page.transform.localScale = Vector3.one * CanvasScale;

            var scaler = page.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 300f; // High-DPI for crisp SDF text.

            page.AddComponent<GraphicRaycaster>();

            // Text area (child of canvas).
            var textGo = new GameObject("TextArea");
            textGo.transform.SetParent(page.transform, false);

            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(20f, 20f);
            textRt.offsetMax = new Vector2(-20f, -20f);

            // TextMeshProUGUI — RequireComponent on TextRendererController adds this automatically,
            // but we add it first so we can configure it.
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = index == 0
                ? "Loading passage…"
                : $"[Page {index + 1}]";
            tmp.fontSize = 24f;
            tmp.color = new Color(0.08f, 0.08f, 0.08f);
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.TopLeft;

            // TextRendererController + TypographyAnimator handle all further styling.
            textGo.AddComponent<TextRendererController>();
            textGo.AddComponent<TypographyAnimator>();

            // Only page 0 is visible initially.
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
