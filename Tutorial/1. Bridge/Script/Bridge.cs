using UnityEngine;

namespace Strix.ShapeBuilder.Bridge
{
    public class Bridge : MonoBehaviour
    {
        [ContextMenu("RefreshPillarsHeight")]
        public void RefreshPillarsHeight()
        {
            Pillars[] pillars = GetComponentsInChildren<Pillars>();
            foreach (var pillar in pillars)
                pillar.RefreshPillar();
        }
    }
}

