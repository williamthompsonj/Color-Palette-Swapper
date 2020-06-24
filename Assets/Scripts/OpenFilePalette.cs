using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using SFB;
using System.Diagnostics;
using System;

[RequireComponent(typeof(Button))]
public class OpenFilePalette : MonoBehaviour, IPointerDownHandler {
    public RawImage input_image;
    public RawImage output_image;
    private Texture2D output_palette;

#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    public void OnPointerDown(PointerEventData eventData) {
        UploadFile(gameObject.name, "OnFileUpload", "", false);
    }

    // Called from browser
    public void OnFileUpload(string url) {
        StartCoroutine(OutputRoutine(url));
    }
#else
    //
    // Standalone platforms & editor
    //
    public void OnPointerDown(PointerEventData eventData) { }

    void Start() {
        var button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Open Palette Image", "", "", false);
        if (paths.Length < 1) return;

        string thePath = new System.Uri(paths[0]).AbsoluteUri;
        StartCoroutine(OutputRoutine(thePath));
    }
#endif

    private IEnumerator OutputRoutine(string url)
    {
        var loader = UnityWebRequestTexture.GetTexture(url);
        yield return loader.SendWebRequest();
        output_palette = ((DownloadHandlerTexture)loader.downloadHandler).texture;

        List<ColorPlus> color_list = new List<ColorPlus>();
        Color32[] cols = output_palette.GetPixels32(0);
        ColorPlus color_ref = new ColorPlus();
        bool exists;
        int i, j;

        // cycle through all the colors
        for (i = 0; i < cols.Length; ++i)
        {
            if (cols[i].a < 246)
            {
                continue;
            }

            exists = false;

            for (j = 0; j < color_list.Count; j++)
            {
                if (cols[i].r == color_list[j].red_int &&
                   cols[i].g == color_list[j].green_int &&
                   cols[i].b == color_list[j].blue_int)
                {
                    exists = true;
                    color_ref = color_list[j];
                    break;
                }
            }

            if (!exists)
            {
                color_ref = new ColorPlus(cols[i]);
                color_list.Add(color_ref);
            }
        }

        // save output palette list
        ImageUtilities.output_palette = color_list;

        // reference to input_image and output_image in easy place
        ImageUtilities.input_image = input_image;
        ImageUtilities.output_image = output_image;

        // auto-magically find nearest color neighbor using CIE2000 color distance algorithm
        ImageUtilities.FindClosest();

        // try to do recolor work from input to output
        ImageUtilities.SetOutputImage();
    }
}