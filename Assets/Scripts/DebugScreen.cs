using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DebugScreen : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private GameObject debug_panel;

    private Vector3 begin_vec, current_vec, hide_vec;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Rect r = debug_panel.GetComponent<RectTransform>().rect;
        begin_vec = Camera.main.ScreenToWorldPoint(debug_panel.transform.position + ImageUtilities.screen_offset + new Vector3(r.width / 2, -r.height / 2, 0));
    }

    public void OnDrag(PointerEventData eventData)
    {
        debug_panel.transform.position = Camera.main.ScreenToWorldPoint(eventData.position) - begin_vec;
    }

    private void Start()
    {
        useGUILayout = false;

        // everything attached to this script gets setup here
        debug_panel = GameObject.Find("DebugPanel");
        GameObject.Find("Debug_BtnOkay").GetComponent<Button>().onClick.AddListener(CloseDebug);

        // initialize current menu position to center of the screen
        current_vec = new Vector3();
        hide_vec = new Vector3(800, 500, 2);

        debug_panel.transform.position = hide_vec;
    }

    private void OpenDebug()
    {
        ImageUtilities.HideMainButtons();
        debug_panel.transform.position = current_vec;
    }

    private void CloseDebug()
    {
        ImageUtilities.ShowMainButtons();
        //current_vec = info_panel.transform.position;
        debug_panel.transform.position = hide_vec;
    }
}