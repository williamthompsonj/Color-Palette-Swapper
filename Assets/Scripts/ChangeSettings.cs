using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChangeSettings : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private GameObject settings_menu;
    private Toggle toggle_auto_sort, toggle_auto_match;
    private InputField input_color_groups, input_pixel_size,
        input_transparent, input_gray_detect, input_gray_match, input_filename;

    private Vector3 begin_vec, current_vec, hide_vec;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Rect r = settings_menu.GetComponent<RectTransform>().rect;
        begin_vec = Camera.main.ScreenToWorldPoint(settings_menu.transform.position + ImageUtilities.screen_offset + new Vector3(r.width / 2, -r.height / 2, 0));
    }

    public void OnDrag(PointerEventData eventData)
    {
        settings_menu.transform.position = Camera.main.ScreenToWorldPoint(eventData.position) - begin_vec;
    }

    private void Start()
    {
        useGUILayout = false;

        // store reference to settings menu
        settings_menu = GameObject.Find("SettingsMenu");

        // everything attached to this script will gets setup in this switch
        GameObject.Find("Main_BtnSettings").GetComponent<Button>().onClick.AddListener(ShowSettings);
        settings_menu.transform.Find("Setting_BtnSave").GetComponent<Button>().onClick.AddListener(MenuSave);
        settings_menu.transform.Find("Setting_BtnCancel").GetComponent<Button>().onClick.AddListener(MenuCancel);
        settings_menu.transform.Find("Setting_BtnReset").GetComponent<Button>().onClick.AddListener(MenuReset);

        // store reference to each field's component that we need to set/get
        toggle_auto_sort   = settings_menu.transform.Find("Setting_Toggle_AutoSort").GetComponent<Toggle>();
        toggle_auto_match  = settings_menu.transform.Find("Setting_Toggle_AutoMatch").GetComponent<Toggle>();
        input_color_groups = settings_menu.transform.Find("Setting_Input_QtyColorGroups").GetComponent<InputField>();
        input_pixel_size   = settings_menu.transform.Find("Setting_Input_PNGPixelSize").GetComponent<InputField>();
        input_transparent  = settings_menu.transform.Find("Setting_Input_TransparentThreshold").GetComponent<InputField>();
        input_gray_detect  = settings_menu.transform.Find("Setting_Input_AchromaticDetection").GetComponent<InputField>();
        input_gray_match   = settings_menu.transform.Find("Setting_Input_AchromaticMatch").GetComponent<InputField>();
        input_filename     = settings_menu.transform.Find("Setting_Input_Filename").GetComponent<InputField>();

        input_color_groups.onValueChanged.AddListener(Changed_Colors);
        input_pixel_size.onValueChanged.AddListener(Changed_Pixels);
        input_transparent.onValueChanged.AddListener(Changed_Transparent);
        input_gray_detect.onValueChanged.AddListener(Changed_Gray_Detect);
        input_gray_match.onValueChanged.AddListener(Changed_Gray_Match);
        input_filename.onValueChanged.AddListener(Changed_Filename);

        // initialize current menu position to center of the screen
        current_vec = new Vector3();
        hide_vec = new Vector3(-500, 600, 2);

        // make it really hidden so it doesn't accidentally get shown
        settings_menu.transform.position = hide_vec;
    }

    private void Changed_Colors(string s)
    {
        // set value of text field to safe value
        input_color_groups.text = ImageUtilities.SafeInt(s, 1, 360);
    }
    
    private void Changed_Pixels(string s)
    {
        // set value of text field to safe value
        input_pixel_size.text = ImageUtilities.SafeInt(s, 1, 999);
    }

    private void Changed_Transparent(string s)
    {
        input_transparent.text = ImageUtilities.SafeInt(s, 0, 255);
    }

    private void Changed_Gray_Detect(string s)
    {
        // set value of text field to safe value
        input_gray_detect.text = ImageUtilities.SafeInt(s, 0, 255);
    }

    private void Changed_Gray_Match(string s)
    {
        // set value of text field to safe value
        input_gray_match.text = ImageUtilities.SafeInt(s, 0, 255);
    }

    private void Changed_Filename(string s)
    {
        // https://stackoverflow.com/questions/40564692/c-sharp-regex-to-remove-non-printable-characters-and-control-characters-in-a
        string pattern = @"[\p{C}\\/?.,!@#$*=+<>\{\}~%^&`|:;]*";

        // set value of text field to safe value
        input_filename.text = ImageUtilities.SafeString(s, pattern);
    }

    private void MenuSave()
    {
        // hide settings menu and restore buttons so user can work
        MenuCancel();

        // capture all our newly changed settings
        ImageUtilities.sort_saved_palette = toggle_auto_sort.isOn;
        ImageUtilities.auto_color_match = toggle_auto_match.isOn;
        ImageUtilities.sort_steps = int.Parse(input_color_groups.text);
        ImageUtilities.square_size = int.Parse(input_pixel_size.text);
        ImageUtilities.transparent_threshhold = int.Parse(input_transparent.text);
        ImageUtilities.achromatic_tolerance = int.Parse(input_gray_detect.text);
        ImageUtilities.achromatic_match = int.Parse(input_gray_match.text);

        // detect empty string
        if (input_filename.text.CompareTo(string.Empty) != 0)
            ImageUtilities.filename = input_filename.text;

        if (toggle_auto_match.isOn)
            ImageUtilities.FindClosest();
    }

    private void MenuReset()
    {
        // set menu with current settings
        toggle_auto_sort.isOn = ImageUtilities.sort_saved_palette;
        toggle_auto_match.isOn = ImageUtilities.auto_color_match;
        input_color_groups.text = ImageUtilities.sort_steps.ToString();
        input_pixel_size.text = ImageUtilities.square_size.ToString();
        input_transparent.text = ImageUtilities.transparent_threshhold.ToString();
        input_gray_detect.text = ImageUtilities.achromatic_tolerance.ToString();
        input_gray_match.text = ImageUtilities.achromatic_match.ToString();
        input_filename.text = ImageUtilities.filename;
    }

    private void MenuCancel()
    {
        ImageUtilities.ShowMainButtons();
        current_vec = settings_menu.transform.position;
        settings_menu.transform.position = hide_vec;
    }

    private void ShowSettings()
    {
        // set menu with current settings
        MenuReset();
        ImageUtilities.HideMainButtons();
        settings_menu.transform.position = current_vec;
    }
}