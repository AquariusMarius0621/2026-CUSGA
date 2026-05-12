using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 第二章过场：仿照 Chapter1 的表现，NPC 使用屏幕文字打字显示，Player 使用气泡对话；
/// 仅鼠标左键推进；结束后执行全屏淡入淡出切换下一场景。
/// </summary>
public sealed class Chapter2CutsceneController : MonoBehaviour
{
    [Header("字体")]
    [SerializeField] private TMP_FontAsset dialogueFontAsset;

    [Header("对白")]
    [SerializeField] private bool useScriptDefaultLines = true;
    [SerializeField] private string dialogueId = "chapter2_intro";
    [SerializeField] private List<DialogueLine> lines = new();

    [Header("玩家气泡")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueBubbleView playerBubblePrefab;
    [SerializeField] private Transform playerBubbleAnchor;
    [SerializeField] private Vector3 playerBubbleScreenOffset = new(240f, -220f, 10f);

    [Header("NPC 屏幕文字")]
    [SerializeField] private Vector2 npcTextScreenPosition = new(320f, 120f);
    [SerializeField] private Vector2 npcTextSize = new(760f, 240f);
    [SerializeField] private float npcTextFontSize = 46f;
    [SerializeField] private Color npcTextColor = Color.white;
    [SerializeField] private FontStyles npcTextFontStyle = FontStyles.Normal;
    [SerializeField] private TextAlignmentOptions npcTextAlignment = TextAlignmentOptions.MidlineLeft;
    [SerializeField] private float npcSecondsPerCharacter = 0.03f;

    [Header("交互")]
    [SerializeField] private int mouseButton = 0;
    [SerializeField] private PlayerInteractor2D playerInteractor;

    [Header("切换至下一章（全屏淡入淡出）")]
    [SerializeField] private string nextSceneName = "chapter3";
    [SerializeField] private float fadeOutToBlackDuration = 0.75f;
    [SerializeField] private float fadeInFromBlackDuration = 0.75f;

    private Canvas overlayCanvas;
    private TextMeshProUGUI npcText;
    private CanvasGroup npcTextCanvasGroup;
    private bool waitingForAdvanceClick;
    private int currentLineIndex = -1;
    private bool isConversationActive;
    private Coroutine npcTypingRoutine;
    private bool isNpcTyping;
    private string currentNpcFullText = string.Empty;
    private bool transitionQueued;

    private void Awake()
    {
        ApplyDefaultLinesIfNeeded();
        EnsureDialogueRunner();
        EnsurePlayerAnchor();
        EnsureNpcTextUi();
        ApplyFont();
        ApplyNpcTextStyle();
        UpdatePlayerBubbleAnchor();
        HideNpcTextImmediate();
        ResolvePlayerInteractor();
        PrepareSceneActors();
    }

    private void OnValidate()
    {
        ApplyDefaultLinesIfNeeded();
        if (!Application.isPlaying)
        {
            EnsurePlayerAnchor();
            EnsureNpcTextUi();
        }

        ApplyFont();
        ApplyNpcTextStyle();

        if (!Application.isPlaying)
        {
            UpdatePlayerBubbleAnchor();
        }
    }

    private void Start()
    {
        if (dialogueFontAsset == null)
        {
            dialogueFontAsset = TryLoadDialogueFont();
            ApplyFont();
        }

        if (playerInteractor != null)
        {
            playerInteractor.SetInteractionInputEnabled(false);
            playerInteractor.SetInteractionPromptVisible(false);
            if (playerInteractor.Motor != null)
            {
                playerInteractor.Motor.SetMovementLocked(true);
            }
        }

        StartDialogue();
    }

    private void Update()
    {
        UpdatePlayerBubbleAnchor();

        if (!isConversationActive || transitionQueued || !Input.GetMouseButtonDown(mouseButton))
        {
            return;
        }

        if (isNpcTyping)
        {
            CompleteNpcTyping();
            return;
        }

        if (!waitingForAdvanceClick)
        {
            return;
        }

        waitingForAdvanceClick = false;
        AdvanceDialogue();
    }

    private void ApplyDefaultLinesIfNeeded()
    {
        if (!useScriptDefaultLines)
        {
            return;
        }

        IReadOnlyList<DialogueLine> source = DialogueScripts.Get(dialogueId);
        lines = source != null ? new List<DialogueLine>(source) : new List<DialogueLine>();
    }

    private void EnsureDialogueRunner()
    {
        dialogueRunner = dialogueRunner != null ? dialogueRunner : GetComponent<DialogueRunner>();
        if (dialogueRunner == null)
        {
            dialogueRunner = gameObject.AddComponent<DialogueRunner>();
        }

        if (playerBubblePrefab == null)
        {
            playerBubblePrefab = Resources.Load<DialogueBubbleView>("DialogueBubble");
        }

        dialogueRunner.SetBubblePrefab(playerBubblePrefab);
        dialogueRunner.ConfigureBottomLayout(false, Vector2.zero, Vector2.zero, 1f, false);
    }

    private void EnsurePlayerAnchor()
    {
        if (playerBubbleAnchor == null)
        {
            playerBubbleAnchor = CreateChildAnchor("Chapter2PlayerBubbleAnchor");
        }
    }

    private Transform CreateChildAnchor(string anchorName)
    {
        Transform existing = transform.Find(anchorName);
        if (existing != null)
        {
            return existing;
        }

        GameObject anchor = new GameObject(anchorName);
        anchor.transform.SetParent(transform, false);
        return anchor.transform;
    }

    private void EnsureNpcTextUi()
    {
        if (overlayCanvas == null)
        {
            Transform existingCanvas = transform.Find("Chapter2Canvas");
            if (existingCanvas != null)
            {
                overlayCanvas = existingCanvas.GetComponent<Canvas>();
            }
        }

        if (overlayCanvas == null)
        {
            GameObject canvasObject = new GameObject("Chapter2Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            overlayCanvas = canvasObject.GetComponent<Canvas>();
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        Transform existingText = overlayCanvas.transform.Find("NpcDialogueText");
        if (existingText != null)
        {
            npcText = existingText.GetComponent<TextMeshProUGUI>();
            npcTextCanvasGroup = existingText.GetComponent<CanvasGroup>();
        }

        if (npcText == null)
        {
            GameObject textObject = new GameObject("NpcDialogueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(CanvasGroup));
            textObject.transform.SetParent(overlayCanvas.transform, false);
            npcText = textObject.GetComponent<TextMeshProUGUI>();
            npcTextCanvasGroup = textObject.GetComponent<CanvasGroup>();
        }
        else if (npcTextCanvasGroup == null)
        {
            npcTextCanvasGroup = npcText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void ApplyFont()
    {
        if (npcText != null && dialogueFontAsset != null)
        {
            npcText.font = dialogueFontAsset;
        }
    }

    private void ApplyNpcTextStyle()
    {
        if (npcText == null)
        {
            return;
        }

        RectTransform rect = npcText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = npcTextScreenPosition;
        rect.sizeDelta = npcTextSize;

        npcText.fontSize = npcTextFontSize;
        npcText.color = npcTextColor;
        npcText.fontStyle = npcTextFontStyle;
        npcText.alignment = npcTextAlignment;
        npcText.enableWordWrapping = true;
        npcText.overflowMode = TextOverflowModes.Overflow;
        npcText.raycastTarget = false;

        if (npcTextCanvasGroup != null)
        {
            npcTextCanvasGroup.alpha = npcText.gameObject.activeSelf ? 1f : 0f;
        }
    }

    private void ResolvePlayerInteractor()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor2D>();
        }
    }

    private void PrepareSceneActors()
    {
        GameObject center = GameObject.Find("Center");
        if (center != null)
        {
            NpcDialogue npcDialogue = center.GetComponent<NpcDialogue>();
            if (npcDialogue != null)
            {
                npcDialogue.enabled = false;
            }

            foreach (SpriteRenderer renderer in center.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = false;
            }
        }
    }

    private void StartDialogue()
    {
        currentLineIndex = -1;
        isConversationActive = lines != null && lines.Count > 0;
        waitingForAdvanceClick = false;
        transitionQueued = false;
        StopNpcTypingRoutine();
        HideNpcTextImmediate();

        if (dialogueRunner != null)
        {
            dialogueRunner.HideDialogueBubble();
        }

        if (!isConversationActive)
        {
            return;
        }

        AdvanceDialogue();
    }

    private void AdvanceDialogue()
    {
        currentLineIndex++;
        if (lines == null || currentLineIndex >= lines.Count)
        {
            EndDialogue();
            return;
        }

        ShowLine(lines[currentLineIndex]);
    }

    private void ShowLine(DialogueLine line)
    {
        waitingForAdvanceClick = false;

        if (line.speaker == DialogueSpeaker.NPC)
        {
            ShowNpcText(line.text);
            if (dialogueRunner != null)
            {
                dialogueRunner.HideDialogueBubble();
            }

            StartNpcTyping(line.text);
            return;
        }

        HideNpcTextImmediate();
        if (dialogueRunner != null)
        {
            dialogueRunner.PlayConversation(null, playerBubbleAnchor, null, new List<DialogueLine> { line }, OnPlayerLineFinished);
        }
        else
        {
            waitingForAdvanceClick = true;
        }
    }

    private void OnPlayerLineFinished()
    {
        waitingForAdvanceClick = true;
    }

    private void EndDialogue()
    {
        isConversationActive = false;
        waitingForAdvanceClick = false;
        currentLineIndex = -1;
        StopNpcTypingRoutine();
        HideNpcTextImmediate();

        if (dialogueRunner != null)
        {
            dialogueRunner.HideDialogueBubble();
        }

        if (!transitionQueued)
        {
            transitionQueued = true;
            StartCoroutine(TransitionToNextSceneRoutine());
        }
    }

    private IEnumerator TransitionToNextSceneRoutine()
    {
        yield return null;
        ScreenFadeTransition.Play(nextSceneName, fadeOutToBlackDuration, fadeInFromBlackDuration, startOpaque: false);
    }

    private void ShowNpcText(string content)
    {
        EnsureNpcTextUi();
        ApplyFont();
        ApplyNpcTextStyle();
        currentNpcFullText = content ?? string.Empty;
        npcText.text = string.Empty;
        npcText.gameObject.SetActive(true);
        if (npcTextCanvasGroup != null)
        {
            npcTextCanvasGroup.alpha = 1f;
        }
    }

    private void StartNpcTyping(string content)
    {
        StopNpcTypingRoutine();
        currentNpcFullText = content ?? string.Empty;
        npcTypingRoutine = StartCoroutine(TypeNpcTextRoutine(currentNpcFullText));
    }

    private IEnumerator TypeNpcTextRoutine(string content)
    {
        isNpcTyping = true;
        npcText.text = string.Empty;

        if (string.IsNullOrEmpty(content))
        {
            isNpcTyping = false;
            waitingForAdvanceClick = true;
            npcTypingRoutine = null;
            yield break;
        }

        float delay = Mathf.Max(0.001f, npcSecondsPerCharacter);
        for (int i = 1; i <= content.Length; i++)
        {
            npcText.text = content.Substring(0, i);
            if (i < content.Length)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        isNpcTyping = false;
        waitingForAdvanceClick = true;
        npcTypingRoutine = null;
    }

    private void CompleteNpcTyping()
    {
        StopNpcTypingRoutine();
        if (npcText != null)
        {
            npcText.text = currentNpcFullText;
        }

        isNpcTyping = false;
        waitingForAdvanceClick = true;
    }

    private void StopNpcTypingRoutine()
    {
        if (npcTypingRoutine != null)
        {
            StopCoroutine(npcTypingRoutine);
            npcTypingRoutine = null;
        }

        isNpcTyping = false;
    }

    private void HideNpcTextImmediate()
    {
        StopNpcTypingRoutine();
        currentNpcFullText = string.Empty;

        if (npcText != null)
        {
            npcText.gameObject.SetActive(false);
            npcText.text = string.Empty;
        }

        if (npcTextCanvasGroup != null)
        {
            npcTextCanvasGroup.alpha = 0f;
        }
    }

    private void UpdatePlayerBubbleAnchor()
    {
        Camera cam = Camera.main;
        if (cam == null || playerBubbleAnchor == null)
        {
            return;
        }

        Vector3 screenPoint = new Vector3(
            Screen.width * 0.5f + playerBubbleScreenOffset.x,
            Screen.height * 0.5f + playerBubbleScreenOffset.y,
            Mathf.Max(0.01f, playerBubbleScreenOffset.z));

        playerBubbleAnchor.position = cam.ScreenToWorldPoint(screenPoint);
    }

    private static TMP_FontAsset TryLoadDialogueFont()
    {
        string[] paths = { "DialogueFont", "Fonts/DialogueFont", "Fonts/SCfont SDF" };
        foreach (string path in paths)
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(path);
            if (font != null)
            {
                return font;
            }
        }

        return null;
    }
}
