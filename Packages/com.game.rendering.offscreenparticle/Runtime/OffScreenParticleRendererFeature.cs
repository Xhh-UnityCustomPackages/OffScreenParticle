using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Rendering.OffScreenParticle
{
    [DisallowMultipleRendererFeature]
    public class OffScreenParticleRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Settings m_Settings = new();

        private OffScreenParticleRendererPass m_RendererPass;
        private DownSampleDepthPass m_DownSampleDepthPass;
        private OffScreenParticleMergePass m_MergePass;

        private RTHandle m_ParticleRT;

        public override void Create()
        {
            m_RendererPass = new OffScreenParticleRendererPass(m_Settings, m_ParticleRT);
            m_RendererPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            m_DownSampleDepthPass = new DownSampleDepthPass(m_Settings);
            m_DownSampleDepthPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            m_MergePass = new OffScreenParticleMergePass(m_Settings);
            m_MergePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Reflection || renderingData.cameraData.cameraType == CameraType.Preview)
                return;

            renderer.EnqueuePass(m_RendererPass);
            renderer.EnqueuePass(m_DownSampleDepthPass);
            renderer.EnqueuePass(m_MergePass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            m_RendererPass?.Dispose();
            m_DownSampleDepthPass?.Dispose();
            m_MergePass?.Dispose();
        }

        [Serializable]
        public class Settings
        {
            public LayerMask particleLayerMask;
            public Resolution resolution = Resolution.Half;
            [Range(0.0001f, 0.01f)] public float depthThreshold = 0.005f;


            public Shader depthDownSamplePS;
            public Shader mergePS;
        }

        public enum Resolution
        {
            Full = 0,
            Half = 1, // 1/2
            Quarter = 2, // 1/4
        }
    }
}