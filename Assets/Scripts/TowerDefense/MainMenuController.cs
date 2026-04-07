using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// MainMenuController 负责当前项目的主菜单入口。
///
/// 这次和上一版最大的不同，是它不再把 UI 只当成“运行时临时生成物”，
/// 而是会在编辑器里把缺失的界面对象真正补进场景，
/// 这样你打开 `MainMenu` 后，就能直接在 Scene 视图和 Inspector 里看到并修改这些对象。
///
/// 这个脚本现在承担两类职责：
/// 1. 如果主菜单场景还是一个空壳，就补齐 Canvas / EventSystem / 主要 UI 节点。
/// 2. 在玩家点击“开始游戏”时，切换到当前玩法场景 `SampleScene`。
///
/// 其中第一类职责只用于“建好默认页面骨架”，
/// 一旦页面已经搭好，我们就尽量不再覆盖你手动改过的布局，
/// 避免出现“你刚在 Inspector 调好，脚本下一帧又给你改回去”的糟糕体验。
/// </summary>
[ExecuteAlways]
public sealed class MainMenuController : MonoBehaviour
{
    [Header("Scene Flow")]

    /// <summary>
    /// 点击开始后要切换到的玩法场景名。
    ///
    /// 这里默认直接指向当前塔防主玩法场景 `SampleScene`。
    /// 如果你以后改了玩法场景名，要同步更新这里。
    /// </summary>
    [SerializeField] private string gameplaySceneName = "SampleScene";

    [Header("Visual Theme")]

    /// <summary>
    /// 主页面大背景色。
    ///
    /// 这只是默认值；
    /// 一旦主菜单 UI 已经生成，你完全可以直接在场景里改具体对象颜色。
    /// </summary>
    [SerializeField] private Color backgroundColor = new Color(0.03f, 0.05f, 0.08f, 1f);

    /// <summary>
    /// 暖色强调色，主要服务于开始按钮和重点提示。
    /// </summary>
    [SerializeField] private Color primaryAccent = new Color(1f, 0.62f, 0.29f, 1f);

    /// <summary>
    /// 冷色强调色，主要服务于边框、标签和终端感装饰。
    /// </summary>
    [SerializeField] private Color secondaryAccent = new Color(0.31f, 0.86f, 0.96f, 1f);

    [Header("Scene UI Refs")]

    /// <summary>
    /// 主菜单相机。
    ///
    /// 如果场景里还没有相机，脚本会自动补一个；
    /// 补完后引用会记录到这里，方便你之后直接在 Inspector 里调。
    /// </summary>
    [SerializeField] private Camera sceneCamera;

    /// <summary>
    /// 主菜单 Canvas 根。
    /// </summary>
    [SerializeField] private Canvas mainCanvas;

    /// <summary>
    /// Canvas 缩放器。
    /// </summary>
    [SerializeField] private CanvasScaler canvasScaler;

    /// <summary>
    /// UI 射线器。
    /// </summary>
    [SerializeField] private GraphicRaycaster graphicRaycaster;

    /// <summary>
    /// 主菜单 EventSystem。
    /// </summary>
    [SerializeField] private EventSystem eventSystem;

    /// <summary>
    /// 标准输入模块。
    /// </summary>
    [SerializeField] private StandaloneInputModule standaloneInputModule;

    /// <summary>
    /// 主菜单 UI 的总根节点。
    ///
    /// 所有真正可见的 UI 都挂在这里下面，
    /// 所以后面你如果要整体缩放、整体移动或重新分组，
    /// 这个根节点会是最好用的入口。
    /// </summary>
    [SerializeField] private RectTransform menuRoot;

    /// <summary>
    /// 全屏背景面板。
    /// </summary>
    [SerializeField] private Image backgroundPanel;

    /// <summary>
    /// 外层框架面板。
    /// </summary>
    [SerializeField] private Image frameCorePanel;

    /// <summary>
    /// 内层信息底板。
    /// </summary>
    [SerializeField] private Image frameInsetPanel;

