using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using SFB;
using B83.Image.BMP;

[RequireComponent(typeof(Button))]
public class LoadPNGPalette : MonoBehaviour, IPointerDownHandler
{
    const string MENU_TITLE = "Open Image Palette";
    const string WEBGL_EXTENSIONS = ".bmp, .png, .jpg, .jpeg, .jpe, .jif, .jfif, .jfi";
    const string OTHER_EXTENSIONS = "bmp,png,jpg,jpeg,jpe,jif,jfif,jfi";

    // send debug data to screen
    public GameObject debug_panel;

    public RawImage input_image;
    public RawImage output_image;

    public void Awake()
    {
        useGUILayout = false;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    public void OnPointerDown(PointerEventData eventData)
    {
        UploadFile(gameObject.name, "OnFileUpload", WEBGL_EXTENSIONS, false);
    }

    // Called from browser
    public void OnFileUpload(string url)
    {
        StartCoroutine(OutputRoutine(url));
    }
#else
    //
    // Standalone platforms & editor
    //
    public void OnPointerDown(PointerEventData eventData) { }

    void Start()
    {
        // add click listener
        (GetComponent<Button>()).onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        // show user a file select window
        var paths = StandaloneFileBrowser.OpenFilePanel(MENU_TITLE, "", OTHER_EXTENSIONS, false);

        // check if file name selected
        if (paths.Length < 1) return;

        // begin processing chosen file
        StartCoroutine(OutputRoutine(new System.Uri(paths[0]).AbsoluteUri));
    }
#endif

    private IEnumerator OutputRoutine(string url)
    {
        url = url.Replace('\\', '/');

        // should be PNG or JPEG
        using (UnityWebRequest loader = UnityWebRequestTexture.GetTexture(url))
        {
            yield return loader.SendWebRequest();

            byte[] ba = ((DownloadHandlerTexture)loader.downloadHandler).data;
            char[] data = System.Text.Encoding.UTF8.GetChars(ba);

            if (data[0] == 'B' && data[1] == 'M')
            {
                // BMP file detected

                BMPLoader bmp_loader = new BMPLoader();

                // can be uncomment to read alpha (sometimes breaks)
                //bmp_loader.ForceAlphaReadWhenPossible = true;

                //load the image data
                BMPImage bmp_img = bmp_loader.LoadBMP(ba);

                // Convert the Color32 array into a Texture2D
                ImageUtilities.output_palette.Clear();
                ImageUtilities.output_palette.AddRange(ImageUtilities.LoadImagePalette(bmp_img.imageData));
            }
            else
            {
                // save output palette list
                ImageUtilities.output_palette.Clear();
                ImageUtilities.output_palette.AddRange(ImageUtilities.LoadImagePalette(((DownloadHandlerTexture)loader.downloadHandler).texture.GetPixels32(0)));
            }
        }

        // setup debug window info
        //var debug_text = debug_panel.transform.Find("DebugText").GetComponent<Text>();
        //debug_text.text = url;

        //ImageUtilities.HideMainButtons();
        //debug_panel.transform.position = new Vector3();

        // auto-magically find nearest color neighbor using CIE2000 color distance algorithm
        ImageUtilities.FindClosest();

        // try to do recolor work from input to output
        ImageUtilities.SetOutputImage();
    }
}