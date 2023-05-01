using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using SFB;
using System.Text.RegularExpressions;

[RequireComponent(typeof(Button))]
public class LoadTextPalette : MonoBehaviour, IPointerDownHandler
{
    const string WEBGL_EXTENSIONS = ".txt, .hex, .gpl";
    const string MENU_TITLE = "Load Text Color Palette (HEX Mapping / GIMP GPL)";

    private SFB.ExtensionFilter[] extensions;

    private HashSet<ColorPlus> color_list = new HashSet<ColorPlus>();
    private ColorPlus transparent = new ColorPlus();
    private bool find_closest;

    public void Awake()
    {
        useGUILayout = false;
        extensions = new[]
        {
            new SFB.ExtensionFilter("Text Files", "txt", "hex", "gpl"),
            new SFB.ExtensionFilter("All Files", "*"),
        };
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

    public void Start()
    {
        setupFuncs();

        (GetComponent<Button>()).onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Int64 time_start = PerfMon.Ticks();

        var paths = StandaloneFileBrowser.OpenFilePanel(MENU_TITLE, "", extensions, false);

        // check if file name selected
        if (paths.Length < 1)
            return;

        // process file
        //StartCoroutine(OutputRoutine(new System.Uri(paths[0]).AbsoluteUri));
        AsyncWait(new System.Uri(paths[0]).AbsoluteUri);

        perf("OnClick", time_start);
    }
#endif

    async void AsyncWait(string url)
    {
        Int64 time_start = PerfMon.Ticks();

        // Working... Please Wait
        WaitScreen.OpenPanel();

        await AsyncRoutine(url);

        // done working
        WaitScreen.ClosePanel();

        perf("AsyncWait", time_start);
    }

    async Task<string> AsyncRoutine(string url)
    {
        Int64 time_start = PerfMon.Ticks();

        // ensure URL is valid
        url = url.Replace('\\', '/');

        string text = "";

        // should be PNG or JPEG
        using (UnityWebRequest loader = UnityWebRequest.Get(url))
        {
            await loader.SendWebRequest();
            text = loader.downloadHandler.text;
        }

        // make lowercase
        text = text.ToLower().Trim();

        // ensure text file has content
        if (text.Length < 1)
        {
            return null;
        }

        // replace all whitespace with single space
        text = (Regex.Replace(text, "[ \f\r\t\v]+", " "));

        // remove whitespace at beginning and end of lines
        text = text.Replace(" \n", "\n");
        text = text.Replace("\n ", "\n");

        // sample string to check
        string test_str = text.Substring(0, 12);

        // default state for finding nearst color match
        find_closest = false;

        // clear our color list
        color_list.Clear();

        // check what kind of text file this is
        if (string.Compare(test_str, "gimp palette") == 0)
        {
            // this is a GPL palette, process with GPL function
            ProcessGPL(text);
            find_closest = true;
        }
        else
        {
            // this is a Hex palette, process with hex function
            ProcessHex(text);
        }

        // save output palette list
        ImageUtilities.output_palette.Clear();
        ImageUtilities.output_palette.AddRange(color_list);

        // figure out if we want to find closest match
        if (find_closest)
            ImageUtilities.FindClosest();

        // show results of recolor work
        ImageUtilities.SetOutputImage();

        perf("AsyncRoutine", time_start);

        return null;
    }

    private IEnumerator OutputRoutine(string url)
    {
        Int64 time_start = PerfMon.Ticks();

        // ensure URL is valid
        url = url.Replace('\\', '/');

        var loader = UnityWebRequest.Get(url);
        yield return loader.SendWebRequest();
        string text = loader.downloadHandler.text;

        // dispose of the loader in parallel thread since it can slow down overall time
        //Task task = new Task(() => { loader.Dispose(); });
        //task.Start();
        loader.Dispose();

        // make lowercase
        text = text.ToLower().Trim();

        // ensure text file has content
        if (text.Length < 1)
            yield break;

        // replace all whitespace with single space
        text = (Regex.Replace(text, "[ \f\r\t\v]+", " "));

        // remove whitespace at beginning and end of lines
        text = text.Replace(" \n", "\n");
        text = text.Replace("\n ", "\n");

        // sample string to check
        string test_str = text.Substring(0, 12);

        // default state for finding nearst color match
        find_closest = false;

        // clear our color list
        color_list.Clear();

        // check what kind of text file this is
        if (string.Compare(test_str, "gimp palette") == 0)
        {
            // this is a GPL palette, process with GPL function
            ProcessGPL(text);
            find_closest = true;
        }
        else
        {
            // this is a Hex palette, process with hex function
            ProcessHex(text);
        }

        // save output palette list
        ImageUtilities.output_palette.Clear();
        ImageUtilities.output_palette.AddRange(color_list);

        // figure out if we want to find closest match
        if(find_closest) ImageUtilities.FindClosest();

        // show results of recolor work
        ImageUtilities.SetOutputImage();

        perf("OutputRoutine", time_start);
    }

    private void ProcessGPL(string text)
    {
        Int64 time_start = PerfMon.Ticks();

        // https://gitlab.gnome.org/GNOME/gimp/-/blob/gimp-2-10/app/core/gimppalette-load.c#L39
        text = text.Replace("gimp palette\n", "");
        text = (Regex.Replace(text, "name:[.]*$", ""));
        text = (Regex.Replace(text, "columns:[.]*$", ""));
        text = (Regex.Replace(text, "#[.]*$", ""));

        // strip all characters except 0-9, space, and newline characters
        text = (Regex.Replace(text, "[^ 0-9\n]", "")).Trim();

        // replace multiple lines with single line
        text = (Regex.Replace(text, "[\n]+", "\n"));

        // break into rows and rgb values
        string[] line, rows = text.Split('\n');

        for (int i = 0; i < rows.Length; i++)
        {
            // remove leading spaces to parse easier
            rows[i] = rows[i].Trim();
            line = Regex.Split(rows[i], "[ ]+");
            if (line.Length > 2)
            {
                // use implicit conversion of int -> ColorPlus
                color_list.Add(int.Parse(line[0]) << 24 | int.Parse(line[1]) << 16 | int.Parse(line[2]) << 8 | 255);
            }
        }

        perf("ProcessGPL", time_start);
    }

    private void ProcessHex(string text)
    {
        Int64 time_start = PerfMon.Ticks();

        // remove comments
        text = (Regex.Replace(text, "//[.]*$", ""));

        // strip all characters except 0-9, a-f, greater than symbol, and newline
        text = (Regex.Replace(text, "[^0-9a-f>\n]", "")).Trim();

        // replace multiple lines with single line
        text = (Regex.Replace(text, "[\n]+", "\n"));

        // remove example line at the end
        text = text.Replace("\nc0ffee00>c00c000", "");

        // break into rows
        string[] line, rows = text.Split('\n');

        // these are assigned empty values to make the compiler stop complaining
        ColorPlus in_color = transparent, out_color = transparent, test_color = transparent;
        bool is_color_map;
        int i, j;

        for(i = 0; i < rows.Length; i++)
        {
            is_color_map = false;
            // split line into values
            line = rows[i].Split('>');

            // left side of arrow
            if (line[0].Length >= 8)
            {
                in_color = int.Parse(line[0].Substring(0, 8), System.Globalization.NumberStyles.HexNumber);
            }
            else if (line[0].Length >= 6)
            {
                in_color = int.Parse(line[0].Substring(0, 6), System.Globalization.NumberStyles.HexNumber) << 8 | 255;
            }
            else
                continue;

            // right side of arrow
            if (line.Length > 1)
            {
                is_color_map = true;
                if (line[1].Length >= 8)
                {
                    out_color = int.Parse(line[1].Substring(0, 8), System.Globalization.NumberStyles.HexNumber);
                }
                else if (line[1].Length >= 6)
                {
                    out_color = int.Parse(line[1].Substring(0, 6), System.Globalization.NumberStyles.HexNumber) << 8 | 255;
                }
                else
                    continue;
            }

            // is this a color mapping ?
            if (is_color_map)
            {
                j = ImageUtilities.input_palette.IndexOf(in_color);
                if (j != -1)
                {
                    // found the color, map it
                    test_color = ImageUtilities.input_palette[j];
                    test_color.match = out_color.color32;
                    ImageUtilities.input_palette[j] = test_color;
                }
                // add output color to the list
                color_list.Add(out_color);
            }
            else
            {
                // only adding this color, no mapping
                find_closest = true;
                color_list.Add(in_color);
            }
        }

        perf("ProcessHex", time_start);
    }

    private static void perf(string func, Int64 runtime)
    {
        PerfMon.Call("LoadTextPalette", func, runtime);
    }

    private static void setupFuncs()
    {
        PerfMon.SetupFunc("LoadTextPalette", "OnClick");
        PerfMon.SetupFunc("LoadTextPalette", "AsyncWait");
        PerfMon.SetupFunc("LoadTextPalette", "AsyncRoutine");
        PerfMon.SetupFunc("LoadTextPalette", "OutputRoutine");
        PerfMon.SetupFunc("LoadTextPalette", "ProcessGPL");
        PerfMon.SetupFunc("LoadTextPalette", "ProcessHex");
    }
}