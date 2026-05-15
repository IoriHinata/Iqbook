using Iqbook.Runtime.Core;
using Iqbook.Runtime.IO;
using UnityEngine;

namespace Iqbook.Runtime.UI
{
    public class ReaderBootstrap : MonoBehaviour
    {
        [SerializeField] private string iqbookPath;

        private void Start()
        {
            var package = new IqbookPackageService();
            var metadata = package.LoadAndValidate(iqbookPath, out var content);
            var runner = new StoryRunner(content);
            Debug.Log($"Loaded book: {metadata.title} ({metadata.author}), current node={runner.Current.id}");
        }
    }
}
