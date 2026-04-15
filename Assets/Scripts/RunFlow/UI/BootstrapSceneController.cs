using UnityEngine;

namespace RunFlow
{
    public class BootstrapSceneController : MonoBehaviour
    {
        private void Start()
        {
            GameFlowRoot.EnsureInstance();
            GameFlowRoot.Instance.LoadScene(SceneNames.MainMenu);
        }
    }
}