    /// <summary>
    /// 主标题文字。
    /// </summary>
    [SerializeField] private TextMeshProUGUI titleText;

    /// <summary>
    /// 副标题文字。
    /// </summary>
    [SerializeField] private TextMeshProUGUI subtitleText;

    /// <summary>
    /// 页面说明正文。
    /// </summary>
    [SerializeField] private TextMeshProUGUI descriptionText;

    /// <summary>
    /// 开始按钮上方的小提示。
    /// </summary>
    [SerializeField] private TextMeshProUGUI hintText;

    /// <summary>
    /// 开始按钮本体。
    ///
    /// 这个引用是最关键的 Inspector 引用之一，
    /// 因为真正触发切场景的就是它。
    /// </summary>
    [SerializeField] private Button startButton;

    /// <summary>
    /// 开始按钮底图。
    /// </summary>
    [SerializeField] private Image startButtonImage;

    /// <summary>
    /// 开始按钮主文字。
    /// </summary>
    [SerializeField] private TextMeshProUGUI startButtonPrimaryText;

    /// <summary>
    /// 开始按钮副文字。
    /// </summary>
    [SerializeField] private TextMeshProUGUI startButtonSecondaryText;

    /// <summary>
    /// 底部左侧标签。
    /// </summary>
    [SerializeField] private TextMeshProUGUI footerLeftText;

    /// <summary>
    /// 底部右侧标签。
    /// </summary>
    [SerializeField] private TextMeshProUGUI footerRightText;

    /// <summary>
    /// 记录默认主菜单骨架是否已经搭建过。
    ///
    /// 这个标记非常关键：
    /// - `false`：说明场景还是空的，脚本应该补齐一版默认 UI
    /// - `true`：说明主菜单已经成型，脚本以后就尽量只补缺引用，不再强推默认布局
    ///
    /// 这样我们就兼顾了“自动搭出来”和“后续可手改”两件事。
    /// </summary>
    [SerializeField] private bool hasBuiltSceneUi;

    private const string CanvasName = "MainMenuCanvas";
    private const string EventSystemName = "MainMenuEventSystem";
    private const string RootName = "MainMenuRoot";
    private const string BackgroundName = "BackgroundPanel";
    private const string FrameCoreName = "FrameCore";
    private const string FrameInsetName = "FrameInset";
    private const string TitleName = "TitleText";
    private const string SubtitleName = "SubtitleText";
    private const string DescriptionName = "DescriptionText";
    private const string HintName = "HintText";
    private const string StartButtonName = "StartGameButton";
    private const string StartPrimaryName = "StartButtonPrimaryText";
    private const string StartSecondaryName = "StartButtonSecondaryText";
    private const string FooterLeftName = "FooterLeftText";
    private const string FooterRightName = "FooterRightText";

    /// <summary>
    /// 无论在编辑器还是运行时，只要脚本启用，就先补齐场景骨架并绑定按钮。
    ///
    /// 用 `ExecuteAlways` 的意义就在这里：
    /// 你打开场景时，哪怕没进 Play，也能看到 UI 被真正生成到层级里。
    /// </summary>
    private void OnEnable()
    {
        EnsureSceneObjects();
        BindStartButton();
    }

    /// <summary>
    /// 关闭或销毁时解绑按钮，避免重复注册监听。
    /// </summary>
    private void OnDisable()
    {
        UnbindStartButton();
    }

