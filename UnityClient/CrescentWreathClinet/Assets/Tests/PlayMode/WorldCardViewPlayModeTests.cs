using System.Collections;
using System.Linq;
using CrescentWreath.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace CrescentWreath.Client.Tests.PlayMode
{
public sealed class WorldCardViewPlayModeTests
{
    [UnityTest]
    public IEnumerator WorldCardView_AnimateTo_CompletesAtTargetPose()
    {
        var root = new GameObject("WorldCardAnimateRoot");
        try
        {
            var view = WorldCardViewFactory.Create("WorldCardView", root.transform);
            var targetPose = new CardPose(
                new Vector3(1.5f, 0.1f, -0.75f),
                Quaternion.Euler(0f, 25f, 0f),
                new Vector3(0.7f, 0.02f, 1f));
            var settings = new CardAnimationSettings
            {
                moveDuration = 0.04f,
                moveHeightArc = 0f,
                staggerInterval = 0f,
            };

            view.AnimateTo(targetPose, settings.moveDuration, settings);
            yield return new WaitForSeconds(0.08f);

            Assert.That(view.transform.localPosition, Is.EqualTo(targetPose.Position).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(view.transform.localScale, Is.EqualTo(targetPose.Scale).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(view.transform.localRotation.eulerAngles.y, Is.EqualTo(25f).Within(0.01f));
        }
        finally
        {
            Object.Destroy(root);
        }
    }

    [UnityTest]
    public IEnumerator WorldCardView_AnimateTo_InterruptsPreviousMotion()
    {
        var root = new GameObject("WorldCardInterruptRoot");
        try
        {
            var view = WorldCardViewFactory.Create("WorldCardView", root.transform);
            var settings = new CardAnimationSettings
            {
                moveDuration = 0.2f,
                moveHeightArc = 0f,
                staggerInterval = 0f,
            };
            var firstTarget = new CardPose(new Vector3(4f, 0f, 0f), Quaternion.identity, Vector3.one);
            var secondTarget = new CardPose(new Vector3(-1f, 0.1f, 2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.75f);

            view.AnimateTo(firstTarget, settings.moveDuration, settings);
            yield return null;
            view.AnimateTo(secondTarget, 0.04f, settings);
            yield return new WaitForSeconds(0.08f);

            Assert.That(view.transform.localPosition, Is.EqualTo(secondTarget.Position).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(view.transform.localScale, Is.EqualTo(secondTarget.Scale).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(view.transform.localRotation.eulerAngles.y, Is.EqualTo(90f).Within(0.01f));
        }
        finally
        {
            Object.Destroy(root);
        }
    }

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
    public IEnumerator InteractableCardClick_TogglesSelectedWithoutLegacyDetailPanel()
    {
        yield return loadGameClient();

        var demo = Object.FindFirstObjectByType<BattlefieldWorldCardDemoController>()!;
        var view = demo.LayoutByZoneKey["playerField:bottom"].OrderedViews[0];

        Assert.That(view.IsSelected, Is.False);
        view.NotifyClicked();

        Assert.That(view.IsSelected, Is.True);
        Assert.That(Resources.FindObjectsOfTypeAll<GameObject>()
            .Any(obj => obj.name == "CardDetailPanel"), Is.False);
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
        Assert.That(view.transform.Find("VisualRoot/FrontSurface").gameObject.activeSelf, Is.False);
        Assert.That(view.transform.Find("VisualRoot/BackSurface").gameObject.activeSelf, Is.True);
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
