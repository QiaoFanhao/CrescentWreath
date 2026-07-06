using System.Collections;
using System.Linq;
using CrescentWreath.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrescentWreath.Client.Tests.PlayMode
{
public sealed class WorldCardViewPlayModeTests
{
    [UnityTest]
    public IEnumerator GameClientScene_DisplaysWorldCardFakeDataWithoutHost()
    {
        yield return loadGameClient();

        var demo = Object.FindFirstObjectByType<BattlefieldWorldCardDemoController>();

        Assert.That(demo, Is.Not.Null);
        Assert.That(demo!.LayoutByZoneKey["publicTreasureDeck"].OrderedViews, Has.Count.GreaterThanOrEqualTo(3));
        Assert.That(demo.LayoutByZoneKey["currentAnomaly"].OrderedViews, Has.Count.EqualTo(1));
        Assert.That(demo.LayoutByZoneKey["currentPlayerMirror"].OrderedViews, Has.Count.GreaterThanOrEqualTo(10));
    }

    [UnityTest]
    public IEnumerator InteractableCardClick_TogglesSelectedAndUpdatesDetail()
    {
        yield return loadGameClient();

        var demo = Object.FindFirstObjectByType<BattlefieldWorldCardDemoController>()!;
        var view = demo.LayoutByZoneKey["playerField:bottom"].OrderedViews[0];
        var detailText = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .FirstOrDefault(text => text.name == "Label" && text.transform.parent?.name == "CardDetailPanel");

        Assert.That(view.IsSelected, Is.False);
        view.NotifyClicked();

        Assert.That(view.IsSelected, Is.True);
        Assert.That(detailText, Is.Not.Null);
        Assert.That(detailText!.text, Does.Contain(view.DefinitionId));
    }

    [UnityTest]
    public IEnumerator ReadOnlyMirrorClick_UpdatesDetailWithoutTogglingSelection()
    {
        yield return loadGameClient();

        var demo = Object.FindFirstObjectByType<BattlefieldWorldCardDemoController>()!;
        var view = demo.LayoutByZoneKey["currentPlayerMirror"].OrderedViews[0];

        Assert.That(view.IsReadOnly, Is.True);
        Assert.That(view.IsSelected, Is.False);
        view.NotifyClicked();

        Assert.That(view.IsSelected, Is.False);
    }

    [UnityTest]
    public IEnumerator FaceDownDeckCards_ShowBackSurface()
    {
        yield return loadGameClient();

        var demo = Object.FindFirstObjectByType<BattlefieldWorldCardDemoController>()!;
        var view = demo.LayoutByZoneKey["publicTreasureDeck"].OrderedViews[0];

        Assert.That(view.IsFaceUp, Is.False);
        Assert.That(view.transform.Find("FrontSurface").gameObject.activeSelf, Is.False);
        Assert.That(view.transform.Find("BackSurface").gameObject.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator RefreshFakeData_ReusesExistingCardViews()
    {
        yield return loadGameClient();

        var demo = Object.FindFirstObjectByType<BattlefieldWorldCardDemoController>()!;
        var firstView = demo.LayoutByZoneKey["playerField:bottom"].OrderedViews[0];

        demo.RefreshFakeData();

        Assert.That(demo.LayoutByZoneKey["playerField:bottom"].OrderedViews[0], Is.SameAs(firstView));
    }

    private static IEnumerator loadGameClient()
    {
        var load = SceneManager.LoadSceneAsync("GameClient", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load!.isDone)
        {
            yield return null;
        }

        yield return null;
    }
}
}
