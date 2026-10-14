using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Attach to an empty GameObject, or use Tools/Elemental Bender/Create Demo Scene.
public class ElementalArena : MonoBehaviour {
    public GameObject playerModelPrefab, enemyModelPrefab;
    public bool fixedDuelStaging = true;
    class Enemy {
        public GameObject body;
        public float hp = 300, slowUntil, stunUntil, attackAt;
    }
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly Dictionary<string,float> ready = new Dictionary<string,float>();
    ElementalReceiver receiver;
    TMP_Text hud;
    BattlePresentation presentation;
    float hp = 100, shieldUntil;
    int kills;
    string lastSkill = "Ready";
    bool lost;

    void Start() {
        Application.runInBackground=true;
        receiver = GetComponent<ElementalReceiver>();
        if (!receiver) receiver = gameObject.AddComponent<ElementalReceiver>();
        receiver.Cast += Cast;
        Camera cam = Camera.main;
        if (!cam) cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag="MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.035f,0.045f,0.075f);
        if (FindFirstObjectByType<Light>() == null) {
            var light = new GameObject("Arena Light",typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(45,-30,0);
        }
        Visual(PrimitiveType.Cube, new Vector3(0,-0.2f,4),new Vector3(14,0.3f,18),new Color(0.12f,0.15f,0.2f));
        var playerPosition=new Vector3(-2,0,0);
        var opponentPosition=new Vector3(2,0,6);
        float facing=Mathf.Atan2(opponentPosition.x-playerPosition.x,opponentPosition.z-playerPosition.z)*Mathf.Rad2Deg;
        var player=Character(playerModelPrefab,playerPosition,Color.cyan,"DuelPlayer",facing);
        player.transform.localScale=Vector3.one*1.3f;
        Visual(PrimitiveType.Cylinder,playerPosition+new Vector3(0,-.03f,0),new Vector3(2.4f,.05f,1.8f),new Color(.2f,.3f,.38f));
        Visual(PrimitiveType.Cylinder,opponentPosition+new Vector3(0,-.03f,0),new Vector3(2.4f,.05f,1.8f),new Color(.3f,.25f,.25f));
        presentation=gameObject.AddComponent<BattlePresentation>();presentation.Build(cam);
        hud=presentation.Status;
        foreach(string element in new[]{"FIRE","WATER","AIR","EARTH"}) ready[element]=0;
        var opponent=Character(enemyModelPrefab,opponentPosition,new Color(.9f,.35f,.2f),"DuelEnemy",facing+180);
        enemies.Add(new Enemy {body=opponent});
    }
    GameObject Character(GameObject prefab,Vector3 position,Color color,string name,float facing) {
        if(prefab) {
            var model=Instantiate(prefab,position,Quaternion.Euler(0,facing,0));model.name=name;return model;
        }
        var root=new GameObject(name);root.transform.position=position;
        // Rig-free human-shaped stand-ins. Replace with supplied humanoid prefabs later.
        Part(root,PrimitiveType.Sphere,new Vector3(0,1.85f,0),Vector3.one*.44f,new Color(.9f,.7f,.52f));
        Part(root,PrimitiveType.Capsule,new Vector3(0,1.13f,0),new Vector3(.58f,.45f,.4f),color);
        foreach(float side in new[]{-1f,1f}) {
            Part(root,PrimitiveType.Capsule,new Vector3(side*.4f,1.1f,.06f),new Vector3(.17f,.34f,.17f),color);
            Part(root,PrimitiveType.Capsule,new Vector3(side*.19f,.43f,0),new Vector3(.22f,.37f,.22f),new Color(.13f,.18f,.26f));
            Part(root,PrimitiveType.Sphere,new Vector3(side*.08f,1.88f,.2f),Vector3.one*.055f,Color.black);
        }
        root.transform.rotation=Quaternion.Euler(0,facing,0);return root;
    }
    void Part(GameObject root,PrimitiveType type,Vector3 position,Vector3 scale,Color color) {
        var part=Visual(type,root.transform.position+position,scale,color);part.transform.SetParent(root.transform,true);
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
            if(lost || fixedDuelStaging) continue;
            if(Time.time>=e.stunUntil) e.body.transform.position=Vector3.MoveTowards(e.body.transform.position,
                new Vector3(0,0.5f,0),Time.deltaTime*(Time.time<e.slowUntil?0.3f:0.9f));
            if(e.body.transform.position.z<0.7f && Time.time>=e.attackAt && Time.time>=e.stunUntil) {
                e.attackAt=Time.time+2;
                if(Time.time>shieldUntil) hp=Mathf.Max(0,hp-10);
                if(hp<=0) lost=true;
            }
        }
        if(presentation) presentation.SetHealth(hp,enemies.Count>0?enemies[0].hp:0);
        if(hud) hud.text="ELEMENTAL BENDER - 1 vs 1   Player HP "+hp+"   Enemy HP "+(enemies.Count>0?enemies[0].hp:0)+"\n"+
            (receiver.Connected?receiver.Status+" | transit "+receiver.LastTransitMs.ToString("F0")+" ms":receiver.Status+" (no recent signal)")+"\n"+
            "Last: "+lastSkill+"   Shield: "+Mathf.Max(0,shieldUntil-Time.time).ToString("F1")+"s\n"+
            Cooldowns()+"\n"+(lost?"DEFEAT - stop and play again":kills>0?"VICTORY - stop and play again":fixedDuelStaging?"Gesture demo - elemental attack/defense definitions pending":"Release hands briefly between gestures");
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
            if(element=="AIR") {e.hp-=15; if(!fixedDuelStaging) e.body.transform.position+=Vector3.forward*3;}
            if(element=="EARTH") {e.hp-=35; e.stunUntil=Time.time+2;}
            if(e.hp<=0) {Destroy(e.body); enemies.RemoveAt(i); kills++;}
        }
    }
    void OnDestroy() { if(receiver) receiver.Cast-=Cast; }
}
