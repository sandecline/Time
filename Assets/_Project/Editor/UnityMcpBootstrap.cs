#if UNITY_EDITOR
using MCPForUnity.Editor;
using UnityEditor;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 在编辑器加载完成后启动本地 Unity MCP 桥接。
    /// </summary>
    [InitializeOnLoad]
    internal static class UnityMcpBootstrap
    {
        static UnityMcpBootstrap()
        {
            EditorApplication.delayCall += McpCiBoot.StartStdioForCi;
        }
    }
}
#endif
