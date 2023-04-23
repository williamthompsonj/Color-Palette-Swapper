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
using System.Text;

[RequireComponent(typeof(Button))]
public class LoadImage : MonoBehaviour, IPointerDownHandler
{
    const string MENU_TITLE = "Open Image To Color Swap";
    const string WEBGL_EXTENSIONS = ".bmp, .png, .jpg, .jpeg, .jpe, .jif, .jfif, .jfi";
    const string OTHER_EXTENSIONS = "bmp,png,jpg,jpeg,jpe,jif,jfif,jfi";

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
                input_image.texture = bmp_img.ToTexture2D();
            }
            else
            {
                // assign directly to input texture
                input_image.texture = ((DownloadHandlerTexture)loader.downloadHandler).texture as Texture2D;
            }
        }

        // store filename without extension for later use
        ImageUtilities.filename = Path.GetFileNameWithoutExtension(url);

        // fix html encoded spaces
        if (ImageUtilities.filename.IndexOf("%20") > -1)
        {
            ImageUtilities.filename = ImageUtilities.filename.Replace("%20", " ");
        }

        // tell unity to load palette data on next frame update
        //byte[] b = ((DownloadHandlerTexture)loader.downloadHandler).GetData();
        //ImageUtilities.input_palette = ImageUtilities.LoadImagePalette(((DownloadHandlerTexture)loader.downloadHandler).texture.GetRawTextureData<Color32>(), true);
        ImageUtilities.output_palette.Clear();
        ImageUtilities.input_palette = ImageUtilities.LoadImagePalette((input_image.mainTexture as Texture2D).GetPixels32(0));

        // try to do recolor work from input to output
        ImageUtilities.SetOutputImage();

        // get image size
        float wide = input_image.texture.width;
        float high = input_image.texture.height;

        // fix aspect ratio to retain original proportions
        input_image.rectTransform.sizeDelta = new Vector2(wide, high);
        output_image.rectTransform.sizeDelta = new Vector2(wide, high);

        // set preview images to origin
        input_image.rectTransform.localPosition = new Vector3(0, 0, 1);
        output_image.rectTransform.localPosition = new Vector3(0, 0, 1);

        // set Texture.filterMode to point to stop the weird filtered look
        input_image.texture.filterMode = FilterMode.Point;
        output_image.texture.filterMode = FilterMode.Point;

        // set zoom level to default
        ImageUtilities.ZoomReset();
    }

    void log(object a)
    {
        UnityEngine.Debug.Log(a.ToString());
    }
}