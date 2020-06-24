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
public class OpenFileImage : MonoBehaviour, IPointerDownHandler {
    public RawImage input_image;
    public RawImage output_image;
    public Text output_text;

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
        var paths = StandaloneFileBrowser.OpenFilePanel("Title", "", "", false);
        if (paths.Length < 1) return;

        string thePath = new System.Uri(paths[0]).AbsoluteUri;
        StartCoroutine(OutputRoutine(thePath));
        ImageUtilities.filename = thePath.Substring(thePath.LastIndexOf('/')+1);

        // check if file ending has an extension and remove it
        int i = ImageUtilities.filename.LastIndexOf('.');
        if(i > 0) ImageUtilities.filename = ImageUtilities.filename.Substring(0, i);
    }
#endif

    private IEnumerator OutputRoutine(string url)
    {
        var loader = UnityWebRequestTexture.GetTexture(url);
        yield return loader.SendWebRequest();
        input_image.texture = ((DownloadHandlerTexture)loader.downloadHandler).texture;

        // duplicate input texture as Texture2D
        Texture2D texture = Instantiate(input_image.mainTexture) as Texture2D;

        bool exists;
        List<ColorPlus> color_list = new List<ColorPlus>();

        // get all the colors used in this image
        Color32[] cols = texture.GetPixels32(0);
        ColorPlus color_ref = new ColorPlus();

        // cycle through all the colors
        for (int i = 0; i < cols.Length; ++i)
        {
            if (cols[i].a <= ImageUtilities.transparent_threshhold)
            {
                continue;
            }

            exists = false;

            for (int j = 0; j < color_list.Count; j++)
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

        // save palette data in easily accessible place
        ImageUtilities.input_palette = color_list;

        // reference to input_image and output_image in easy place
        ImageUtilities.input_image = input_image;
        ImageUtilities.output_image = output_image;

        // auto-magically find nearest color neighbor using CIE2000 color distance algorithm
        ImageUtilities.FindClosest();

        // try to do recolor work from input to output
        ImageUtilities.SetOutputImage();

        // get image size
        Vector2 img_size = input_image.rectTransform.sizeDelta;
        float wide = input_image.texture.width;
        float high = input_image.texture.height;

        // set display based on image width (height can fill toward bottom without problem)
        float delta = input_image.rectTransform.rect.width / wide;

        // fix aspect ratio to retain original proportions
        input_image.rectTransform.sizeDelta = new Vector2(wide * delta, high * delta);
        output_image.rectTransform.sizeDelta = new Vector2(wide * delta, high * delta);
    }
}