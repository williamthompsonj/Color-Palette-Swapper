using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;

[RequireComponent(typeof(Button))]
public class SavePNGImage : MonoBehaviour, IPointerDownHandler
{
    const string MENU_TITLE = "Sava image as PNG";

    private byte[] _textureBytes;
    public RawImage output_image;

    void Awake()
    {
        useGUILayout = false;

        // Create red texture as place holder so no errors are generated
        var width = 10;
        var height = 10;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                tex.SetPixel(i, j, Color.red);
            }
        }
        tex.Apply();
        _textureBytes = tex.EncodeToPNG();
        UnityEngine.Object.Destroy(tex);
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

    // Broser plugin should be called in OnPointerDown.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (ImageUtilities.input_palette.Count == 0) return;
        SaveFile();
        DownloadFile(gameObject.name, "OnFileDownload", ImageUtilities.filename+".png", _textureBytes, _textureBytes.Length);
    }

    // Called from browser
    public void OnFileDownload() { }
#else
    //
    // Standalone platforms & editor
    //
    public void OnPointerDown(PointerEventData eventData) { }

    // Listen OnClick event in standlone builds
    void Start()
    {
        (GetComponent<Button>()).onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        if (ImageUtilities.input_palette.Count == 0) return;
        var path = StandaloneFileBrowser.SaveFilePanel(MENU_TITLE, "", ImageUtilities.filename, "png");
        if (!string.IsNullOrEmpty(path))
        {
            SaveFile();
            File.WriteAllBytes(path, _textureBytes);
        }
    }
#endif

    public void SaveFile()
    {
        // get the existing altered image from output_image
        _textureBytes = (output_image.texture as Texture2D).EncodeToPNG();
    }
}