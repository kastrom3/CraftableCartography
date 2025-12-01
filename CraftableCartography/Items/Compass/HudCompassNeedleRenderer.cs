using HarmonyLib;
using System;
using System.Reflection.Metadata.Ecma335;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace CraftableCartography.Items.Compass
{
    internal class HudCompassNeedleRenderer : IRenderer, IDisposable
    {
        ICoreClientAPI api;

        private float _heading;

        public float heading
        {
            get
            {
                return _heading;
            }
            set
            {
                _heading = value;

                compassAngle = 360 - heading;
            }
        }

        private float _compassZoom;

        public float compassZoom
        {
            get
            {
                return _compassZoom;
            }
            set
            {
                _compassZoom = Math.Clamp(value, 0.7f, 1.3f);
            }
        }

        private float compassAngle;

        private MeshData[] meshDatas;
        private MeshRef[] meshRefs;

        private MeshData[] labelMeshDatas;
        private MeshRef[] labelMeshRefs;
        private LoadedTexture[] labelTextures;

        private Item item;

        private TextureAtlasPosition itemTexturePosition;

        private bool isPrimitive
        {
            get
            {
                return item.Code.FirstCodePart() == "primitivecompass";
            }
        }

        public double RenderOrder
        {
            get
            {
                return 1;
            }
        }

        public int RenderRange
        {
            get
            {
                return 10;
            }
        }

        public HudCompassNeedleRenderer(ICoreClientAPI api, Item item)
        {
            this.api = api;
            this.item = item;

            itemTexturePosition = api.ItemTextureAtlas.GetPosition(item, "tinbronze");

            api.Event.RegisterRenderer(this, EnumRenderStage.Ortho);

            PrepareMesh();

            compassZoom = 0.7f;
        }

        void PrepareMesh()
        {
            /*
            needleMeshData = new(3, 4, false, false, true, false);

            needleMeshData.AddVertexSkipTex(-0.1f, 0, 0);
            needleMeshData.AddVertexSkipTex(0.1f, 0, 0);
            needleMeshData.AddVertexSkipTex(0, -1.5f, 0);

            needleMeshData.AddIndices(new int[] { 0, 1, 2, 0 });
            */

            meshDatas = new MeshData[33];
            labelMeshDatas = new MeshData[meshDatas.Length];
            labelTextures = new LoadedTexture[meshDatas.Length];

            if (!isPrimitive)
            {
                MeshData circleMeshData = new(720, 721 * 6, false, true, true, false);

                float circleWidth = 1.1f;

                for (int i = 0; i < 360; i++)
                {
                    double angle = i * (Math.PI / 180);

                    float x = (float)Math.Sin(angle) * 2;
                    float y = (float)-Math.Cos(angle) * 2;

                    circleMeshData.AddVertex(x, y, 0, (x / 4.4f) + 0.5f, (y / 4.4f) + 0.5f);
                    circleMeshData.AddVertex(x * circleWidth, y * circleWidth, 0, ((x * circleWidth) / 4.4f) + 0.5f, ((y * circleWidth) / 4.4f) + 0.5f);

                    if (i > 0)
                    {
                        circleMeshData.AddIndices(new int[] { (i * 2) - 2, (i * 2) - 1, (i * 2) + 0 });
                        circleMeshData.AddIndices(new int[] { (i * 2) + 0, (i * 2) - 1, (i * 2) + 1 });
                    }
                }

                circleMeshData.AddIndices(new int[] { 718, 719, 0 });
                circleMeshData.AddIndices(new int[] { 0, 719, 1 });

                circleMeshData.SetTexPos(itemTexturePosition);

                meshDatas[0] = circleMeshData;
            }

            int j = 1;
            int k = 0;
            for (float i = 0; i < 360; i += 11.25f)
            {
                if (isPrimitive)
                {
                    if (i != 0) continue;
                }

                float baseWidth;

                float baseRadius;

                if (i == 0)
                {
                    baseRadius = 0.25f;
                    baseWidth = 0.4f;
                }
                else if (i % 90 == 0)
                {
                    baseRadius = 0.5f;
                    baseWidth = 0.2f;
                }
                else if (i % 45 == 0)
                {
                    baseRadius = 1f;
                    baseWidth = 0.1f;
                }
                else if (i % 22.5 == 0)
                {
                    baseRadius = 1.5f;
                    baseWidth = 0.1f;
                } else
                {
                    baseRadius = 1.75f;
                    baseWidth = 0.05f;
                }

                float tipRadius = 2f;

                Vec3f p1 = new(baseWidth * -0.5f, 0, baseRadius);
                Vec3f p2 = new(baseWidth * 0.5f, 0, baseRadius);
                Vec3f p3 = new(0, 0, tipRadius);

                p1 = p1.RotatedCopy(i + 180);
                p2 = p2.RotatedCopy(i + 180);
                p3 = p3.RotatedCopy(i + 180);

                MeshData lineMesh = new(3, 3, false, true, true, false);

                lineMesh.AddVertex(p1.X, p1.Z, 0, (p1.X / 4) + 0.5f, (p1.Z / 4) + 0.5f);
                lineMesh.AddVertex(p2.X, p2.Z, 0, (p2.X / 4) + 0.5f, (p2.Z / 4) + 0.5f);
                lineMesh.AddVertex(p3.X, p3.Z, 0, (p3.X / 4) + 0.5f, (p3.Z / 4) + 0.5f);

                lineMesh.AddIndices(new int[] { 0, 1, 2 });

                lineMesh.SetTexPos(itemTexturePosition);

                meshDatas[j] = lineMesh;
                j++;

                if (!isPrimitive)
                {
                    if (i % 22.5 == 0)
                    {
                        LoadedTexture labelTexture = new(api);

                        float i_hdg = i + 180;
                        while (i_hdg >= 360) i_hdg -= 360;

                        string str = i_hdg == 0 ? "-N-" : i_hdg.ToString();
                        CairoFont font = CairoFont.WhiteDetailText()
                            .WithColor(new double[] { 1, 1, 1, 1 })
                            .WithFontSize(32)
                            .WithStroke(new double[] { 0, 0, 0, 1 }, 2);
                        if (i_hdg == 0)
                        {
                            font.FontWeight = Cairo.FontWeight.Bold;
                            font.Color = new double[] { 1, 0, 0, 1 };
                            font.StrokeWidth = 0;
                        }

                        api.Gui.TextTexture.GenOrUpdateTextTexture(str, font, ref labelTexture);

                        labelTextures[k] = labelTexture;

                        float labelHeight = 0.2f;
                        float labelXScale = labelTexture.Width / labelTexture.Height;

                        Vec3f lp1 = new(-labelHeight * labelXScale * 0.5f, 0, 2f + labelHeight);
                        Vec3f lp2 = new(labelHeight * labelXScale * 0.5f, 0, 2f);

                        //lp1 = lp1.RotatedCopy(i);
                        //lp2 = lp2.RotatedCopy(i);

                        MeshData labelMesh = new(4, 6, false, true, true, false);

                        labelMesh.AddVertex(lp1.X, lp1.Z, 0, 1, 0);
                        labelMesh.AddVertex(lp1.X, lp2.Z, 0, 1, 1);
                        labelMesh.AddVertex(lp2.X, lp2.Z, 0, 0, 1);
                        labelMesh.AddVertex(lp2.X, lp1.Z, 0, 0, 0);

                        labelMesh.AddIndices(0, 1, 2, 0, 2, 3);

                        labelMeshDatas[k] = labelMesh;
                        k++;
                    }
                }
            }
            
            meshRefs = new MeshRef[meshDatas.Length];
            labelMeshRefs = new MeshRef[meshDatas.Length];

            for (int i = 0; i < meshDatas.Length; i++)
            {
                if (meshDatas[i] is not null)
                {
                    if (meshRefs[i] is null) meshRefs[i] = api.Render.UploadMesh(meshDatas[i]);
                    else api.Render.UpdateMesh(meshRefs[i], meshDatas[i]);
                }

                if (labelMeshDatas[i] is not null)
                {
                    if (labelMeshRefs[i] is null) labelMeshRefs[i] = api.Render.UploadMesh(labelMeshDatas[i]);
                    else api.Render.UpdateMesh(labelMeshRefs[i], labelMeshDatas[i]);
                }
            }
        }

        public void Dispose()
        {
            if (labelTextures != null)
            {
                foreach (LoadedTexture text in labelTextures)
                {
                    if (text != null)
                    {
                        text.Dispose();
                    }
                }
            }
            labelTextures = null;
            if (meshRefs is not null)
            {
                foreach (MeshRef meshRef in meshRefs)
                {
                    api.Render.DeleteMesh(meshRef);
                }
            }
            if (meshDatas is not null)
            {
                foreach (MeshData meshDat in meshDatas)
                {
                    meshDat?.Dispose();
                }
            }
            if (labelMeshDatas is not null)
            {
                foreach (MeshData meshDat2 in labelMeshDatas)
                {
                    meshDat2?.Dispose();
                }
            }
            if (labelMeshRefs is not null)
            {
                foreach (MeshRef meshRef2 in labelMeshRefs)
                {
                    api.Render.DeleteMesh(meshRef2);
                }
            }
            meshRefs = null;
            labelMeshRefs = null;

            api.Event.UnregisterRenderer(this, EnumRenderStage.Ortho);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (meshRefs is null) return;

            ShaderProgramGui shader = api.Render.CurrentActiveShader as ShaderProgramGui;

            shader.RgbaIn = ColorUtil.WhiteArgbVec;
            shader.ExtraGlow = 0;
            shader.ApplyColor = 0;
            shader.NoTexture = 0f;
            shader.OverlayOpacity = 0f;
            shader.NormalShaded = 0;
            shader.Tex2d2D = itemTexturePosition.atlasTextureId;

            shader.ProjectionMatrix = api.Render.CurrentProjectionMatrix;

            /*
            shader.Uniform("rgbaIn", ColorUtil.WhiteArgbVec);
            shader.Uniform("extraGlow", 0);
            shader.Uniform("applyColor", 0);
            shader.Uniform("noTexture", 0f);
            shader.Uniform("overlayOpacity", 0f);
            shader.Uniform("normalShaded", 0);
            shader.BindTexture2D("tex2d", textureID, 0);
            shader.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
            */

            float vPosM = compassZoom;
            float scale = (api.Render.FrameHeight * (vPosM - 0.5f)) / 2;
            
            Matrixf viewMatrix = new(api.Render.CurrentModelviewMatrix);
            viewMatrix.Translate(
                api.Render.FrameWidth / 2,
                api.Render.FrameHeight * vPosM,
                10)
                .Scale(scale, scale, 0)
                .RotateZDeg(compassAngle);

            shader.ModelViewMatrix = viewMatrix.Values;
            //shader.UniformMatrix("modelViewMatrix", viewMatrix.Values);

            foreach (MeshRef meshRef in meshRefs)
            {
                if (meshRef is not null) api.Render.RenderMesh(meshRef);
            }

            shader.NoTexture = 0f;
            //shader.Uniform("noTexture", 0f);

            for (int i = 0; i < meshRefs.Length; i++)
            {
                if (labelMeshRefs[i] is not null)
                {
                    //shader.BindTexture2D("tex2d", labelTextures[i].TextureId, 0);
                    shader.Tex2d2D = labelTextures[i].TextureId;
                    api.Render.RenderMesh(labelMeshRefs[i]);
                    viewMatrix.RotateZDeg(22.5f);
                    //shader.UniformMatrix("modelViewMatrix", viewMatrix.Values);
                    shader.ModelViewMatrix = viewMatrix.Values;
                }
            }
        }
    }
}
