using System.Collections;
using CrescentWreath.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrescentWreath.Client.Tests.PlayMode
{
public sealed class BattlefieldSceneSkeletonPlayModeTests
{
    [UnityTest]
    public IEnumerator GameClientScene_LoadsWithoutServerWithCameraTableAndIndependentOverlay()
    {
        var load = SceneManager.LoadSceneAsync("GameClient", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load!.isDone)
        {
            yield return null;
        }

        yield return null;

        var layout = Object.FindFirstObjectByType<BattlefieldLayoutRoot>();
        Assert.That(layout, Is.Not.Null);
        Assert.That(layout!.TableWorld, Is.Not.Null);
        Assert.That(layout.TableWorld!.MainCamera, Is.Not.Null);
        Assert.That(layout.TableWorld.MainCamera!.isActiveAndEnabled, Is.True);
        Assert.That(layout.TableWorld.TableSurface, Is.Not.Null);
        Assert.That(layout.ScreenHudCanvas, Is.Not.Null);
        Assert.That(layout.OverlayCanvas, Is.Not.Null);
        Assert.That(layout.OverlayCanvas!.sortingOrder, Is.GreaterThan(layout.ScreenHudCanvas!.sortingOrder));

        layout.SetOverlayPreviewVisible(true);
        Assert.That(layout.InteractionDimmer!.gameObject.activeSelf, Is.True);
        Assert.That(layout.InteractionDimmer.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(layout.InteractionDimmer.anchorMax, Is.EqualTo(Vector2.one));

        layout.SetOverlayPreviewVisible(false);
        Assert.That(layout.InteractionDimmer.gameObject.activeSelf, Is.False);

        layout.SetDebugVisible(true);
        Assert.That(layout.IsDebugVisible, Is.True);
        layout.SetDebugVisible(false);
        Assert.That(layout.IsDebugVisible, Is.False);
    }
}
}
