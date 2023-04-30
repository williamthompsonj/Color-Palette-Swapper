using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WaitScreen : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private GameObject wait_panel;

    private Vector3 begin_vec, current_vec, hide_vec;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Rect r = wait_panel.GetComponent<RectTransform>().rect;
        begin_vec = Camera.main.ScreenToWorldPoint(wait_panel.transform.position + ImageUtilities.screen_offset + new Vector3(r.width / 2, -r.height / 2, 0));
    }

    public void OnDrag(PointerEventData eventData)
    {
        wait_panel.transform.position = Camera.main.ScreenToWorldPoint(eventData.position) - begin_vec;
    }

    private void Start()
    {
        useGUILayout = false;

        // everything attached to this script gets setup here
        wait_panel = GameObject.Find("WaitPanel");

        // initialize current menu position to center of the screen
        current_vec = new Vector3();
        hide_vec = new Vector3(800, 800, 2);

        wait_panel.transform.position = hide_vec;
    }

    public void OpenPanel()
    {
        ImageUtilities.HideMainButtons();
        wait_panel.transform.position = current_vec;
    }

    public void ClosePanel()
    {
        ImageUtilities.ShowMainButtons();
        wait_panel.transform.position = hide_vec;
    }
}