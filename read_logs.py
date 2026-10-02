import sys
import json
sys.path.append(r'C:\Users\Artemis\.unity\relay')
from call_unity_mcp import call_unity_tool

res = call_unity_tool("Unity_GetConsoleLogs", {})
content = res.get("result", {}).get("content", [{}])[0].get("text", "")
data = json.loads(content)
logs = data.get("data", {}).get("logs", [])
print(f"Total logs: {len(logs)}")
for l in logs[-15:]:
    print(l.get("type"), ":", l.get("message"))
