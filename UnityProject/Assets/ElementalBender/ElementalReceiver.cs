using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

[Serializable] public class ElementPacket {
    public int version;
    public string session;
    public long seq;
    public long sent_unix_ms;
    public string element;
    public int hands;
}

public class ElementalReceiver : MonoBehaviour {
    public int port = 5055;
    public event Action<string> Cast;
    public bool Connected { get { return Time.realtimeSinceStartup - lastSeen < 1.5f; } }
    public float LastTransitMs { get; private set; }
    public string Status { get; private set; } = "Waiting for Python";
    readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
    UdpClient client;
    Thread worker;
    volatile bool running;
    long lastSeq;
    string session;
    float lastSeen = -100;

    void OnEnable() {
        try {
            client = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
            running = true;
            worker = new Thread(Receive) { IsBackground = true };
            worker.Start();
        } catch (SocketException e) { Status = "UDP bind failed: " + e.Message; Debug.LogError(Status); }
    }
    void Receive() {
        while (running) {
            try {
                IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref remote);
                if (data.Length <= 4096 && queue.Count < 128) queue.Enqueue(Encoding.UTF8.GetString(data));
            } catch (SocketException) { if (!running) break; }
              catch (ObjectDisposedException) { break; }
        }
    }
    void Update() {
        int count = 0;
        while (count++ < 128 && queue.TryDequeue(out string json)) {
            ElementPacket p;
            try { p = JsonUtility.FromJson<ElementPacket>(json); } catch (ArgumentException) { continue; }
            if (p == null || p.version != 1 || string.IsNullOrEmpty(p.session) || p.seq <= 0) continue;
            long age = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - p.sent_unix_ms;
            if (age < -500 || age > 500) continue;
            if (p.element != "NONE" && p.element != "FIRE" && p.element != "WATER" && p.element != "AIR" && p.element != "EARTH") continue;
            if (p.session != session) { session = p.session; lastSeq = 0; }
            if (p.seq <= lastSeq) continue;
            lastSeq = p.seq;
            lastSeen = Time.realtimeSinceStartup;
            LastTransitMs = Mathf.Max(0, age);
            Status = "UDP connected | hands " + p.hands;
            if (p.element != "NONE") Cast?.Invoke(p.element);
        }
    }
    void OnDisable() {
        running = false;
        client?.Close();
        worker?.Join(500);
        while (queue.TryDequeue(out _)) {}
        session = null; lastSeq = 0; lastSeen = -100;
    }
}
