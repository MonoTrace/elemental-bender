using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class BattlePresentation : MonoBehaviour {
    readonly System.Collections.Generic.List<Sprite> generatedSprites=new System.Collections.Generic.List<Sprite>();
    UnityEngine.UI.Image playerBar, enemyBar;
    TMP_Text playerLabel, enemyLabel;
    public TMP_Text Status { get; private set; }
    public WebcamPreview Preview { get; private set; }

    public void Build(Camera cam) {
        // Rear-left player, front-right opponent; fixed diagonal 3D battle view.
        cam.transform.position=new Vector3(-1.6892796f,1.7757857f,-1.1559143f);
        cam.transform.rotation=new Quaternion(.030648912f,.21299562f,-.006684841f,.9765495f);
        cam.transform.localScale=Vector3.one;
        cam.orthographic=true;cam.orthographicSize=3.8f;
        cam.rect=new Rect(0,.23f,1,.77f);
        cam.backgroundColor=new Color(.12f,.17f,.24f);
        var canvas=new GameObject("BattleHUD",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var dialog=Panel(canvas.transform,"BattleMessage",new Vector2(.5f,0),new Vector2(0,12),new Vector2(1232,152),new Color(.04f,.065f,.1f));
        dialog.rectTransform.pivot=new Vector2(.5f,0);
        Status=Label(dialog.transform,"Status",new Vector2(0,1),new Vector2(22,-12),new Vector2(1180,130),21);
        playerLabel=Label(canvas.transform,"PlayerHP",new Vector2(0,0),new Vector2(28,192),new Vector2(360,36),24);
        playerLabel.rectTransform.pivot=Vector2.zero;
        playerBar=HealthBar(canvas.transform,"PlayerHealth",new Vector2(0,0),new Vector2(28,178));
        enemyLabel=Label(canvas.transform,"EnemyHP",new Vector2(0,1),new Vector2(28,-22),new Vector2(360,36),24);
        enemyBar=HealthBar(canvas.transform,"EnemyHealth",new Vector2(0,1),new Vector2(28,-65));
        if(!FindFirstObjectByType<EventSystem>()) {
            var events=new GameObject("BattleEvents",typeof(EventSystem));
            #if ENABLE_INPUT_SYSTEM
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            #else
            events.AddComponent<StandaloneInputModule>();
            #endif
        }
        Preview=gameObject.AddComponent<WebcamPreview>();Preview.Build(canvas.transform);
    }
    public void SetHealth(float playerHP,float enemyHP) {
        playerLabel.text="PLAYER   "+playerHP.ToString("F0")+" / 100";
        enemyLabel.text="OPPONENT   "+enemyHP.ToString("F0")+" / 300";
        playerBar.fillAmount=Mathf.Clamp01(playerHP/100);enemyBar.fillAmount=Mathf.Clamp01(enemyHP/300);
    }
    UnityEngine.UI.Image HealthBar(Transform parent,string name,Vector2 anchor,Vector2 position) {
        var background=Panel(parent,name,anchor,position,new Vector2(320,12),new Color(.06f,.08f,.12f));
        var fill=Panel(background.transform,"Fill",Vector2.zero,Vector2.zero,new Vector2(320,12),new Color(.24f,.84f,.65f));
        fill.rectTransform.pivot=Vector2.zero;
        fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;
        fill.rectTransform.offsetMin=Vector2.zero;fill.rectTransform.offsetMax=Vector2.zero;
        // Filled Image requires a sprite on some Unity versions; use a shared white sprite.
        fill.sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f));
        generatedSprites.Add(fill.sprite);
        fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        return fill;
    }
    public static UnityEngine.UI.Image Panel(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Color color) {
        var obj=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));obj.transform.SetParent(parent,false);
        var rect=obj.GetComponent<RectTransform>();rect.anchorMin=anchor;rect.anchorMax=anchor;rect.pivot=anchor;
        rect.anchoredPosition=position;rect.sizeDelta=size;
        var image=obj.GetComponent<UnityEngine.UI.Image>();image.color=color;return image;
    }
    public static TMP_Text Label(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,float fontSize) {
        var obj=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));obj.transform.SetParent(parent,false);
        var text=obj.GetComponent<TMP_Text>();var rect=text.rectTransform;
        rect.anchorMin=anchor;rect.anchorMax=anchor;rect.pivot=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;
        text.fontSize=fontSize;text.color=Color.white;text.raycastTarget=false;
        if(TMP_Settings.instance) text.font=TMP_Settings.defaultFontAsset;
        return text;
    }
    void OnDestroy() {foreach(var sprite in generatedSprites) if(sprite) Destroy(sprite);}
}
