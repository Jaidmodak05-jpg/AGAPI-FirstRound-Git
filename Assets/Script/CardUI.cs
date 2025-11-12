using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Assign in Prefab (root = Card)")]
    [SerializeField] GameObject front;     // child named "Front"
    [SerializeField] GameObject back;      // child named "Back"
    [SerializeField] Image     frontImage; // Image attached to Front (to show the face)

    [Header("Optional")]
    [SerializeField] float flipDuration = 0.22f;

    [HideInInspector] public GameControllerUI controller; // set by spawner
    [HideInInspector] public AudioManager audioMgr;        // optional

    public int CardId    { get; private set; } = -1;
    public bool IsFaceUp { get; private set; } = false;
    public bool IsMatched{ get; private set; } = false;
    public bool IsLocked { get; private set; } = false;

    public void Init(int id, Sprite face, GameControllerUI owner, AudioManager audio)
    {
        CardId = id;
        controller = owner;
        audioMgr = audio;
        if (frontImage) frontImage.sprite = face;
        ShowBackFaceInstant();
        IsMatched = false;
        IsLocked  = false;
    }

    public void SetFrontSprite(Sprite s)
    {
        if (frontImage) frontImage.sprite = s;
    }

    public void MarkMatched()
    {
        IsMatched = true;
        IsLocked  = true;
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (IsLocked || IsMatched) return;
        if (controller == null)   return;
        if (!controller.CanAcceptClick()) return;

        StartCoroutine(FlipUp());
        controller.OnCardFlippedUp(this);
    }

    void ShowBackFaceInstant()
    {
        if (front) front.SetActive(false);
        if (back)  back.SetActive(true);
        IsFaceUp = false;
    }

    public IEnumerator FlipUp()
    {
        if (IsFaceUp || IsLocked) yield break;
        IsLocked = true;
        if (audioMgr) audioMgr.Flip();
        yield return AnimateFlip(false);    // back -> edge
        if (back)  back.SetActive(false);
        if (front) front.SetActive(true);
        yield return AnimateFlip(true);     // edge -> front
        IsFaceUp = true;
        IsLocked = false;
    }

    public IEnumerator FlipDown()
    {
        if (!IsFaceUp || IsLocked || IsMatched) yield break;
        IsLocked = true;
        yield return AnimateFlip(false); // front -> edge
        if (front) front.SetActive(false);
        if (back)  back.SetActive(true);
        yield return AnimateFlip(true);  // edge -> back
        IsFaceUp = false;
        IsLocked = false;
    }

    IEnumerator AnimateFlip(bool opening)
    {
        float t = 0f;
        while (t < flipDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flipDuration);
            float scaleX = opening ? Mathf.SmoothStep(0f, 1f, k) : Mathf.SmoothStep(1f, 0f, k);
            var rt = (RectTransform)transform;
            rt.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }
    }

    // Called by controller when matched — fade but keep layout slot.
    public IEnumerator Vanish()
    {
        MarkMatched();
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        float t = 0f, dur = 0.2f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / dur);
            yield return null;
        }
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Disable visuals; keep the object active so GridLayoutGroup preserves the slot.
        if (frontImage) frontImage.enabled = false;
        if (front) front.SetActive(false);
        if (back)  back.SetActive(false);
    }
}
