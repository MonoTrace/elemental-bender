#if UNITY_EDITOR
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEngine;

// Run Unity with -batchmode -executeMethod ElementalSmokeTest.Run (without -quit).
[InitializeOnLoad]
public static class ElementalSmokeTest {
    const string Flag="ElementalBender.Smoke";
    static double start;
    static int phase, casts;
    static ElementalReceiver receiver;
    static string session;
    static ElementalSmokeTest() {
        if(SessionState.GetBool(Flag,false)) {
            start=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=Log;
        }
    }
    public static void Run() {
        CreateDemoScene.CreateBatch();
        SessionState.SetBool(Flag,true);
        start=EditorApplication.timeSinceStartup;
        EditorApplication.update-=Tick;
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=Log;
    }
    static void Log(string message,string stack,LogType type) {
        if(type==LogType.Exception || type==LogType.Error) Finish(1,"Runtime error: "+message+"\n"+stack);
    }
    static void Tick() {
        if(EditorApplication.timeSinceStartup-start>60) {Finish(1,"Play-mode timeout");return;}
        if(!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode &&
           !EditorApplication.isCompiling && !EditorApplication.isUpdating &&
           System.IO.File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) {
            EditorApplication.EnterPlaymode();
            return;
        }
        if(!EditorApplication.isPlaying || Time.time<0.5f) return;
        if(!receiver) {
            receiver=UnityEngine.Object.FindFirstObjectByType<ElementalReceiver>();
            if(!receiver) {Finish(1,"Receiver missing");return;}
            receiver.Cast+=_=>casts++;
            session=Guid.NewGuid().ToString("N");
        }
        string[] labels={"FIRE","WATER","AIR","EARTH"};
        if(phase<4 && Time.time>0.7f+phase*1.7f) {
            var packet=new ElementPacket {version=1,session=session,seq=phase+1,
                sent_unix_ms=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),element=labels[phase],hands=2};
            using(var sender=new UdpClient()) {
                byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
                for(int n=0;n<3;n++) sender.Send(bytes,bytes.Length,new IPEndPoint(IPAddress.Loopback,5055));
            }
            phase++;
        }
        if(phase==4 && Time.time>7) {
            if(casts!=4) {Finish(1,"Expected 4 casts, got "+casts);return;}
            var arena=UnityEngine.Object.FindFirstObjectByType<ElementalArena>();
            var text=UnityEngine.Object.FindFirstObjectByType<TMPro.TextMeshProUGUI>();
            if(!arena || !text || !text.text.Contains("EARTH")) {Finish(1,"HUD/arena missing");return;}
            Finish(0,"PASS: Play-mode arena/HUD, four UDP casts, duplicate suppression, clean shutdown");
        }
    }
    static void Finish(int code,string message) {
        SessionState.SetBool(Flag,false);
        EditorApplication.update-=Tick;
        Application.logMessageReceived-=Log;
        Debug.Log(message);
        // Exit disposes the play session and sockets.
        EditorApplication.Exit(code);
    }
}
#endif
