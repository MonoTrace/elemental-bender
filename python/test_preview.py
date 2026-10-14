import unittest
import urllib.request
import urllib.error
import cv2
import numpy as np
from preview_stream import PreviewStream

class PreviewTests(unittest.TestCase):
    def test_frame_delivery_and_shutdown(self):
        stream=PreviewStream(0)
        url=f'http://127.0.0.1:{stream.server.server_address[1]}/frame.jpg'
        try:
            with self.assertRaises(urllib.error.HTTPError) as error:
                urllib.request.urlopen(url,timeout=2)
            self.assertEqual(error.exception.code,503)
            error.exception.close()
            stream.publish(np.zeros((480,640,3),dtype=np.uint8))
            with urllib.request.urlopen(url,timeout=2) as response:
                self.assertEqual(response.headers['Content-Type'],'image/jpeg')
                decoded=cv2.imdecode(np.frombuffer(response.read(),dtype=np.uint8),cv2.IMREAD_COLOR)
            self.assertEqual(decoded.shape[:2],(360,480))
        finally:
            stream.close()
        self.assertFalse(stream.worker.is_alive())

if __name__=='__main__':unittest.main()
