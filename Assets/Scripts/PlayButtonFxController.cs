using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PlayButtonFxController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private GameObject idleFxObject;
    [SerializeField] private GameObject pressedFxObject;
    [SerializeField] private ParticleSystem pressedFxParticleSystem;

    private void Awake()
    {
        ResolveReferences();
        HidePressedFx();
    }

    private void OnDisable()
    {
        HidePressedFx();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ResolveReferences();
        if (pressedFxObject == null)
        {
            return;
        }

        pressedFxObject.SetActive(true);

        if (pressedFxParticleSystem == null)
        {
            pressedFxParticleSystem = pressedFxObject.GetComponentInChildren<ParticleSystem>(true);
        }

        if (pressedFxParticleSystem != null)
        {
            pressedFxParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            pressedFxParticleSystem.Play(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        HidePressedFx();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HidePressedFx();
    }

    private void ResolveReferences()
    {
        if (idleFxObject == null || pressedFxObject == null)
        {
            var startScreen = transform.parent;
            var fxAnchor = startScreen != null ? startScreen.Find("FxAnchor") : null;
            if (fxAnchor != null)
            {
                idleFxObject ??= fxAnchor.Find("UiFxButton")?.gameObject;
                pressedFxObject ??= fxAnchor.Find("UiFxButtonPushed")?.gameObject;
            }
        }

        if (pressedFxParticleSystem == null && pressedFxObject != null)
        {
            pressedFxParticleSystem = pressedFxObject.GetComponentInChildren<ParticleSystem>(true);
        }
    }

    private void HidePressedFx()
    {
        ResolveReferences();
        if (pressedFxObject != null)
        {
            pressedFxObject.SetActive(false);
        }
    }
}
