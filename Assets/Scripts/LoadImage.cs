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
public class LoadImage : MonoBehaviour, IPointerDownHandler
{
    const string MENU_TITLE = "Open Image To Color Swap";

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
    const string WEBGL_EXTENSIONS = ".bmp, .png, .jpg, .jpeg, .jpe, .jif, .jfif, .jfi";

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    public void OnPointerDown(PointerEventData eventData)
    {
        UploadFile(gameObject.name, "OnFileUpload", WEBGL_EXTENSIONS, false);
    }

    // Called from browser
    public void OnFileUpload(string url)
    {
        // check if file name selected
        if (url.Length < 1) return;
        
        // process file
        //StartCoroutine(OutputRoutine(new System.Uri(paths[0]).AbsoluteUri));
        AsyncWait(url);
    }
#else
    //
    // Standalone platforms & editor
    //
    public void OnPointerDown(PointerEventData eventData) { }

    private SFB.ExtensionFilter[] extensions = new[]
    {
        new SFB.ExtensionFilter("Image Files", "bmp", "png", "jpg", "jpeg", "jpe", "jif", "jfif", "jfi"),
        new SFB.ExtensionFilter("All Files", "*"),
    };

    void Start()
    {
        setupFuncs();
        
        // add click listener
        (GetComponent<Button>()).onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Int64 time_start = PerfMon.Ticks();

        // show user a file select window
        var paths = StandaloneFileBrowser.OpenFilePanel(MENU_TITLE, "", extensions, false);

        // check if file name selected
        if (paths.Length < 1) return;

        // process file
        //StartCoroutine(OutputRoutine(new System.Uri(paths[0]).AbsoluteUri));
        AsyncWait(new System.Uri(paths[0]).AbsoluteUri);

        perf("OnClick", time_start);
    }
#endif

    private async void AsyncWait(string url)
    {
        Int64 time_start = PerfMon.Ticks();

        // Working... Please wait...
        WaitScreen.SetText();
        WaitScreen.OpenPanel();

        await AsyncRoutine(url);

        // done working
        WaitScreen.ClosePanel();

        perf("AsyncWait", time_start);
    }

    private async Task<string> AsyncRoutine(string url)
    {
        Int64 time_start = PerfMon.Ticks();

        // ensure URL is valid
        url = url.Replace('\\', '/');

        // should be PNG or JPEG
        using (UnityWebRequest loader = UnityWebRequestTexture.GetTexture(url))
        {
            await loader.SendWebRequest();

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

        perf("AsyncRoutine", time_start);

        return null;
    }

    private IEnumerator OutputRoutine(string url)
    {
        Int64 time_start = PerfMon.Ticks();

        // ensure URL is valid
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

        perf("OutputRoutine", time_start);
    }

    private static void perf(string func, Int64 runtime)
    {
        PerfMon.Call("LoadImage", func, runtime);
    }

    private static void setupFuncs()
    {
        PerfMon.SetupFunc("LoadImage", "OnClick");
        PerfMon.SetupFunc("LoadImage", "AsyncWait");
        PerfMon.SetupFunc("LoadImage", "AsyncRoutine");
        PerfMon.SetupFunc("LoadImage", "OutputRoutine");
    }
}