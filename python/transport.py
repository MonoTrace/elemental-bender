import json
import socket
import time
import uuid

class Sender:
    def __init__(self, host='127.0.0.1', port=5055):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.address = (host,port)
        self.session = uuid.uuid4().hex
        self.seq = 0

    def send(self, element='NONE', hands=0):
        self.seq += 1
        packet = dict(version=1, session=self.session, seq=self.seq,
                      sent_unix_ms=int(time.time()*1000), element=element, hands=hands)
        # Repeat casts with identical seq; Unity deduplicates. Heartbeats only once.
        data = json.dumps(packet).encode('utf-8')
        for _ in range(3 if element!='NONE' else 1):
            self.sock.sendto(data,self.address)
        return packet

    def close(self):
        self.sock.close()
