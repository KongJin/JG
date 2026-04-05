using Features.Player.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CreateManaBarUI
{
    [MenuItem("Tools/Create Mana Bar UI")]
    public static void Create()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in scene.");
            return;
        }

        // Delete existing if re-running
        var existing = GameObject.Find("ManaBar");
        if (existing != null)
            Object.DestroyImmediate(existing);

        // Create root
        var root = new GameObject("ManaBar", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);

        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 120f);
        rt.sizeDelta = new Vector2(300f, 20f);

        // Background
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.2f, 0.8f);

        // Slider
        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(root.transform, false);
        var sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = Vector2.zero;
        sliderRt.anchorMax = Vector2.one;
        sliderRt.sizeDelta = Vector2.zero;

        // Fill area
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;

        // Fill image
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.4f, 0.9f, 1f);

        var slider = sliderGo.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;

        // Add ManaBarView
        var manaBarView = root.AddComponent<ManaBarView>();

        // Set serialized fields via SerializedObject
        var so = new SerializedObject(manaBarView);
        so.FindProperty("_manaSlider").objectReferenceValue = slider;
        so.FindProperty("_manaFillImage").objectReferenceValue = fillImg;
        so.ApplyModifiedProperties();

        // Wire to GameSceneRoot
        var bootstrap = GameObject.Find("GameSceneRoot");
        if (bootstrap != null)
        {
            var comp = bootstrap.GetComponent<Features.Player.GameSceneRoot>();
            if (comp != null)
            {
                var bso = new SerializedObject(comp);
                bso.FindProperty("_manaBarView").objectReferenceValue = manaBarView;
                bso.ApplyModifiedProperties();
            }
        }

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[CreateManaBarUI] Mana bar created and wired successfully.");
    }
}
