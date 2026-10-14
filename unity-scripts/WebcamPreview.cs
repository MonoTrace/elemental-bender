using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class WebcamPreview : MonoBehaviour {
    public string frameUrl = "http://127.0.0.1:5056/frame.jpg";
    public bool PreviewVisible { get; private set; } = true;
    public bool HasFrame { get; private set; }
    public int ReceivedFrames { get; private set; }
    GameObject panel;
    UnityEngine.UI.RawImage image;
    TMP_Text caption, buttonLabel;
    Texture2D texture;

    public void Build(Transform canvas) {
        var toggle=BattlePresentation.Panel(canvas,"WebcamToggle",new Vector2(1,1),new Vector2(-24,-20),new Vector2(210,42),new Color(.16f,.24f,.35f));
        var button=toggle.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic=toggle;
        button.onClick.AddListener(TogglePreview);
        buttonLabel=BattlePresentation.Label(toggle.transform,"Label",Vector2.zero,Vector2.zero,new Vector2(210,42),20);
        buttonLabel.rectTransform.anchorMin=Vector2.zero;buttonLabel.rectTransform.anchorMax=Vector2.one;
        buttonLabel.rectTransform.offsetMin=Vector2.zero;buttonLabel.rectTransform.offsetMax=Vector2.zero;
        buttonLabel.alignment=TextAlignmentOptions.Center; buttonLabel.text="WEBCAM: ON";
        var background=BattlePresentation.Panel(canvas,"WebcamPanel",new Vector2(1,0),new Vector2(-24,188),new Vector2(256,216),new Color(.04f,.07f,.11f));
        panel=background.gameObject;
        var viewport=new GameObject("FrameViewport",typeof(RectTransform));viewport.transform.SetParent(panel.transform,false);
        var bounds=viewport.GetComponent<RectTransform>();bounds.anchorMin=Vector2.zero;bounds.anchorMax=Vector2.one;
        bounds.offsetMin=new Vector2(8,32);bounds.offsetMax=new Vector2(-8,-8);
        var picture=new GameObject("WebcamImage",typeof(RectTransform),typeof(UnityEngine.UI.RawImage),typeof(UnityEngine.UI.AspectRatioFitter));
        picture.transform.SetParent(viewport.transform,false);
        image=picture.GetComponent<UnityEngine.UI.RawImage>();image.raycastTarget=false; image.enabled=false;
        picture.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        caption=BattlePresentation.Label(panel.transform,"WebcamStatus",new Vector2(.5f,0),new Vector2(0,6),new Vector2(250,24),15);
        caption.rectTransform.pivot=new Vector2(.5f,0); caption.alignment=TextAlignmentOptions.Center;
        caption.text="Waiting for Python camera";
        StartCoroutine(ReceiveFrames());
    }
    public void TogglePreview() { SetPreviewVisible(!PreviewVisible); }
    public void SetPreviewVisible(bool visible) {
        PreviewVisible=visible;
        if(panel) panel.SetActive(visible);
        if(buttonLabel) buttonLabel.text=visible?"WEBCAM: ON":"WEBCAM: OFF";
    }
    IEnumerator ReceiveFrames() {
        while(true) {
            if(!PreviewVisible) {yield return new WaitForSecondsRealtime(.2f);continue;}
            using(var request=UnityWebRequest.Get(frameUrl)) {
                request.timeout=2;
                yield return request.SendWebRequest();
                if(!PreviewVisible) continue;
                if(request.result==UnityWebRequest.Result.Success && request.downloadHandler.data.Length<=1024*1024) {
                    if(!texture) texture=new Texture2D(2,2,TextureFormat.RGB24,false);
                    if(texture.LoadImage(request.downloadHandler.data)) {
                        image.texture=texture;image.enabled=true;
                        image.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectRatio=(float)texture.width/texture.height;
                        HasFrame=true;ReceivedFrames++;caption.text="Live camera - mirrored";
                    }
                } else {
                    image.enabled=false;HasFrame=false;caption.text="Waiting for Python camera";
                }
            }
            yield return new WaitForSecondsRealtime(1f/12);
        }
    }
    void OnDestroy() { if(texture) Destroy(texture); }
}
