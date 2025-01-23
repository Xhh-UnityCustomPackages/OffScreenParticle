using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Rendering.OffScreenParticle
{
    public class OffScreenParticleRendererPass : ScriptableRenderPass
    {
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("OffScreenParticleRendererPass");
        private OffScreenParticleRendererFeature.Settings m_Settings;

        private RTHandle m_ParticleRT;
        private FilteringSettings m_FilteringSettings;
        private RenderStateBlock m_RenderStateBlock;


        private readonly List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>()
        {
            new ShaderTagId("OffScreenParticle")
        };

        public OffScreenParticleRendererPass(OffScreenParticleRendererFeature.Settings settings, RTHandle particleRT)
        {
            m_Settings = settings;
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.all, settings.particleLayerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_ParticleRT = particleRT;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.colorFormat = RenderTextureFormat.ARGB32;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            //降采样
            desc.width = desc.width >> (int)m_Settings.resolution;
            desc.height = desc.height >> (int)m_Settings.resolution;
            RenderingUtils.ReAllocateIfNeeded(ref m_ParticleRT, desc, FilterMode.Bilinear, name: "_ParticleRT");
            ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 0));
            ConfigureTarget(m_ParticleRT);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);

            var cmd = CommandBufferPool.Get("OffScreenParticle-----RendererPass");
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                // Ensure we flush our command-buffer before we render...
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Render the objects...
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
                cmd.SetGlobalTexture("_ParticleRT", m_ParticleRT);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            m_ParticleRT?.Release();
        }
    }
}