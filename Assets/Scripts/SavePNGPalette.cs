using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;
using System.Threading.Tasks;

[RequireComponent(typeof(Button))]
public class SavePNGPalette : MonoBehaviour, IPointerDownHandler
{
    const string MENU_TITLE = "Save PNG Color Palette";

    private byte[] _textureBytes;

    void Awake()
    {
        useGUILayout = false;

        var width = 4;
        var height = 4;

        // Create 4x4 red texture to avoid causing errors
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                texture.SetPixel(i, j, Color.red);
            }
        }
        texture.Apply();
        _textureBytes = texture.EncodeToPNG();
        UnityEngine.Object.Destroy(texture);
        return;
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
        DownloadFile(gameObject.name, "OnFileDownload", "palette_" + ImageUtilities.filename+".png", _textureBytes, _textureBytes.Length);
    }

    // Called from browser
    public void OnFileDownload()
    {
        //output.text = "File Successfully Downloaded";
    }
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

        var path = StandaloneFileBrowser.SaveFilePanel(MENU_TITLE, "", "palette_" + ImageUtilities.filename, "png");
        if (string.IsNullOrEmpty(path)) return;

        SaveFile();

        File.WriteAllBytes(path, _textureBytes);
    }
#endif

    public void SaveFile()
    {
        List<ColorPlus> palette = new List<ColorPlus>();

        // only save colors that meet transparency thresh hold
        for (int i = 0; i < ImageUtilities.input_palette.Count; i++)
        {
            if (ImageUtilities.input_palette[i].alpha > ImageUtilities.transparent_threshhold)
                palette.Add(ImageUtilities.input_palette[i]);
        }

        if (ImageUtilities.sort_saved_palette)
        {
            // sort colors according to current criteria
            palette = ImageUtilities.StepSort(palette);
        }

        // input image has a color palette, sort and generate an image for export
        int square_size = ImageUtilities.square_size;
        int width = square_size * palette.Count;
        int height = square_size;
        Color32[] colors = new Color32[width * height];

        // cycle through palette colors
        Parallel.For(0, palette.Count, index =>
        {
            Color32 my_color = new Color32(
                        (byte)palette[index].red,
                        (byte)palette[index].green,
                        (byte)palette[index].blue,
                        (byte)255 // no alpha transparency in RGB24 format
                    );

            int i, j, offset;

            // cycle through height of square
            for (j = 0; j < height; j++)
            {
                // figure out which line and how far to shift right
                offset = (width * j) + (index * square_size);

                // cycle through each pixel in square left to right
                for (i = 0; i != square_size; i++)
                {
                    colors[offset + i] = my_color;
                }
            }
        });

        // create new texture
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

        // set color data
        texture.SetPixels32(colors);
        texture.Apply();

        // encode as PNG
        _textureBytes = texture.EncodeToPNG();

        // remove garbage when finished
        UnityEngine.Object.Destroy(texture);
    }
}