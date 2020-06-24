using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;
using System;

[RequireComponent(typeof(Button))]
public class SavePaletteImage : MonoBehaviour, IPointerDownHandler
{
    private byte[] _textureBytes;
    private Texture2D input_palette;

    void Awake()
    {
        var width = 4;
        var height = 4;

        // Create 4x4 red texture
        input_palette = new Texture2D(width, height, TextureFormat.RGB24, false);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                input_palette.SetPixel(i, j, Color.red);
            }
        }
        input_palette.Apply();
        _textureBytes = input_palette.EncodeToPNG();
        return;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

    // Broser plugin should be called in OnPointerDown.
    public void OnPointerDown(PointerEventData eventData) {
        DownloadFile(gameObject.name, "OnFileDownload", "palette_" + ImageUtilities.filename, _textureBytes, _textureBytes.Length);
    }

    // Called from browser
    public void OnFileDownload() {
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
        var button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        var path = StandaloneFileBrowser.SaveFilePanel("Save Color Palette", "", "palette_" + ImageUtilities.filename, "png");
        if (string.IsNullOrEmpty(path)) return;

        // sort colors according to current criteria
        List<ColorPlus> palette = ImageUtilities.StepSort(ImageUtilities.input_palette);

        // input image has a color palette, sort and generate an image for export
        int square_size = ImageUtilities.square_size;
        int width = square_size * palette.Count;
        int height = square_size;
        input_palette = new Texture2D(width, height, TextureFormat.RGB24, false);
        Color my_color = new Color(
                    palette[0].red,
                    palette[0].green,
                    palette[0].blue,
                    1 // alpha
                );

        int i, j, input_color_index;

        // cycle through width of palette image
        for (i = 0; i < width; i++)
        {
            // check if we need the next color
            if(i % square_size == 0)
            {
                // current palette index
                input_color_index = i / square_size;

                my_color = new Color(
                    palette[input_color_index].red,
                    palette[input_color_index].green,
                    palette[input_color_index].blue,
                    1 // alpha
                );
            }

            // cycle through height of square
            for (j = 0; j < height; j++)
            {
                input_palette.SetPixel(i, j, my_color);
            }
        }
        input_palette.Apply();
        _textureBytes = input_palette.EncodeToPNG();
        File.WriteAllBytes(path, _textureBytes);
    }
#endif
}