using UnityEngine;

namespace Lungfetcher.Development
{
    /// <summary>
    /// The development anchor is a ScripteableObject wich holds development states and informations.
    /// It is supposed to be located under Lungfetcher/Runtime/Resources and it is automatically removed
    /// upon release.
    /// </summary>
    [CreateAssetMenu(fileName = "DevelopmentAnchor", menuName = "Lungfetcher/Development Anchor", order = 0)]
    public class DevelopmentAnchor : ScriptableObject
    {
        [SerializeField]
        private bool _developmentOn;

        public bool DevelopmentOn => _developmentOn;
    }
}
