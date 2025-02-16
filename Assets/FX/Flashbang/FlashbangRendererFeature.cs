using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FX.Flashbang
{
    public class FlashbangRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private FlashbangSettings _settings;
        private FlashbangRenderPass _pass;
        private Material _material;

        public override void Create()
        {
            _material = new Material(Shader.Find("Pecli/Flashbang"));
            _pass = new FlashbangRenderPass(_material, _settings);
            _pass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(_pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _pass.Dispose();
        }

        public class FlashbangRenderPass : ScriptableRenderPass
        {
            private static bool _flashbangIsRequested;
            private static float _snapshotAmount = 0;
            private static float _whiteAmount;
            
            private readonly Material _material;
            private static FlashbangSettings _settings;
            private RenderTextureDescriptor _snapshotDescriptor;
            private RTHandle _snapshotHandle;
            private static readonly int _snapshotProperty = Shader.PropertyToID("_Snapshot");
            private static readonly int _snapshotBlendProperty = Shader.PropertyToID("_SnapshotBlend");
            private static readonly int _whiteBlendProperty = Shader.PropertyToID("_WhiteBlend");


            public FlashbangRenderPass(Material material, FlashbangSettings settings)
            {
                _settings = settings;
                _material = material;
                _snapshotDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                _snapshotDescriptor.width = cameraTextureDescriptor.width;
                _snapshotDescriptor.height = cameraTextureDescriptor.height;
                RenderingUtils.ReAllocateIfNeeded(ref _snapshotHandle, _snapshotDescriptor);
            }

            private void UpdateMaterialSettings()
            {
                if (_material == null) return;
                _material.SetTexture(_snapshotProperty, _snapshotHandle);
                _material.SetFloat(_snapshotBlendProperty, _snapshotAmount);
                _material.SetFloat(_whiteBlendProperty, _whiteAmount);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get();
                if (_flashbangIsRequested)
                {
                    _flashbangIsRequested = false;
                    Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, _snapshotHandle);
                }
                
                if (_snapshotAmount > 0 || _whiteAmount > 0)
                {
                    UpdateMaterialSettings();
                    Blit(cmd, ref renderingData, _material);
                    context.ExecuteCommandBuffer(cmd);
                    _snapshotAmount -= Time.deltaTime * _settings.SnapshotFadeSpeed;
                    _whiteAmount -= Time.deltaTime * _settings.WhiteFadeSpeed;
                    _snapshotAmount = Mathf.Clamp(_snapshotAmount, 0, Mathf.Infinity);
                    _whiteAmount = Mathf.Clamp(_whiteAmount, 0, Mathf.Infinity);

                    if (_snapshotAmount <= 0 && _whiteAmount <= 0)
                    {
                        _snapshotHandle.Release();
                    }
                }
                CommandBufferPool.Release(cmd);
            }

            public static void DoFlashbang()
            {
                _flashbangIsRequested = true;
                _snapshotAmount = _settings.SnapshotBlendStart;
                _whiteAmount = _settings.WhiteBlendStart;

            }

            public void Dispose()
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying)
                {
                    Destroy(_material);
                }
                else
                {
                    DestroyImmediate(_material);
                }
#else
                Destroy(_material);
#endif
                _snapshotHandle?.Release();
            }
        }

        [Serializable]
        public class FlashbangSettings
        {
            public float WhiteBlendStart = 1;
            public float WhiteFadeSpeed = 1;
            public float SnapshotBlendStart = 1;
            public float SnapshotFadeSpeed = 1;
        }
    }
}
