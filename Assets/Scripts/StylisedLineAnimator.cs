using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class StylisedLineAnimator : MonoBehaviour
{

    private Slider slider;
    public Text timeMonitor;
    private StylisedLineRenderer currentSelecting;

    private void OnEnable()
    {
        if (!slider)
            slider = GetComponentInChildren<Slider>();
    }
    // Update is called once per frame
    void Update()
    {
        currentSelecting = StylisedLineRenderer.currentSelecting;
        if(currentSelecting != null && currentSelecting.isAlembic)
        {
            timeMonitor.text = currentSelecting.currentAnimFrameTime.ToString();
            if (!currentSelecting.paused)
            {
                slider.value = currentSelecting.GetAnimationInfo();
            }
        } else
        {
            currentSelecting = null;
        }
    }

    public void StartAnimation()
    {
        if(currentSelecting)
            currentSelecting.PlayAnimation();
    }

    public void PauseAnimation()
    {
        if(currentSelecting)
            currentSelecting.PauseAnimation();
    }

    public void SetAnimationFrame()
    {
        if (currentSelecting.paused)
        {
            currentSelecting.SetAnimationInfo(slider.value);
            StylisedLineRenderer.currentEditing = null;
            StylisedLineController.Instance.UpdateUI();
        }
    }
}
