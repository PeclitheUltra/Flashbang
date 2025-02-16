using UnityEngine;

namespace FX.Flashbang
{
    public class FlashbangUsageExample : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Flashbang();
            }
        }

        public void Flashbang()
        {
            FlashbangRendererFeature.FlashbangRenderPass.DoFlashbang();
        }
    }
}
