using System;
using System.Collections.Generic;
using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldCharacterMarkerView : MonoBehaviour
{
    private const string PrimaryFontResourcePath = "UI/Fonts/SourceHanSansSC-Regular SDF";
    private const float MarkerRadius = 0.19f;
    private const float MarkerGap = 0.34f;
    private const int MarkersPerRow = 5;

    private static readonly Dictionary<string, Material> materialByMarkerKey = new(StringComparer.Ordinal);
    private static TMP_FontAsset? primaryFont;

    private readonly Dictionary<string, GameObject> markerObjectByKey = new(StringComparer.Ordinal);

    public void Reconcile(
        IReadOnlyList<ProjectionMarkerViewModel> markers,
        Vector3 cardLocalRotation)
    {
        transform.localEulerAngles = cardLocalRotation;

        var expectedKeys = new HashSet<string>(StringComparer.Ordinal);
        var visualIndex = 0;
        foreach (var marker in markers)
        {
            if (marker.count <= 0 || string.IsNullOrWhiteSpace(marker.markerTypeKey))
            {
                continue;
            }

            for (var countIndex = 0; countIndex < marker.count; countIndex += 1)
            {
                var visualKey = marker.markerTypeKey + ":" + countIndex.ToString();
                expectedKeys.Add(visualKey);
                var markerObject = getOrCreateMarkerObject(visualKey, marker.markerTypeKey);
                markerObject.transform.localPosition = calculateMarkerLocalPosition(visualIndex);
                updateLabel(markerObject, marker.markerTypeKey, countIndex + 1, marker.count);
                visualIndex += 1;
            }
        }

        foreach (var existingKey in new List<string>(markerObjectByKey.Keys))
        {
            if (expectedKeys.Contains(existingKey))
            {
                continue;
            }

            destroy(markerObjectByKey[existingKey]);
            markerObjectByKey.Remove(existingKey);
        }
    }

    public void Clear()
    {
        foreach (var markerObject in markerObjectByKey.Values)
        {
            destroy(markerObject);
        }

        markerObjectByKey.Clear();
    }

    private GameObject getOrCreateMarkerObject(string visualKey, string markerTypeKey)
    {
        if (markerObjectByKey.TryGetValue(visualKey, out var existing) && existing is not null)
        {
            return existing;
        }

        var markerObject = new GameObject("Marker_" + visualKey);
        markerObject.transform.SetParent(transform, false);

        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Orb";
        orb.transform.SetParent(markerObject.transform, false);
        orb.transform.localPosition = Vector3.zero;
        orb.transform.localScale = Vector3.one * MarkerRadius;
        var renderer = orb.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = getMarkerMaterial(markerTypeKey);
        destroy(orb.GetComponent<Collider>());

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(markerObject.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        labelObject.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        var label = labelObject.AddComponent<TextMeshPro>();
        label.font = getPrimaryFont();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 2.8f;
        label.fontStyle = FontStyles.Bold;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.color = Color.white;

        markerObjectByKey[visualKey] = markerObject;
        return markerObject;
    }

    private static Vector3 calculateMarkerLocalPosition(int visualIndex)
    {
        var column = visualIndex % MarkersPerRow;
        var row = visualIndex / MarkersPerRow;
        return new Vector3(
            -0.68f + column * MarkerGap,
            0.16f,
            0.46f - row * 0.32f);
    }

    private static void updateLabel(GameObject markerObject, string markerTypeKey, int countIndex, int count)
    {
        var label = markerObject.transform.Find("Label")?.GetComponent<TextMeshPro>();
        if (label is null)
        {
            return;
        }

        label.text = count > 1 ? countIndex.ToString() : getMarkerGlyph(markerTypeKey);
    }

    private static string getMarkerGlyph(string markerTypeKey)
    {
        return markerTypeKey switch
        {
            "dream" => "梦",
            "swordAura" => "剑",
            "destruction" => "毁",
            "metal" => "金",
            "wood" => "木",
            "water" => "水",
            "fire" => "火",
            "earth" => "土",
            "doll" => "偶",
            "divinity" => "神",
            "jewel" => "珠",
            _ => "●",
        };
    }

    private static Material getMarkerMaterial(string markerTypeKey)
    {
        if (materialByMarkerKey.TryGetValue(markerTypeKey, out var material))
        {
            return material;
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit") ??
                                Shader.Find("Standard") ??
                                Shader.Find("Unlit/Color"))
        {
            name = "MarkerMaterial_" + markerTypeKey,
            color = getMarkerColor(markerTypeKey),
        };
        material.SetFloat("_Smoothness", 0.62f);
        material.SetFloat("_Metallic", 0.16f);
        materialByMarkerKey[markerTypeKey] = material;
        return material;
    }

    private static Color getMarkerColor(string markerTypeKey)
    {
        return markerTypeKey switch
        {
            "dream" => new Color(0.82f, 0.38f, 1f, 1f),
            "swordAura" => new Color(0.35f, 0.82f, 1f, 1f),
            "destruction" => new Color(1f, 0.18f, 0.12f, 1f),
            "metal" => new Color(0.86f, 0.86f, 0.78f, 1f),
            "wood" => new Color(0.25f, 0.82f, 0.28f, 1f),
            "water" => new Color(0.18f, 0.48f, 1f, 1f),
            "fire" => new Color(1f, 0.35f, 0.08f, 1f),
            "earth" => new Color(0.72f, 0.46f, 0.2f, 1f),
            "doll" => new Color(1f, 0.65f, 0.82f, 1f),
            "divinity" => new Color(1f, 0.86f, 0.2f, 1f),
            "jewel" => new Color(0.2f, 1f, 0.86f, 1f),
            _ => new Color(0.95f, 0.95f, 0.95f, 1f),
        };
    }

    private static TMP_FontAsset? getPrimaryFont()
    {
        if (primaryFont is not null)
        {
            return primaryFont;
        }

        primaryFont = Resources.Load<TMP_FontAsset>(PrimaryFontResourcePath);
        return primaryFont;
    }

    private static void destroy(UnityEngine.Object? target)
    {
        if (target is null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
}
