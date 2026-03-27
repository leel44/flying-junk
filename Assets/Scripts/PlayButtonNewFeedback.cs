using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PlayButtonNewFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private GameObject buttonBaseVisual;
    [SerializeField] private GameObject buttonVisual;
    [SerializeField] private GameObject buttonPushedVisual;

    private void Awake()
    {
        ApplyNormalVisual();
    }

    private void OnDisable()
    {
        ApplyNormalVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyPressedVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void Release()
    {
        ApplyNormalVisual();
    }

    private void ApplyNormalVisual()
    {
        if (buttonBaseVisual != null)
        {
            buttonBaseVisual.SetActive(true);
        }
        if (buttonVisual != null)
        {
            buttonVisual.SetActive(true);
        }

        if (buttonPushedVisual != null)
        {
            buttonPushedVisual.SetActive(false);
        }
    }

    private void ApplyPressedVisual()
    {
        if (buttonBaseVisual != null)
        {
            buttonBaseVisual.SetActive(true);
        }
        if (buttonVisual != null)
        {
            buttonVisual.SetActive(false);
        }

        if (buttonPushedVisual != null)
        {
            buttonPushedVisual.SetActive(true);
        }
    }
}
