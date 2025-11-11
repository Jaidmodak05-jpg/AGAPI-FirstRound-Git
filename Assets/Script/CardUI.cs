using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Assign in Prefab (ROOT: Card)")]
    [SerializeField] GameObject front;           // child "Front"
    [SerializeField] GameObject back;            // child "Back"
    [SerializeField] Image frontImage;           // Image inside "Front"

    [Header("Optional")]
    [SerializeField] float flipDuration = 0.22f; // seconds

    // Set by spawner:
    [HideInInspector] public GameControllerUI controller;
    [HideInInspector] public AudioManager audioMgr;

    public int CardId { get; private set; } = -1;
    public bool IsFaceUp { get; private set; } = false;
    public bool IsMatched { get; private set; } = false;
    public bool IsLocked { get; private set; } = false;

    public void Init(int id, Sprite face, GameControllerUI owner, AudioManager audio)
    {
        CardId = id;
        controller = owner;
        audioMgr = audio;
        SetFrontSprite(face);
        ShowFace(false, instant: true);
        IsMatched = false;
        IsLocked = false;
    }

    // --- Public helpers used by Grid/Game ---
    public void SetFrontSprite(Sprite face)
    {
        if (frontImage) frontImage.sprite = face;
    }

    public void ShowFace(bool faceUp, bool instant = false)
    {
        IsFaceUp = faceUp;
        if (instant)
        {
            if (front) front.SetActive(faceUp);
            if (back) back.SetActive(!faceUp);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(faceUp ? FlipUp() : FlipDown());
    }

    public IEnumerator FlipUp()
    {
        if (IsLocked || IsMatched || IsFaceUp) yield break;
        IsLocked = true;

        // simple swap (UI) – you can replace by animation later
        if (back) back.SetActive(false);
        if (front) front.SetActive(true);

        audioMgr?.Flip();
        yield return new WaitForSeconds(flipDuration);

        IsFaceUp = true;
        IsLocked = false;
    }

    public IEnumerator FlipDown()
    {
        if (IsLocked || !IsFaceUp || IsMatched) yield break;
        IsLocked = true;

        if (front) front.SetActive(false);
        if (back) back.SetActive(true);

        yield return new WaitForSeconds(flipDuration);

        IsFaceUp = false;
        IsLocked = false;
    }

    public IEnumerator Vanish()
    {
        // mark state
        IsLocked = true;
        IsMatched = true;

        // fade out but keep layout slot
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        float t = 0f;
        const float fadeDur = 0.20f;
        while (t < fadeDur)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / fadeDur);
            yield return null;
        }
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        if (frontImage) frontImage.enabled = false;
        if (front) front.SetActive(false);
        if (back) back.SetActive(false);
        // DO NOT deactivate this GameObject – keeps its slot in the GridLayoutGroup
    }

    // --- Clicks ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!controller || !controller.CanAcceptClick() || IsLocked || IsMatched || IsFaceUp) return;

        // do the visual flip right away
        StartCoroutine(FlipUp());
        // let controller resolve pairs
        controller.OnCardFlippedUp(this);
    }
}
