using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class StylisedLineMenuItem : MonoBehaviour
{
    public StylisedLineRenderer linkedLineObject;

    private Toggle toggle;
    private Text label;

    public static UnityEvent updateSelection = new UnityEvent();

    public void OnEnable()
    {
        if(!label)
            label = GetComponentInChildren<Text>();

        if (!toggle)
            toggle = GetComponent<Toggle>();

        updateSelection.AddListener(UpdateSelection);
    }

    public void ChangeName(string name)
    {
        if (label)
            label.text = name;
        else
        {
            label = GetComponentInChildren<Text>();
            label.text = name;
        }
    }

    private void UpdateSelection()
    {
        if(StylisedLineRenderer.currentSelecting != linkedLineObject)
        {
            toggle.isOn = false;
        } else
        {
            toggle.isOn = true;
        }
    }

    public void OnToggleChange()
    {
        if (toggle.isOn)
        {
            StylisedLineRenderer.currentSelecting = linkedLineObject;
        } else if(StylisedLineRenderer.currentSelecting == linkedLineObject)
        {
            StylisedLineRenderer.currentSelecting = null;
        }
        StylisedLineController.Instance.UpdateUI();
    }


    private void OnDisable()
    {
        updateSelection.RemoveListener(UpdateSelection);
    }
}
