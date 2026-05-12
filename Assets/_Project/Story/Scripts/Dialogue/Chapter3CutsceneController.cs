using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 第三章过场：Chen 从场外向左走入时播放 WalkLeft；到位后切到 Idle；
/// 对话结束后向右离场时播放 WalkRight，并在离场后加载下一关。
/// </summary>
public sealed class Chapter3CutsceneController : MonoBehaviour
{
    [Header("对白")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private string dialogueId = "chapter3_Chen";

    [Header("Chen")]
    [SerializeField] private Transform chen;
    [SerializeField] private Transform chenStopPoint;
    [SerializeField] private Chapter3ChenStopConfig chenStopConfig;
    [SerializeField] private Animator chenAnimator;
    [SerializeField] private Vector2 chenStopOffsetFromPlayer = new(-1.25f, 0f);
    [SerializeField] private float chenMoveSpeed = 3.5f;
    [SerializeField] private float chenArriveEpsilon = 0.04f;
    [SerializeField] private float chenStartExtraRightBeyondCamera = 0.75f;
    [SerializeField] private float offscreenMarginWorld = 2.5f;

    [Header("Chen 动画状态")]
    [SerializeField] private string walkLeftStateName = "ChenWalkLeft";
    [SerializeField] private string idleStateName = "ChenIdle";
    [SerializeField] private string walkRightStateName = "ChenWalkRight";

    [Header("玩家")]
    [SerializeField] private PlayerInteractor2D playerInteractor;

    [Header("切换至下一章")]
    [SerializeField] private string nextSceneName = "chapter3";
    [SerializeField] private float fadeOutToBlackDuration = 0.75f;
    [SerializeField] private float fadeInFromBlackDuration = 0.75f;

    private void Reset()
    {
        dialogueRunner = GetComponent<DialogueRunner>();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();

        if (dialogueRunner == null || playerInteractor == null || chen == null)
        {
            Debug.LogError("Chapter3CutsceneController: 缺少 DialogueRunner / PlayerInteractor2D / Chen。", this);
            return;
        }

        NpcDialogue npcDialogue = chen.GetComponent<NpcDialogue>();
        if (npcDialogue != null)
        {
            npcDialogue.enabled = false;
        }

        playerInteractor.SetInteractionInputEnabled(false);
        playerInteractor.SetInteractionPromptVisible(false);
        if (playerInteractor.Motor != null)
        {
            playerInteractor.Motor.SetMovementLocked(true);
        }

        StartCoroutine(EnterThenDialogueRoutine());
    }

    private void ResolveReferences()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = GetComponent<DialogueRunner>();
        }

        if (chen == null)
        {
            GameObject go = GameObject.Find("Chen");
            if (go != null)
            {
                chen = go.transform;
            }
        }

        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor2D>();
        }

        if (chen != null && chenStopConfig == null)
        {
            chenStopConfig = chen.GetComponent<Chapter3ChenStopConfig>();
        }

        if (chen != null && chenAnimator == null)
        {
            chenAnimator = chen.GetComponentInChildren<Animator>(true);
        }
    }

    private IEnumerator EnterThenDialogueRoutine()
    {
        yield return ChenEnterRoutine();

        IReadOnlyList<DialogueLine> read = DialogueScripts.Get(dialogueId);
        if (read == null || read.Count == 0)
        {
            Debug.LogError($"Chapter3CutsceneController: 对白 id「{dialogueId}」无内容。", this);
            yield break;
        }

        IList<DialogueLine> lines = read as IList<DialogueLine> ?? new List<DialogueLine>(read);
        PlayChenAnimationState(idleStateName);
        dialogueRunner.PlayConversation(
            playerInteractor,
            playerInteractor.transform,
            chen,
            lines,
            OnConversationComplete);
    }

    private IEnumerator ChenEnterRoutine()
    {
        if (chen == null)
        {
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            yield break;
        }

        Vector3 playerPosition = playerInteractor != null ? playerInteractor.transform.position : Vector3.zero;
        Vector3 target = ResolveChenStopTarget(playerPosition);

        float halfWidth = cam.orthographic ? cam.orthographicSize * cam.aspect : 5f;
        float startX = cam.transform.position.x + halfWidth + chenStartExtraRightBeyondCamera;
        chen.position = new Vector3(startX, target.y, chen.position.z);

        PlayChenAnimationState(walkLeftStateName);

        while (Vector3.Distance(
                   new Vector3(chen.position.x, chen.position.y, 0f),
                   new Vector3(target.x, target.y, 0f)) > chenArriveEpsilon)
        {
            chen.position = Vector3.MoveTowards(chen.position, target, chenMoveSpeed * Time.deltaTime);
            yield return null;
        }

        chen.position = new Vector3(target.x, target.y, chen.position.z);
        PlayChenAnimationState(idleStateName);
    }

    private Vector3 ResolveChenStopTarget(Vector3 playerPosition)
    {
        if (chenStopConfig != null)
        {
            Vector2 stopPosition = chenStopConfig.StopPosition;
            return new Vector3(stopPosition.x, stopPosition.y, chen.position.z);
        }

        if (chenStopPoint != null)
        {
            return new Vector3(chenStopPoint.position.x, chenStopPoint.position.y, chen.position.z);
        }

        return new Vector3(
            playerPosition.x + chenStopOffsetFromPlayer.x,
            playerPosition.y + chenStopOffsetFromPlayer.y,
            chen.position.z);
    }

    private void PlayChenAnimationState(string stateName)
    {
        if (chenAnimator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        chenAnimator.enabled = true;
        chenAnimator.Play(stateName, 0, 0f);
        chenAnimator.Update(0f);
    }

    private void OnConversationComplete()
    {
        if (playerInteractor != null && playerInteractor.Motor != null)
        {
            playerInteractor.Motor.SetMovementLocked(true);
        }

        if (chen != null)
        {
            CutsceneCoroutineUtility.StartPreferredOrFallback(this, playerInteractor, ChenExitAndLoadNextRoutine());
        }
        else
        {
            LoadNext();
        }
    }

    private IEnumerator ChenExitAndLoadNextRoutine()
    {
        PlayChenAnimationState(walkRightStateName);

        Camera cam = Camera.main;
        while (chen != null)
        {
            chen.position += Vector3.right * (chenMoveSpeed * Time.deltaTime);
            if (cam != null && cam.orthographic)
            {
                float halfW = cam.orthographicSize * cam.aspect;
                if (chen.position.x >= cam.transform.position.x + halfW + offscreenMarginWorld)
                {
                    break;
                }
            }
            else
            {
                yield return new WaitForSeconds(2f);
                break;
            }

            yield return null;
        }

        LoadNext();
    }

    private void LoadNext()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            return;
        }

        ScreenFadeTransition.Play(nextSceneName, fadeOutToBlackDuration, fadeInFromBlackDuration);
    }
}
