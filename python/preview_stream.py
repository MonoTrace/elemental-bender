"""Latest-frame JPEG bridge, loopback only. No second camera acquisition."""
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import threading
import time

class PreviewStream:
    def __init__(self, port=5056):
        self._jpeg = None
        self._lock = threading.Lock()
        self._last_publish = -100
        owner = self
        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                if self.path != '/frame.jpg':
                    self.send_error(404)
                    return
                with owner._lock:
                    jpeg = owner._jpeg
                if jpeg is None:
                    self.send_error(503, 'Waiting for camera frame')
                    return
                self.send_response(200)
                self.send_header('Content-Type', 'image/jpeg')
                self.send_header('Content-Length', str(len(jpeg)))
                self.send_header('Cache-Control', 'no-store')
                self.end_headers()
                try:
                    self.wfile.write(jpeg)
                except (BrokenPipeError, ConnectionResetError):
                    pass
            def log_message(self, *args):
                pass
        self.server = ThreadingHTTPServer(('127.0.0.1', port), Handler)
        self.server.daemon_threads = True
        self.worker = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.worker.start()

    def publish(self, frame):
        # Keep encoding bounded; full-resolution input stays in MediaPipe.
        now = time.monotonic()
        if now-self._last_publish < 1/12:
            return
        import cv2
        width = min(480,frame.shape[1])
        small = cv2.resize(frame,(width,round(frame.shape[0]*width/frame.shape[1])))
        ok, jpeg = cv2.imencode('.jpg',small,[cv2.IMWRITE_JPEG_QUALITY,75])
        if ok:
            with self._lock:
                self._jpeg = jpeg.tobytes()
            self._last_publish = now

    def close(self):
        self.server.shutdown()
        self.server.server_close()
        self.worker.join(timeout=1)