    /// <summary>
    /// 额外支持 Enter / Space 作为开始键。
    ///
    /// 这里只在 Play 模式真正响应，
    /// 避免你在编辑器里调东西时误触发场景切换。
    /// </summary>
    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
    }

    /// <summary>
    /// 切换到当前玩法场景。
    ///
    /// 如果你在编辑模式下点了按钮，
    /// 这里不会真的切场景，防止改场景时误操作。
    /// </summary>
    public void StartGame()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogWarning("MainMenuController 没有配置要进入的玩法场景名。", this);
            return;
        }

        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// 确保主菜单场景拥有一套可编辑、可运行的基础对象。
    ///
    /// 处理顺序是：
    /// 1. 补相机
    /// 2. 补 EventSystem
    /// 3. 补 Canvas
    /// 4. 如果主菜单骨架还没搭过，就生成默认 UI
    /// 5. 如果已经搭过，只做引用回填，不打断你自己的手动调整
    /// </summary>
    private void EnsureSceneObjects()
    {
        EnsureCameraExists();
        EnsureEventSystemExists();
        EnsureCanvasExists();

        if (!hasBuiltSceneUi)
        {
            BuildDefaultMenuLayout();
            hasBuiltSceneUi = true;
            MarkSceneDirty();
            return;
        }

        ValidateBoundReferences();
    }

    /// <summary>
    /// 给开始按钮注册点击事件。
    ///
    /// 这里用脚本注册，而不是在场景 YAML 里硬写 onClick，
    /// 是为了让按钮逻辑更集中，也更方便你以后改目标场景或扩展逻辑。
    /// </summary>
    private void BindStartButton()
    {
        if (startButton == null)
        {
            return;
        }

        startButton.onClick.RemoveListener(StartGame);
        startButton.onClick.AddListener(StartGame);
    }

    /// <summary>
    /// 解绑按钮监听，避免重复添加。
    /// </summary>
    private void UnbindStartButton()
    {
        if (startButton == null)
        {
            return;
        }

        startButton.onClick.RemoveListener(StartGame);
    }

    /// <summary>
    /// 如果场景里还没有可用相机，就自动补一个。
    /// </summary>
    private void EnsureCameraExists()
    {
        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
        }

        if (sceneCamera != null)
        {
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = backgroundColor;
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        sceneCamera = cameraObject.AddComponent<Camera>();
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = backgroundColor;
        sceneCamera.orthographic = true;
        sceneCamera.nearClipPlane = -10f;
        sceneCamera.farClipPlane = 10f;
        cameraObject.tag = "MainCamera";
    }

    /// <summary>
    /// 确保 EventSystem 存在。
    /// </summary>
    private void EnsureEventSystemExists()
    {
        if (eventSystem == null)
        {
            eventSystem = FindObjectOfType<EventSystem>();
        }

        if (eventSystem != null)
        {
            standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule == null)
            {
                standaloneInputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            return;
        }

        GameObject eventSystemObject = new GameObject(EventSystemName);
        eventSystem = eventSystemObject.AddComponent<EventSystem>();
        standaloneInputModule = eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    /// <summary>
    /// 确保 Canvas 基础设施存在。
    /// </summary>
    private void EnsureCanvasExists()
    {
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }

        if (mainCanvas == null)
        {
            GameObject canvasObject = new GameObject(CanvasName);
            mainCanvas = canvasObject.AddComponent<Canvas>();
        }

        mainCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        mainCanvas.worldCamera = sceneCamera;
        mainCanvas.planeDistance = 10f;

        canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            canvasScaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
        }

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        graphicRaycaster = mainCanvas.GetComponent<GraphicRaycaster>();
        if (graphicRaycaster == null)
        {
            graphicRaycaster = mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    /// <summary>
    /// 搭一版默认主菜单骨架。
    ///
    /// 重点是：
    /// - 把 UI 对象真正创建到场景层级里
    /// - 创建完后记录引用
    /// - 以后你就可以直接在 Scene / Inspector 里继续调
    /// </summary>
    private void BuildDefaultMenuLayout()
    {
        RectTransform canvasRect = mainCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        menuRoot = EnsureRectTransform(canvasRect, RootName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1320f, 760f));
        backgroundPanel = EnsureImage(canvasRect, BackgroundName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f), backgroundColor);
        backgroundPanel.raycastTarget = false;

        frameCorePanel = EnsureImage(menuRoot, FrameCoreName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1320f, 760f), new Color(0.04f, 0.06f, 0.09f, 0.92f));
        frameInsetPanel = EnsureImage(menuRoot, FrameInsetName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1256f, 696f), new Color(0.03f, 0.05f, 0.08f, 0.96f));

        titleText = EnsureText(menuRoot, TitleName, new Vector2(-304f, 168f), new Vector2(720f, 180f), 84f, FontStyles.Bold, Color.white, "电量防线", TextAlignmentOptions.Left);
        titleText.lineSpacing = -18f;

        subtitleText = EnsureText(menuRoot, SubtitleName, new Vector2(-300f, 76f), new Vector2(760f, 92f), 28f, FontStyles.Bold, new Color(0.78f, 0.86f, 0.94f, 1f), "Power Grid Defense / Prototype Access Terminal", TextAlignmentOptions.Left);
        subtitleText.characterSpacing = 2f;

        descriptionText = EnsureText(menuRoot, DescriptionName, new Vector2(-288f, -40f), new Vector2(760f, 180f), 26f, FontStyles.Normal, new Color(0.82f, 0.88f, 0.95f, 1f), "在这里进入当前塔防测试关卡。\n\n你将使用发电机与炮塔，在有限电量下沿部署网络逐步扩张防线。\n\n点击下方开始，即可切换到当前制作中的玩法场景。", TextAlignmentOptions.Left);
        descriptionText.lineSpacing = 6f;

        hintText = EnsureText(menuRoot, HintName, new Vector2(-284f, -214f), new Vector2(760f, 36f), 20f, FontStyles.Bold, new Color(1f, 0.82f, 0.6f, 1f), "START will load SampleScene directly", TextAlignmentOptions.Left);

        startButton = EnsureButton(menuRoot, StartButtonName, new Vector2(344f, -28f), new Vector2(360f, 116f), primaryAccent);
        startButtonImage = startButton.GetComponent<Image>();
        startButtonPrimaryText = EnsureText(startButton.transform as RectTransform, StartPrimaryName, new Vector2(24f, 12f), new Vector2(260f, 40f), 36f, FontStyles.Bold, Color.white, "开始游戏", TextAlignmentOptions.Left);
        startButtonSecondaryText = EnsureText(startButton.transform as RectTransform, StartSecondaryName, new Vector2(24f, -22f), new Vector2(260f, 28f), 18f, FontStyles.Bold, new Color(0.76f, 0.86f, 0.95f, 1f), "ENTER BATTLEFIELD / SAMPLE SCENE", TextAlignmentOptions.Left);

        footerLeftText = EnsureText(menuRoot, FooterLeftName, new Vector2(-334f, -322f), new Vector2(420f, 34f), 18f, FontStyles.Bold, new Color(secondaryAccent.r, secondaryAccent.g, secondaryAccent.b, 0.95f), "ENTRY NODE / MAIN MENU", TextAlignmentOptions.Left);
        footerRightText = EnsureText(menuRoot, FooterRightName, new Vector2(310f, -322f), new Vector2(560f, 34f), 18f, FontStyles.Bold, new Color(primaryAccent.r, primaryAccent.g, primaryAccent.b, 0.95f), "Press Enter / Space or click Start", TextAlignmentOptions.Left);
    }

    /// <summary>
    /// 当主菜单骨架已经搭好后，我们改成“显式引用优先”的维护方式。
    ///
    /// 这里不再像旧版本那样，悄悄按对象名把一整套 UI 子节点再找回来；
    /// 原因是主菜单场景现在已经把这些引用序列化保存好了，
    /// 再去按名字回填，反而会让对象名重新承担装配职责，后续改名也更不安心。
    ///
    /// 因此这一步只做两件事：
    /// 1. 补齐那些可以从已绑定组件直接推导出的轻量引用，例如按钮底图。
    /// 2. 对真正缺失的关键引用输出明确告警，提醒维护者去 Inspector 里补。
    /// </summary>
    private void ValidateBoundReferences()
    {
        if (startButton != null)
        {
            startButtonImage = startButton.GetComponent<Image>();
        }

        WarnIfMissing(sceneCamera, nameof(sceneCamera));
        WarnIfMissing(mainCanvas, nameof(mainCanvas));
        WarnIfMissing(canvasScaler, nameof(canvasScaler));
        WarnIfMissing(graphicRaycaster, nameof(graphicRaycaster));
        WarnIfMissing(eventSystem, nameof(eventSystem));
        WarnIfMissing(standaloneInputModule, nameof(standaloneInputModule));
        WarnIfMissing(menuRoot, nameof(menuRoot));
        WarnIfMissing(backgroundPanel, nameof(backgroundPanel));
        WarnIfMissing(frameCorePanel, nameof(frameCorePanel));
        WarnIfMissing(frameInsetPanel, nameof(frameInsetPanel));
        WarnIfMissing(titleText, nameof(titleText));
        WarnIfMissing(subtitleText, nameof(subtitleText));
        WarnIfMissing(descriptionText, nameof(descriptionText));
        WarnIfMissing(hintText, nameof(hintText));
        WarnIfMissing(startButton, nameof(startButton));
        WarnIfMissing(startButtonImage, nameof(startButtonImage));
        WarnIfMissing(startButtonPrimaryText, nameof(startButtonPrimaryText));
        WarnIfMissing(startButtonSecondaryText, nameof(startButtonSecondaryText));
        WarnIfMissing(footerLeftText, nameof(footerLeftText));
        WarnIfMissing(footerRightText, nameof(footerRightText));
    }

    /// <summary>
    /// 主菜单现在走“显式场景装配”后，缺引用应该尽早暴露出来，
    /// 而不是继续靠隐式查找把问题藏住。
    /// </summary>
    private void WarnIfMissing(Object reference, string fieldName)
    {
        if (reference != null)
        {
            return;
        }

        Debug.LogWarning($"MainMenuController 缺少场景引用：{fieldName}。请在 MainMenu 场景的 Inspector 中补齐。", this);
    }
    private static TMP_FontAsset ResolveFontAsset()
    {
        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    /// <summary>
    /// 创建或复用一个 RectTransform。
    ///
    /// 如果对象已存在，就直接复用；
    /// 只有首次创建时才写默认位置和尺寸，避免覆盖你后续手调。
    /// </summary>
    private static RectTransform EnsureRectTransform(RectTransform parent, string objectName, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform existing = FindRect(parent, objectName);
        if (existing != null)
        {
            return existing;
        }

        GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    /// <summary>
    /// 创建或复用一个 Image。
    /// </summary>
    private static Image EnsureImage(RectTransform parent, string objectName, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        RectTransform rectTransform = EnsureRectTransform(parent, objectName, anchor, pivot, anchoredPosition, sizeDelta);
        Image image = rectTransform.GetComponent<Image>();
        if (image == null)
        {
            image = rectTransform.gameObject.AddComponent<Image>();
        }

        image.color = color;
        return image;
    }

    /// <summary>
    /// 创建或复用一个 TMP 文本。
    /// </summary>
    private static TextMeshProUGUI EnsureText(RectTransform parent, string objectName, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, FontStyles fontStyle, Color color, string text, TextAlignmentOptions alignment)
    {
        RectTransform rectTransform = EnsureRectTransform(parent, objectName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta);
        TextMeshProUGUI label = rectTransform.GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        }

        label.font = ResolveFontAsset();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>
    /// 创建或复用一个按钮。
    /// </summary>
    private Button EnsureButton(RectTransform parent, string objectName, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        RectTransform rectTransform = EnsureRectTransform(parent, objectName, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, sizeDelta);

        Image image = rectTransform.GetComponent<Image>();
        if (image == null)
        {
            image = rectTransform.gameObject.AddComponent<Image>();
        }

        image.color = color;

        Button button = rectTransform.GetComponent<Button>();
        if (button == null)
        {
            button = rectTransform.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.22f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.16f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.3f, 0.32f, 0.36f, 0.8f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        return button;
    }

    /// <summary>
    /// 查找某个同名子 RectTransform。
    /// </summary>
    private static RectTransform FindRect(RectTransform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(objectName);
        return child as RectTransform;
    }

    /// <summary>
    /// 查找某个同名子 Image。
    /// </summary>
    private void MarkSceneDirty()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            return;
        }

        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}