using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CrescentWreath.Client.UI
{
public sealed class CardInspectPreviewController : MonoBehaviour
{
    private static readonly List<RaycastResult> UiRaycastResults = new();
    private Camera? mainCamera;
    private CardInspectPreviewView? previewView;

    public void Configure(Camera? camera, CardInspectPreviewView view)
    {
        mainCamera = camera;
        previewView = view;
        previewView.Hide();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse is null || previewView is null)
        {
            return;
        }

        if (mouse.rightButton.wasReleasedThisFrame || !mouse.rightButton.isPressed)
        {
            previewView.Hide();
            return;
        }

        var pointerPosition = mouse.position.ReadValue();
        if (tryShowUiCardPreview(pointerPosition))
        {
            return;
        }

        if (isPointerBlockedByUi(pointerPosition))
        {
            previewView.Hide();
            return;
        }

        var cameraToUse = mainCamera != null ? mainCamera : Camera.main;
        if (cameraToUse is null)
        {
            previewView.Hide();
            return;
        }

        var ray = cameraToUse.ScreenPointToRay(pointerPosition);
        if (!Physics.Raycast(ray, out var hit, 500f))
        {
            previewView.Hide();
            return;
        }

        var cardView = hit.collider.GetComponentInParent<WorldCardView>();
        if (cardView is null || !cardView.CanInspect)
        {
            previewView.Hide();
            return;
        }

        previewView.Show(cardView.BuildInspectViewModel());
        previewView.SetScreenPosition(pointerPosition);
    }

    private bool tryShowUiCardPreview(Vector2 pointerPosition)
    {
        if (EventSystem.current is null || previewView is null)
        {
            return false;
        }

        UiRaycastResults.Clear();
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition,
        };
        EventSystem.current.RaycastAll(eventData, UiRaycastResults);
        foreach (var result in UiRaycastResults)
        {
            var inspectableCard = result.gameObject.GetComponentInParent<BattlefieldInspectableUiCard>();
            if (inspectableCard is not null &&
                inspectableCard.TryBuildInspectViewModel(out var uiInspectModel))
            {
                previewView.Show(uiInspectModel);
                previewView.SetScreenPosition(pointerPosition);
                return true;
            }

            var handCardButton = result.gameObject.GetComponentInParent<BattlefieldLocalHandCardButton>();
            if (handCardButton is null ||
                !handCardButton.TryBuildInspectViewModel(out var inspectModel))
            {
                continue;
            }

            previewView.Show(inspectModel);
            previewView.SetScreenPosition(pointerPosition);
            return true;
        }

        return false;
    }

    private static bool isPointerBlockedByUi(Vector2 pointerPosition)
    {
        if (EventSystem.current is null)
        {
            return false;
        }

        UiRaycastResults.Clear();
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition,
        };
        EventSystem.current.RaycastAll(eventData, UiRaycastResults);
        foreach (var result in UiRaycastResults)
        {
            if (result.gameObject.GetComponentInParent<Canvas>() is not null &&
                result.gameObject.GetComponentInParent<CardInspectPreviewView>() is null)
            {
                return true;
            }
        }

        return false;
    }
}
}
