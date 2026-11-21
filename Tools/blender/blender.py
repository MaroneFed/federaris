"""Parle au Blender ouvert de Martin, par le module "MCP for Blender" (port 9876, sur ce PC seulement).
Usage : python Tools/blender/blender.py info
        python Tools/blender/blender.py code <fichier.py>     (execute ce fichier dans Blender)
        python Tools/blender/blender.py capture <image.png>    (photo de la vue 3D)"""
import json, socket, sys


def send(kind, params=None, timeout=180):
    s = socket.create_connection(("localhost", 9876), timeout=timeout)
    s.sendall(json.dumps({"type": kind, "params": params or {}}).encode("utf-8"))
    buf = b""
    while True:
        part = s.recv(65536)
        if not part:
            break
        buf += part
        try:
            return json.loads(buf.decode("utf-8"))
        except ValueError:
            continue
    s.close()
    return json.loads(buf.decode("utf-8"))


if __name__ == "__main__":
    what = sys.argv[1]
    if what == "info":
        r = send("get_scene_info")
    elif what == "code":
        r = send("execute_code", {"code": open(sys.argv[2], encoding="utf-8").read()})
    elif what == "capture":
        r = send("get_viewport_screenshot", {"filepath": sys.argv[2], "max_size": 1600, "format": "png"})
    else:
        r = send(what)
    print(json.dumps(r, ensure_ascii=False, indent=1)[:4000])
