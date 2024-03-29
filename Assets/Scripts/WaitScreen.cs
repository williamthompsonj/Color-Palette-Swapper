﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WaitScreen : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private static GameObject wait_panel;
    private static UnityEngine.UI.Text wait_text;

    private static Vector3 begin_vec, current_vec, hide_vec;

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
        wait_text = GameObject.Find("WaitText").GetComponent<UnityEngine.UI.Text>();

        // initialize current menu position to center of the screen
        current_vec = new Vector3();
        hide_vec = new Vector3(800, 800, 2);

        wait_panel.transform.position = hide_vec;
    }

    public static void SetText(string text = "Working... Please wait...", int fontSize = 34)
    {
        wait_text.text = text;
        wait_text.fontSize = fontSize;
    }

    public static void AddLine(string text)
    {
        wait_text.text = GetText() + "\r\n" + text;
    }

    public static string GetText()
    {
        return wait_text.text;
    }

    public static void OpenPanel()
    {
        wait_panel.transform.position = current_vec;
        ImageUtilities.HideMainButtons();
    }

    public static void ClosePanel()
    {
        ImageUtilities.ShowMainButtons();
        wait_panel.transform.position = hide_vec;
    }
}