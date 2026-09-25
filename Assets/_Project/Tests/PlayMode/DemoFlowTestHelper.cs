using System.Collections;
using TimeMetroidvania.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeMetroidvania.Tests
{
    internal static class DemoFlowTestHelper
    {
        public static IEnumerator LoadStartedSevenRoomSlice()
        {
            SceneManager.LoadScene("SevenRoomSlice");
            yield return null;
            Object.FindFirstObjectByType<StageSevenFlowController>()?.StartNewGame();
            yield return null;
        }
    }
}
