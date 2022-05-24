
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech
{
    public class UiShapeFixes : UdonSharpBehaviour
    {
        void Start()
        {
            var canvases = GetComponentsInChildren(typeof(VRC_UiShape), true);
            foreach (Component c in canvases)
            {
                var box = c.GetComponent<BoxCollider>();
                if (box != null)
                {
                    var rect = (RectTransform)c.transform;
                    box.isTrigger = true;
                    box.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 0);
                }
            }
        }
    }
}
