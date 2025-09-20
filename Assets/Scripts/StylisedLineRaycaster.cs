using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class StylisedLineRaycaster : MonoBehaviour
{
    private Camera camera;
    public LayerMask sceneMask;

    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {

                RaycastHit hit;

                Ray ray = camera.ScreenPointToRay(Input.mousePosition);



                if (Physics.Raycast(ray, out hit, Mathf.Infinity, sceneMask))
                {

                    StylisedLineRenderer lineObject = hit.transform.GetComponent<StylisedLineRenderer>();

                    if (lineObject)
                    {
                        StylisedLineRenderer.currentEditing = lineObject;
                        StylisedLineRenderer.currentSelecting = lineObject;
                        
                        lineObject.CheckCurrentEditVert(hit.point);
                    } else
                    {
                        StylisedLineRenderer.currentEditing = null;
                        StylisedLineRenderer.currentSelecting = null;
                        StylisedLineController.Instance.UpdateUI();
                    }
                }
                else
                {
                    StylisedLineRenderer.currentEditing = null;
                    StylisedLineRenderer.currentSelecting = null;
                    StylisedLineController.Instance.UpdateUI();
                }
            }
        }
    }

}
