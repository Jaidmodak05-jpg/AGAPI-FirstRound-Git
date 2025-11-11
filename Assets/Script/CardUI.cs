using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Assign in Prefab (ROOT Card)")]
    [SerializeField] GameObject front;     // child named "Front"
    [SerializeField] GameObject back;      // child named "Back"
    [SerializeField] Image frontImage;// Image under Front

    [Header("Flip Settings")]
    [SerializeField] float flipDuration = 0.22f;

    [HideInInspector] public GameControllerUI controller;  // set by GridGenerator
    [HideInInspector] public AudioManager audioMgr;    // set by GridGenerator

    public int CardId { get; private set; }
    public bool IsFaceUp { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsLocked { get; private set; }

    bool _animating = false;   // prevents overlapping animations

    void Awake()
    {
        ShowFace(false);
    }

    public void Init(int id, Sprite faceSprite)
    {
        CardId = id;
        if (frontImage) frontImage.sprite = faceSprite;

        IsFaceUp = false;
        IsMatched = false;
        IsLocked = false;
        _animating = false;

        ShowFace(false);
    }

    public void SetMatched()
    {
        IsMatched = true;
        IsLocked = true;
    }

    public void Lock(bool v) { IsLocked = v; }

    void ShowFace(bool faceUp)
    {
        if (front)
        {
            front.SetActive(faceUp);
            if (frontImage)
            {
                frontImage.enabled = faceUp;
                if (faceUp) frontImage.color = Color.white;
            }
            if (faceUp) front.transform.SetAsLastSibling();
        }
        if (back) back.SetActive(!faceUp);

        IsFaceUp = faceUp;
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (IsLocked || IsMatched || _animating) return;
        if (IsFaceUp) return; // already up
        if (controller && !controller.CanAcceptClick()) return;

        StartCoroutine(FlipAndNotify());
    }

    IEnumerator FlipAndNotify()
    {
        IsLocked = true;
        _animating = true;

        yield return StartCoroutine(FlipUp());

        _animating = false;
        IsLocked = false;

        if (controller) controller.OnCardFlippedUp(this);
    }

    public IEnumerator FlipUp()
    {
        if (IsFaceUp || IsMatched) yield break;

        Transform tr = transform;
        float half = Mathf.Max(0.01f, flipDuration * 0.5f);

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            tr.localScale = new Vector3(1f - (t / half), 1f, 1f);
            yield return null;
        }
        tr.localScale = new Vector3(0f, 1f, 1f);

        ShowFace(true);

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            tr.localScale = new Vector3((t / half), 1f, 1f);
            yield return null;
        }
        tr.localScale = Vector3.one;

        if (audioMgr) audioMgr.Flip();
    }

    public IEnumerator FlipDown()
    {
        if (!IsFaceUp || IsMatched) yield break;

        Transform tr = transform;
        float half = Mathf.Max(0.01f, flipDuration * 0.5f);

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            tr.localScale = new Vector3(1f - (t / half), 1f, 1f);
            yield return null;
        }
        tr.localScale = new Vector3(0f, 1f, 1f);

        ShowFace(false);

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            tr.localScale = new Vector3((t / half), 1f, 1f);
            yield return null;
        }
        tr.localScale = Vector3.one;

        if (audioMgr) audioMgr.Flip();
    }

    public IEnumerator Vanish()
    {
        // mark state
        IsLocked = true;
        IsMatched = true;

        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        float t = 0f;
        const float fadeDur = 0.20f; // ~0.2s
        while (t < fadeDur)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / fadeDur);
            yield return null;
        }

   
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // turn off visuals so nothing renders even if alpha is adjusted later
        if (frontImage) frontImage.enabled = false;
        if (front) front.SetActive(false);
        if (back) back.SetActive(false);

    }
}
