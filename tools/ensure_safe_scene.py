import urllib.request
import json
import os
import sys
import time

def get_port():
    port_file = os.path.join(os.path.dirname(os.path.dirname(__file__)), 'ProjectSettings', 'UnityMcpPort.txt')
    try:
        with open(port_file, 'r') as f:
            return f.read().strip()
    except Exception as e:
        print(f"Error reading port: {e}", file=sys.stderr)
        sys.exit(1)

def mcp_request(endpoint, data=None):
    port = get_port()
    url = f"http://127.0.0.1:{port}{endpoint}"
    req = urllib.request.Request(url)
    if data:
        req.add_header('Content-Type', 'application/json')
        req.data = json.dumps(data).encode('utf-8')
    
    try:
        with urllib.request.urlopen(req, timeout=10) as response:
            return json.loads(response.read().decode('utf-8'))
    except Exception as e:
        print(f"MCP request failed: {e}", file=sys.stderr)
        sys.exit(1)

def main():
    print("Checking current scene...")
    
    # 1. Health check
    health = mcp_request('/health')
    current_scene = health.get('activeScenePath', '')
    
    print(f"Current scene: {current_scene}")
    
    # If we are in a historical lobby authoring scene, switch away before touching files on disk.
    if current_scene.endswith('/LobbyScene.unity'):
        print("Historical lobby authoring scene is open. Switching to safe scene...")
        
        safe_scene = "Assets/FromStore/Plugins/Demigiant/DOTweenPro Examples/DOTweenAnimation_Basics.unity"
        
        result = mcp_request('/scene/open', {
            "scenePath": safe_scene,
            "saveCurrentSceneIfDirty": False
        })
        
        if result.get('success'):
            print(f"Switched to: {safe_scene}")
            print("READY")
        else:
            print(f"Failed to switch: {result}", file=sys.stderr)
            sys.exit(1)
    else:
        print("Safe scene already active or no historical lobby scene is open.")
        print("ALREADY_SAFE")

if __name__ == "__main__":
    main()
