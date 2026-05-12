using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Chapter1 : MonoBehaviour
{
    [Header("字体")]
    [SerializeField] private TMP_FontAsset dialogueFontAsset;

    [Header("台词")]
    [SerializeField] private bool useScriptDefaultLines = true;
    [SerializeField] private string dialogueId = "chapter1_intro";
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

    [Header("点击推进")]
    [SerializeField] private int mouseButton = 0;

    [Header("音乐")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private string musicResourcePath = "StoryAudio/BGM/rain";
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField] private bool loopMusic = true;

    private AudioSource audioSource;
    private Canvas overlayCanvas;
    private TextMeshProUGUI npcText;
    private CanvasGroup npcTextCanvasGroup;
    private bool waitingForAdvanceClick;
    private int currentLineIndex = -1;
    private bool isConversationActive;
    private Coroutine npcTypingRoutine;
    private bool isNpcTyping;
    private string currentNpcFullText = string.Empty;

    private void Awake()
    {
        ApplyDefaultLinesIfNeeded();
        EnsureDialogueRunner();
        EnsurePlayerAnchor();
        EnsureNpcTextUi();
        EnsureAudioSource();
        ApplyFont();
        ApplyNpcTextStyle();
        UpdatePlayerBubbleAnchor();
        HideNpcTextImmediate();
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

        if (audioSource != null)
        {
            audioSource.volume = musicVolume;
            audioSource.loop = loopMusic;
        }

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

        PlayMusic();
        StartDialogue();
    }

    private void Update()
    {
        UpdatePlayerBubbleAnchor();

        if (!isConversationActive || !Input.GetMouseButtonDown(mouseButton))
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

        IReadOnlyList<DialogueLine> source = Get(dialogueId);
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
            playerBubbleAnchor = CreateChildAnchor("Chapter1PlayerBubbleAnchor");
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
            Transform existingCanvas = transform.Find("Chapter1Canvas");
            if (existingCanvas != null)
            {
                overlayCanvas = existingCanvas.GetComponent<Canvas>();
            }
        }

        if (overlayCanvas == null)
        {
            GameObject canvasObject = new GameObject("Chapter1Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

    private void EnsureAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = loopMusic;
        audioSource.volume = musicVolume;
    }

    private void PlayMusic()
    {
        if (audioSource == null)
        {
            return;
        }

        AudioClip clip = backgroundMusic;
        if (clip == null && !string.IsNullOrWhiteSpace(musicResourcePath))
        {
            clip = Resources.Load<AudioClip>(musicResourcePath);
        }

        if (clip == null)
        {
            return;
        }

        audioSource.clip = clip;
        audioSource.loop = loopMusic;
        audioSource.volume = musicVolume;
        audioSource.Play();
    }

    private void StartDialogue()
    {
        currentLineIndex = -1;
        isConversationActive = lines != null && lines.Count > 0;
        waitingForAdvanceClick = false;
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

    public static IReadOnlyList<DialogueLine> Get(string id)
    {
        switch (id)
        {
            case "chapter1_intro":
                return Intro();
            default:
                return null;
        }
    }

    private static IReadOnlyList<DialogueLine> Intro()
    {
        return new List<DialogueLine>
        {
            Npc("第三小队，东侧防区出现少量变异体。"),
            Npc("清理者，凌，编号Urzu7，请立即前往指定火力点，接管外墙防御单元。"),
            Npc("目标威胁等级：低。执行标准清理流程。"),
            Player("收到！为了伊甸！"),
        };
    }

    private static DialogueLine Npc(string text, DialogueEmphasis? emphasis = null)
    {
        return new DialogueLine
        {
            speaker = DialogueSpeaker.NPC,
            text = text,
            emphasis = emphasis ?? Normal()
        };
    }

    private static DialogueLine Player(string text, DialogueEmphasis? emphasis = null)
    {
        return new DialogueLine
        {
            speaker = DialogueSpeaker.Player,
            text = text,
            emphasis = emphasis ?? Normal()
        };
    }

    private static DialogueEmphasis Normal()
    {
        return new DialogueEmphasis { enabled = false, scaleMultiplier = 1.25f, shakeMagnitude = 0.08f };
    }

    private static DialogueEmphasis Strong(float scaleMultiplier = 1.4f, float shakeMagnitude = 0.12f)
    {
        return new DialogueEmphasis { enabled = true, scaleMultiplier = scaleMultiplier, shakeMagnitude = shakeMagnitude };
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
