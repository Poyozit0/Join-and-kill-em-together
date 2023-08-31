namespace Jaket.UI;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using Jaket.Assets;
using Jaket.World;

using static System.Array;

/// <summary> Wheel for selecting emotions that will be displayed as an animation of the player doll. </summary>
[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class EmojiWheel : MonoSingleton<EmojiWheel>
{
    /// <summary> Whether emoji wheel is visible or hidden. </summary>
    public bool Shown;

    /// <summary> An array containing the rotation of all segments in degrees. </summary>
    public static float[] SegmentRotations = { -30f, 0f, 30f, -30f, 0f, 30f };
    /// <summary> List of all wheel segments. Needed to change the color of elements and store icons. </summary>
    public List<WheelSegment> Segments = new();

    /// <summary> Id of the selected segment, it will be highlighted in red. </summary>
    private int lastSelected, selected;
    /// <summary> Cursor direction relative to wheel center. </summary>
    private Vector2 direction;

    /// <summary> Creates a singleton of emoji wheel. </summary>
    public static void Build()
    {
        // initialize the singleton and create a canvas
        Utils.Canvas("Emoji Wheel", Plugin.Instance.transform).AddComponent<EmojiWheel>().gameObject.SetActive(false);

        // hide emoji wheel once loading a scene
        SceneManager.sceneLoaded += (scene, mode) => Instance.gameObject.SetActive(Instance.Shown = false);

        // build emoji wheel
        Utils.CircleShadow("Shadow", Instance.transform, 0f, 0f, 640f, 640f, 245f);

        for (int i = 0; i < 6; i++)
        {
            float deg = 150f - i * 60f, rad = deg * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 200f;

            var segment = new WheelSegment
            {
                segment = Utils.Circle("Segment " + i, Instance.transform, 0f, 0f, 150f, 150f, 1f / 6f, i * 60, 8f, true).GetComponent<UICircle>(),
                divider = Utils.Circle("Divider " + i, Instance.transform, 0f, 0f, 640f, 640f, .005f, i * 60, 245f, false).GetComponent<UICircle>(),

                iconGlow = Utils.Image("Glow", Instance.transform, pos.x, pos.y, 285f, 150f, Color.white).GetComponent<Image>(),
                icon = Utils.Image("Icon", Instance.transform, pos.x, pos.y, 285f, 150f, Color.white).GetComponent<Image>(),
            };

            segment.icon.rectTransform.localEulerAngles = new(0f, 0f, SegmentRotations[i]);
            segment.iconGlow.rectTransform.localEulerAngles = new(0f, 0f, SegmentRotations[i]);

            Instance.Segments.Add(segment);
            segment.SetActive(false);
        }

        Instance.Invoke("UpdateIcons", 5f);
    }

    public void Update()
    {
        // the weapon wheel should be unavailable while the emoji wheel is open
        WeaponWheel.Instance.gameObject.SetActive(false);

        // some code from the weapon wheel that I don't understand
        direction = Vector2.ClampMagnitude(direction + InputManager.Instance.InputSource.WheelLook.ReadValue<Vector2>(), 1f);
        float num = Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * 57.29578f + 90f, 360f);
        selected = direction.sqrMagnitude > 0f ? (int)(num / 60f) : selected;

        // update segments
        for (int i = 0; i < Segments.Count; i++) Segments[i].SetActive(i == selected);

        // play sound
        if (lastSelected != selected)
        {
            lastSelected = selected;
            Instantiate(WeaponWheel.Instance.clickSound);
        }
    }

    /// <summary> Updates the emoji icons if they are loaded, otherwise repeats the same actions after 5 seconds. </summary>
    public void UpdateIcons()
    {
        if (TrueForAll(DollAssets.EmojiIcons, tex => tex != null) && TrueForAll(DollAssets.EmojiGlows, tex => tex != null))
        {
            for (int i = 0; i < 6; i++)
            {
                Segments[i].icon.sprite = DollAssets.EmojiIcons[i];
                Segments[i].iconGlow.sprite = DollAssets.EmojiGlows[i];
            }
        }
        else Instance.Invoke("UpdateIcons", 5f);
    }

    /// <summary> Shows emoji selection wheel and resets the selected segment. </summary>
    public void Show()
    {
        // the wheel should be inaccessible in the tunnel between levels
        if (FinalRank.Instance.gameObject.activeInHierarchy || WeaponWheel.Instance.gameObject.activeSelf) return;

        gameObject.SetActive(Shown = true);
        CameraController.Instance.enabled = false;

        lastSelected = selected = -1;
        direction = Vector2.zero;
    }

    /// <summary> Hides emoji selection wheel and starts the selected animation. </summary>
    public void Hide()
    {
        gameObject.SetActive(Shown = false);
        CameraController.Instance.enabled = true;

        Movement.Instance.StartEmoji((byte)selected);
    }
}