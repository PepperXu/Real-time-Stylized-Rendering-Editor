using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class StylisedLineController : MonoBehaviour
{

    public static StylisedLineController Instance { get; private set; }

    [SerializeField] private Slider thicknessSlider, radiusSlider, animSlider;
    public GameObject editingSphere;
    public GameObject animPanel, controlPanel, outlinePanel;
    public GameObject lineObjectMenuItem;

    private int menuItemCounter = 0;

    public enum SliderType
    {
        Thickness,
        Radius,
        Anim
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        } else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (!thicknessSlider)
            Debug.LogError("Thickness slider not assigned");
        if (!radiusSlider)
            Debug.LogError("Radius slider not assigned");
        if (!animSlider)
            Debug.LogError("Animation slider not assigned");

        editingSphere.SetActive(false);
        animPanel.SetActive(false);
        controlPanel.SetActive(false);
        outlinePanel.SetActive(true);

        StylisedLineRenderer[] lineObjects = GameObject.FindObjectsOfType<StylisedLineRenderer>();


        foreach (StylisedLineRenderer lineObject in lineObjects)
        {
            GameObject obj = GameObject.Instantiate(lineObjectMenuItem, outlinePanel.transform);
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - 25 * menuItemCounter);
            StylisedLineMenuItem item = obj.GetComponent<StylisedLineMenuItem>();
            item.ChangeName(ActualName( lineObject.name));
            item.linkedLineObject = lineObject;
            menuItemCounter++;
        }

    }

    public float GetSliderValue(SliderType sliderType)
    {
        switch (sliderType)
        {
            case SliderType.Thickness:
                return thicknessSlider.value;
            case SliderType.Radius:
                return radiusSlider.value;
            case SliderType.Anim:
                return animSlider.value;
            default:
                return 0f;
        }

    }

    public void SetSliderValue(SliderType sliderType, float value)
    {
        switch (sliderType)
        {
            case SliderType.Thickness:
                thicknessSlider.value = value;
                break;
            case SliderType.Radius:
                radiusSlider.value = value;
                break;
            case SliderType.Anim:
                animSlider.value = value;
                break;
            default:
                return;
        }
    }

    public void SaveVertexColorToCustomData()
    {
        StylisedLineRenderer.currentEditing.SaveVertexColorsToCustomData();
    }

    public void EraseFunction()
    {
        StylisedLineRenderer.currentEditing.EraseCurrentLine();
    }

    public void FillFunction()
    {
        StylisedLineRenderer.currentEditing.FillCurrentLine();
    }


    public void UpdateUI()
    {
        StylisedLineMenuItem.updateSelection.Invoke();

        if (StylisedLineRenderer.currentSelecting == null || !StylisedLineRenderer.currentSelecting.isAlembic)
        {
            animPanel.SetActive(false);
        } else
        {
            animPanel.SetActive(true);
        }

        if (StylisedLineRenderer.currentSelecting != StylisedLineRenderer.currentEditing)
        {
            StylisedLineRenderer.currentEditing = null;
        }
        
        if (StylisedLineRenderer.currentEditing != null)
        {
            controlPanel.SetActive(true);
            if (editingSphere)
                editingSphere.SetActive(true);
        } else
        {
            controlPanel.SetActive(false);
            if (editingSphere)
                editingSphere.SetActive(false);
        }

    }
    
    private string ActualName(string tempName)
    {
        switch (tempName)
        {
            case "Tie":
                return "Armstrong_Tie";
            case "Head":
                return "Armstrong_Head";
            case "Body_LP":
                return "Armstrong_Body";
            case "Trumpet":
                return "Armstrong_Trumpet";
            case "Hands":
                return "Armstrong_Hands";
            case "Plane":
                return "Gershwin_Body";
            case "Mesh_008":
                return "POL_Cornea";
            case "Mesh_009":
                return "POL_Pupil";
            case "Mesh_010":
                return "POL_Foot";
            case "Mesh_011":
                return "POL_Nails";
            case "Mesh_012":
                return "POL_Body";
            default:
                return tempName;
        }
    }
}
