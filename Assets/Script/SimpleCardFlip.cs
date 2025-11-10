using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimpleCardFlip : MonoBehaviour, IPointerClickHandler
{
    [Header("Assign these in the Card prefab")]
    public GameObject front;      // parent GO "Front"
    public GameObject back;       // parent GO "Back"
    public Image frontImage; // Image on Front (e.g., Front/frontImage)

    // optional: show this sprite as the front picture (or set it at runtime)
    public Sprite initialSprite;

    void Awake()
    {
        // sanity defaults: start face-down
        if (front) front.SetActive(false);
        if (back) back.SetActive(true);

        if (frontImage && initialSprite) frontImage.sprite = initialSprite;
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (!front || !back) return;

        bool toFaceUp = !front.activeSelf; // toggle
        front.SetActive(toFaceUp);
        back.SetActive(!toFaceUp);
    }
}
