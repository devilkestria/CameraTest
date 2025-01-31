using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    private bool camAvailable;
    private WebCamTexture backCamera;
    private Texture defaultBackground;
    public RawImage background, result;
    public AspectRatioFitter fitter;
    private float scaleY;
    private int orient;

    private Color transparent = new Color(1, 1, 1, 0f);

    //UI
    public GameObject buttons, resultBtn, resultPanel;

    private void Start()
    {
        using (AndroidJavaClass ajc = new AndroidJavaClass("com.yasirkula.unity.NativeGalleryMediaPickerFragment")) ajc.SetStatic<bool>("preferGetContent", true);
#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera)) Permission.RequestUserPermission(Permission.Camera);
#endif
        defaultBackground = background.texture;
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.Log("No Camera Detected");
            camAvailable = false;
            return;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing) backCamera = new WebCamTexture(devices[i].name, Screen.width, Screen.height);
        }

        if (!backCamera) return;
        backCamera.Play();
        background.texture = backCamera;
        camAvailable = true;
        result.color = transparent;
        resultBtn.SetActive(false);
    }

    private void Update()
    {
        if (!camAvailable) return;
        float ratio = (float)backCamera.width / (float)backCamera.height;
        fitter.aspectRatio = ratio;

        scaleY = backCamera.videoVerticallyMirrored ? -1f : 1f;
        background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

        orient = -backCamera.videoRotationAngle;
        background.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
    }

    public void TakePicture()
    {
        if (!camAvailable) return;
        StartCoroutine(TakeScreenshotAndSave());
        IEnumerator TakeScreenshotAndSave()
        {
            buttons.SetActive(false);
            yield return new WaitForEndOfFrame();

            Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            ss.Apply();

            // Save the screenshot to Gallery/Photos
            NativeGallery.Permission permission = NativeGallery.SaveImageToGallery(ss, "GalleryTest", "Image.JPEG", (success, path) => Debug.Log("Media save result: " + success + " " + path));

            Debug.Log("Permission result: " + permission);

            // To avoid memory leaks
            Destroy(ss);
            buttons.SetActive(true);
            resultBtn.SetActive(true);
        }
    }
    public void ShowPicture()
    {
        if (NativeGallery.IsMediaPickerBusy()) return;
        PickImage(512);
    }
    private void PickImage(int maxSize)
    {
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
       {
           Debug.Log("Image path: " + path);
           if (path != null)
           {
               ToggleResultPanel(true);
               Texture2D tex = null;
               byte[] filedata = File.ReadAllBytes(path);
               tex = new Texture2D(2, 2);
               tex.LoadImage(filedata);
               result.texture = tex;
               
               /*
               // Create Texture from selected image
               Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize);
               if (texture == null)
               {
                   Debug.Log("Couldn't load texture from " + path);
                   return;
               }
               texture.Apply();
               result.texture = texture;
               */
           }
       });

        Debug.Log("Permission result: " + permission);
    }
    public void CloseResultPanel() => ToggleResultPanel(false);
    public void ToggleResultPanel(bool value) => resultPanel.SetActive(value);
}