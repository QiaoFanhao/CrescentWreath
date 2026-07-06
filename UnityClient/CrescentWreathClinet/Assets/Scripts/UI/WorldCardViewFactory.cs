using UnityEngine;

namespace CrescentWreath.Client.UI
{
public static class WorldCardViewFactory
{
    public const string CardBodyMaterialResourcesPath = "UI/Materials/WorldCardBodyMaterial";
    public const string SelectionMaterialResourcesPath = "UI/Materials/WorldCardSelectionMaterial";
    public const string HoverMaterialResourcesPath = "UI/Materials/WorldCardHoverMaterial";

    private static Material? cardBodyMaterial;
    private static Material? selectionMaterial;
    private static Material? hoverMaterial;

    public static WorldCardView Create(string name, Transform parent)
    {
        var cardObject = new GameObject(name);
        cardObject.transform.SetParent(parent, false);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        body.name = "CardBody";
        body.SetParent(cardObject.transform, false);
        body.localPosition = Vector3.zero;
        body.localScale = WorldCardView.CardBodyLocalScale;
        var bodyRenderer = body.GetComponent<MeshRenderer>();
        bodyRenderer.sharedMaterial = getCardBodyMaterial();
        bodyRenderer.enabled = false;
        destroy(body.GetComponent<Collider>());

        var front = createSpriteSurface("FrontSurface", cardObject.transform, 0.018f, false);
        var back = createSpriteSurface("BackSurface", cardObject.transform, 0.019f, true);

        var selection = createFrame(
            "SelectionOutline",
            cardObject.transform,
            1.08f,
            1.48f,
            0.035f,
            -0.006f,
            getSelectionMaterial());
        selection.SetActive(false);

        var hover = createFrame(
            "HoverHighlight",
            cardObject.transform,
            1.14f,
            1.54f,
            0.025f,
            -0.012f,
            getHoverMaterial());
        hover.SetActive(false);

        var collider = cardObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(1.08f, 0.1f, 1.48f);
        collider.center = Vector3.up * 0.02f;

        var view = cardObject.AddComponent<WorldCardView>();
        view.ConfigureForFactory(body, front, back, selection, hover, collider);
        cardObject.AddComponent<WorldCardPointerController>();
        return view;
    }

    private static SpriteRenderer createSpriteSurface(
        string name,
        Transform parent,
        float yOffset,
        bool flip)
    {
        var surfaceObject = new GameObject(name);
        surfaceObject.transform.SetParent(parent, false);
        surfaceObject.transform.localPosition = Vector3.up * yOffset;
        // Both prototype surfaces face the table camera. Actual flip animation can swap
        // active surfaces later without making the card back disappear in the greybox.
        surfaceObject.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        surfaceObject.transform.localScale = Vector3.one;
        var spriteRenderer = surfaceObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = flip ? 0 : 1;
        return spriteRenderer;
    }

    private static GameObject createPlate(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = name;
        plate.transform.SetParent(parent, false);
        plate.transform.localPosition = localPosition;
        plate.transform.localScale = localScale;
        plate.GetComponent<MeshRenderer>().sharedMaterial = material;
        destroy(plate.GetComponent<Collider>());
        return plate;
    }

    private static GameObject createFrame(
        string name,
        Transform parent,
        float width,
        float height,
        float thickness,
        float yOffset,
        Material material)
    {
        var frame = new GameObject(name);
        frame.transform.SetParent(parent, false);
        createPlate(
            "Top",
            frame.transform,
            new Vector3(0f, yOffset, height * 0.5f - thickness * 0.5f),
            new Vector3(width, 0.006f, thickness),
            material);
        createPlate(
            "Bottom",
            frame.transform,
            new Vector3(0f, yOffset, -height * 0.5f + thickness * 0.5f),
            new Vector3(width, 0.006f, thickness),
            material);
        createPlate(
            "Left",
            frame.transform,
            new Vector3(-width * 0.5f + thickness * 0.5f, yOffset, 0f),
            new Vector3(thickness, 0.006f, height),
            material);
        createPlate(
            "Right",
            frame.transform,
            new Vector3(width * 0.5f - thickness * 0.5f, yOffset, 0f),
            new Vector3(thickness, 0.006f, height),
            material);
        return frame;
    }

    private static void destroy(Object? target)
    {
        if (target is null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }

    private static Material getCardBodyMaterial()
    {
        return cardBodyMaterial ??= loadOrCreateMaterial(
            CardBodyMaterialResourcesPath,
            "WorldCardBodyMaterial",
            new Color(0.02f, 0.024f, 0.03f, 1f));
    }

    private static Material getSelectionMaterial()
    {
        return selectionMaterial ??= loadOrCreateMaterial(
            SelectionMaterialResourcesPath,
            "WorldCardSelectionMaterial",
            new Color(1f, 0.79f, 0.13f, 1f));
    }

    private static Material getHoverMaterial()
    {
        return hoverMaterial ??= loadOrCreateMaterial(
            HoverMaterialResourcesPath,
            "WorldCardHoverMaterial",
            new Color(0.12f, 0.82f, 0.95f, 0.78f));
    }

    private static Material loadOrCreateMaterial(string resourcesPath, string materialName, Color color)
    {
        var loaded = Resources.Load<Material>(resourcesPath);
        return loaded is not null
            ? loaded
            : createMaterial(materialName, color);
    }

    private static Material createMaterial(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Standard") ??
            Shader.Find("Unlit/Color");
        var material = new Material(shader)
        {
            name = name,
            color = color,
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }
}
}
