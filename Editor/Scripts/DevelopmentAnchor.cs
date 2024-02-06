using UnityEngine;

namespace Lungfetcher.Editor
{
    [CreateAssetMenu(menuName = "Anchor")]
    public class DevelopmentAnchor : ScriptableObject
    {
        [SerializeField]
        private bool _testOn;

        public bool TestOn => _testOn;
    }
}
