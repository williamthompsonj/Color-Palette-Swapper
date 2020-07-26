using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InfoScreen : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private GameObject info_panel;

    private Vector3 begin_vec, current_vec, hide_vec;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Rect r = info_panel.GetComponent<RectTransform>().rect;
        begin_vec = Camera.main.ScreenToWorldPoint(info_panel.transform.position + ImageUtilities.screen_offset + new Vector3(r.width / 2, -r.height / 2, 0));
    }

    public void OnDrag(PointerEventData eventData)
    {
        info_panel.transform.position = Camera.main.ScreenToWorldPoint(eventData.position) - begin_vec;
    }

    private void Start()
    {
        useGUILayout = false;

        // everything attached to this script gets setup here
        info_panel = GameObject.Find("InfoPanel");
        GameObject.Find("Main_BtnInfo").GetComponent<Button>().onClick.AddListener(OpenInfo);
        GameObject.Find("Info_BtnOkay").GetComponent<Button>().onClick.AddListener(CloseInfo);

        // initialize current menu position to center of the screen
        current_vec = new Vector3();
        hide_vec = new Vector3(0, 500, 2);

        info_panel.transform.position = hide_vec;
    }

    private void OpenInfo()
    {
        ImageUtilities.HideMainButtons();
        info_panel.transform.position = current_vec;
    }

    private void CloseInfo()
    {
        ImageUtilities.ShowMainButtons();
        //current_vec = info_panel.transform.position;
        info_panel.transform.position = hide_vec;
    }
}