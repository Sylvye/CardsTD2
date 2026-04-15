using UnityEngine;
using UnityEngine.SceneManagement;

namespace RunFlow
{
    public class GameFlowRoot : MonoBehaviour
    {
        private static GameFlowRoot instance;

        public static GameFlowRoot Instance => instance;

        public RunContentRepository ContentRepository { get; private set; }
        public SaveService SaveService { get; private set; }
        public RunCoordinator Coordinator { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRootExistsOnStartup()
        {
            EnsureInstance();
        }

        public static GameFlowRoot EnsureInstance()
        {
            if (instance != null)
                return instance;

            GameObject rootObject = new(nameof(GameFlowRoot));
            instance = rootObject.AddComponent<GameFlowRoot>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }

        public void LoadScene(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
                SceneManager.LoadScene(sceneName);
        }

        public void OverrideServices(RunContentRepository contentRepository, SaveService saveService, RunCoordinator coordinator)
        {
            ContentRepository = contentRepository;
            SaveService = saveService;
            Coordinator = coordinator;
        }

        private void InitializeServices()
        {
            ContentRepository ??= new RunContentRepository();
            SaveService ??= new SaveService(ContentRepository);
            Coordinator ??= new RunCoordinator(SaveService, ContentRepository, LoadScene);
        }
    }
}
