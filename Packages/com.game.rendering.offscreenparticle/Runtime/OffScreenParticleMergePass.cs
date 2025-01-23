using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

namespace Game.Rendering.OffScreenParticle
{
    public class OffScreenParticleMergePass : ScriptableRenderPass
    {
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("OffScreenParticleMergePass");
        private OffScreenParticleRendererFeature.Settings m_Settings;

        private Material m_MergeMaterial;
        private RTHandle m_MergeRT;

        public OffScreenParticleMergePass(OffScreenParticleRendererFeature.Settings settings)
        {
            m_Settings = settings;
            if (settings.mergePS != null)
                m_MergeMaterial = CoreUtils.CreateEngineMaterial(settings.mergePS);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.colorFormat = RenderTextureFormat.ARGB32;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref m_MergeRT, desc, FilterMode.Bilinear, name: "_MergeRT");

            ConfigureClear(ClearFlag.Color, Color.clear);
            ConfigureTarget(m_MergeRT);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_MergeMaterial == null) return;

            var cmd = CommandBufferPool.Get("OffScreenParticle-----MergePass");
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                // Ensure we flush our command-buffer before we render...
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                RenderTextureDescriptor Desc = renderingData.cameraData.cameraTargetDescriptor;

                Vector2 pixelSize = new Vector2((1.0f + (int)m_Settings.resolution) / Desc.width, (1.0f + (int)m_Settings.resolution) / Desc.height);
                m_MergeMaterial.SetVector("_LowResPixelSize", pixelSize);
                // m_MergeMaterial.SetVector("_LowResTextureSize", new Vector2(Desc.width / 2, Desc.height / 2));
                // m_MergeMaterial.SetFloat("_DepthMult", 32.0f);
                m_MergeMaterial.SetFloat("_Threshold", m_Settings.depthThreshold);
                //DownSample
                cmd.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, m_MergeRT, m_MergeMaterial);
                cmd.Blit(m_MergeRT, renderingData.cameraData.renderer.cameraColorTargetHandle);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }


        public void Dispose()
        {
            CoreUtils.Destroy(m_MergeMaterial);
            m_MergeMaterial = null;
        }
    }
}