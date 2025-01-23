using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Rendering.OffScreenParticle
{
    public class DownSampleDepthPass : ScriptableRenderPass
    {
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DownSampleDepthPass");
        private OffScreenParticleRendererFeature.Settings m_Settings;

        private Material m_DownSampleMaterial;
        private RTHandle m_DownSampleDepthRT;

        public DownSampleDepthPass(OffScreenParticleRendererFeature.Settings settings)
        {
            m_Settings = settings;
            if (settings.depthDownSamplePS != null)
                m_DownSampleMaterial = CoreUtils.CreateEngineMaterial(settings.depthDownSamplePS);
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
            RenderingUtils.ReAllocateIfNeeded(ref m_DownSampleDepthRT, desc, FilterMode.Bilinear, name: "_DownSampleDepthRT");

            ConfigureTarget(m_DownSampleDepthRT);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_DownSampleMaterial == null)
                return;

            var cmd = CommandBufferPool.Get("OffScreenParticle-----DownSampleDepthPass");
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                // Ensure we flush our command-buffer before we render...
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // m_DownSampleMaterial.SetVector("_PixelSize", new Vector2(1.0f / m_DownSampleDepthRT.rt.width, 1.0f / m_DownSampleDepthRT.rt.height));
                cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);

                //DownSample
                cmd.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, m_DownSampleDepthRT, m_DownSampleMaterial);
                cmd.SetGlobalTexture("_CameraDepthLowRes", m_DownSampleDepthRT);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_DownSampleMaterial);
            m_DownSampleMaterial = null;

            m_DownSampleDepthRT?.Release();
        }
    }
}