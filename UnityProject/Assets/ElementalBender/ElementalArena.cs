using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Attach to an empty GameObject, or use Tools/Elemental Bender/Create Demo Scene.
public class ElementalArena : MonoBehaviour {
    class Enemy {
        public GameObject body;
        public float hp = 300, slowUntil, stunUntil, attackAt;
    }
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly Dictionary<string,float> ready = new Dictionary<string,float>();
    ElementalReceiver receiver;
    TMP_Text hud;
    float hp = 100, shieldUntil;
    int kills;
    string lastSkill = "Ready";
    bool lost;

    void Start() {
        receiver = GetComponent<ElementalReceiver>();
        if (!receiver) receiver = gameObject.AddComponent<ElementalReceiver>();
        receiver.Cast += Cast;
        Camera cam = Camera.main;
        if (!cam) cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.transform.position = new Vector3(0, 11, -13);
        cam.transform.LookAt(new Vector3(0, 0, 4));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.035f,0.045f,0.075f);
        if (FindFirstObjectByType<Light>() == null) {
            var light = new GameObject("Arena Light",typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(45,-30,0);
        }
        Visual(PrimitiveType.Cube, new Vector3(0,-0.2f,4),new Vector3(14,0.3f,18),new Color(0.12f,0.15f,0.2f));
        Visual(PrimitiveType.Capsule, new Vector3(0,0.8f,0),new Vector3(1,0.8f,1),Color.white);
        var canvas = new GameObject("HUD",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280,720);
        var text = new GameObject("Status",typeof(RectTransform),typeof(TextMeshProUGUI));
        text.transform.SetParent(canvas.transform,false);
        var rect=text.GetComponent<RectTransform>();
        rect.anchorMin=new Vector2(0,1); rect.anchorMax=new Vector2(1,1); rect.pivot=new Vector2(0,1);
        rect.offsetMin=new Vector2(24,-220); rect.offsetMax=new Vector2(-24,-16);
        hud=text.GetComponent<TMP_Text>(); hud.fontSize=24; hud.color=Color.white;
        if (!hud.font && TMP_Settings.instance) hud.font=TMP_Settings.defaultFontAsset;
        if (!hud.font) hud.font=TMP_FontAsset.CreateFontAsset(Font.CreateDynamicFontFromOSFont("Arial",24));
        foreach(string element in new[]{"FIRE","WATER","AIR","EARTH"}) ready[element]=0;
        var opponent=Visual(PrimitiveType.Cube,new Vector3(0,0.7f,6),new Vector3(1.4f,1.4f,1.4f),Color.red);
        opponent.name="DuelEnemy";
        enemies.Add(new Enemy {body=opponent});
    }
    GameObject Visual(PrimitiveType type,Vector3 pos,Vector3 scale,Color color,float lifetime=0) {
        var obj=GameObject.CreatePrimitive(type); obj.transform.position=pos; obj.transform.localScale=scale;
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader=Shader.Find("Standard");
        var material=new Material(shader); material.color=color;
        obj.GetComponent<Renderer>().sharedMaterial=material;
        obj.AddComponent<ElementalMaterialCleanup>().material=material;
        var collider=obj.GetComponent<Collider>(); if(collider) Destroy(collider);
        if(lifetime>0) Destroy(obj,lifetime);
        return obj;
    }
    void Update() {
        for(int i=enemies.Count-1;i>=0;i--) {
            var e=enemies[i];
            if(lost) continue;
            if(Time.time>=e.stunUntil) e.body.transform.position=Vector3.MoveTowards(e.body.transform.position,
                new Vector3(0,0.5f,0),Time.deltaTime*(Time.time<e.slowUntil?0.3f:0.9f));
            if(e.body.transform.position.z<0.7f && Time.time>=e.attackAt && Time.time>=e.stunUntil) {
                e.attackAt=Time.time+2;
                if(Time.time>shieldUntil) hp=Mathf.Max(0,hp-10);
                if(hp<=0) lost=true;
            }
        }
        if(hud) hud.text="ELEMENTAL BENDER - 1 vs 1   Player HP "+hp+"   Enemy HP "+(enemies.Count>0?enemies[0].hp:0)+"\n"+
            (receiver.Connected?receiver.Status+" | transit "+receiver.LastTransitMs.ToString("F0")+" ms":receiver.Status+" (no recent signal)")+"\n"+
            "Last: "+lastSkill+"   Shield: "+Mathf.Max(0,shieldUntil-Time.time).ToString("F1")+"s\n"+
            Cooldowns()+"\n"+(lost?"DEFEAT - stop and play again":kills>0?"VICTORY - stop and play again":"Release hands briefly between gestures");
    }
    string Cooldowns() {
        string text="";
        foreach(var pair in ready) text+=pair.Key+" "+Mathf.Max(0,pair.Value-Time.time).ToString("F1")+"s   ";
        return text;
    }
    void Cast(string element) {
        if(lost || kills>0 || !ready.ContainsKey(element) || Time.time<ready[element]) return;
        ready[element]=Time.time+(element=="EARTH"?4:1.5f);
        lastSkill=element;
        Color color=element=="FIRE"?new Color(1,0.3f,0):element=="WATER"?Color.cyan:element=="AIR"?Color.white:new Color(0.5f,0.3f,0.1f);
        if(element=="EARTH") shieldUntil=Time.time+3;
        Visual(element=="EARTH"?PrimitiveType.Cube:PrimitiveType.Sphere,new Vector3(0,0.7f,3),
            element=="EARTH"?new Vector3(7,1.4f,0.4f):new Vector3(7,0.2f,7),color,0.45f);
        // MVP instant skill area: all enemies within forward 8m and lateral 4m.
        for(int i=enemies.Count-1;i>=0;i--) {
            var e=enemies[i]; var pos=e.body.transform.position;
            if(pos.z>8 || Mathf.Abs(pos.x)>4) continue;
            if(element=="FIRE") e.hp-=60;
            if(element=="WATER") {e.hp-=25; e.slowUntil=Time.time+3;}
            if(element=="AIR") {e.hp-=15; e.body.transform.position+=Vector3.forward*3;}
            if(element=="EARTH") {e.hp-=35; e.stunUntil=Time.time+2;}
            if(e.hp<=0) {Destroy(e.body); enemies.RemoveAt(i); kills++;}
        }
    }
    void OnDestroy() { if(receiver) receiver.Cast-=Cast; }
}
