using System;
using System.Collections.Generic;
using System.Drawing;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using d3d11 = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;
using assFlags = Assimp.PostProcessSteps;

namespace Bivrost.Bivrost360Player
{

    internal class ModelAsset : IDisposable
    {

        protected d3d11.Buffer vertices;
        protected d3d11.Buffer indices;
        protected int verticesCount;
        protected int indexCount;
        protected d3d11.VertexBufferBinding vertexBufferBinding;    // TODO: This might be recreated each frame as in samples
        const int shaderChannelLength = 13; // TODO: from InputLayout


        public ModelAsset(d3d11.Device device, string objFile)
        {
            Load(device, objFile);
        }


        protected void Load(d3d11.Device device, string objFile)
        {
            Assimp.Scene scene;
            using (var ctx = new Assimp.AssimpContext())
            {
                scene = ctx.ImportFile(objFile, assFlags.MakeLeftHanded);
                Assimp.Mesh mesh = scene.Meshes[0];     // TODO: multi-material meshes

                int[] indicesList = new int[mesh.FaceCount * 3];
                for (int i = 0; i < mesh.FaceCount; i++)
                {
                    var face = mesh.Faces[i];
                    if (face.IndexCount != 3) throw new Exception("Supports only objects with 3-vertex faces");
                    for (int v = 0; v < 3; v++)
                    {
                        int vert = face.Indices[v];
                        //vert = vert / 3 * shaderChannelLength + vert % 3;    // match input layout
                        indicesList[i * 3 + v] = vert;
                    }
                }

                float[] verticesList = new float[mesh.VertexCount * shaderChannelLength];
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    var vert = mesh.Vertices[i];
                    float x = vert.X;
                    float y = vert.Y;
                    float z = vert.Z;
                    float w = 1.0f;

                    float u = 0;
                    float v = 0;
                    if (mesh.HasTextureCoords(0))
                    {
                        u = mesh.TextureCoordinateChannels[0][i].X;
                        v = mesh.TextureCoordinateChannels[0][i].Y;
                    }

                    float r = 0.5f;
                    float g = 0.8f;
                    float b = 0.3f;
                    float a = 0.5f;
                    if (mesh.VertexColorChannelCount > 0)
                    {
                        var col = mesh.VertexColorChannels[0][i];
                        r = col.R;
                        g = col.G;
                        b = col.B;
                        a = col.A;
                    }

                    float nx = 0;
                    float ny = 1;
                    float nz = 0;
                    if(mesh.HasNormals)
                    {
                        nx = mesh.Normals[i].X;
                        ny = mesh.Normals[i].Y;
                        nz = mesh.Normals[i].Z;
                    }

                    // TODO: from InputLayout
                    verticesList[i * shaderChannelLength +  0] = x;
                    verticesList[i * shaderChannelLength +  1] = y;
                    verticesList[i * shaderChannelLength +  2] = z;
                    verticesList[i * shaderChannelLength +  3] = w;
                    verticesList[i * shaderChannelLength +  4] = r;
                    verticesList[i * shaderChannelLength +  5] = g;
                    verticesList[i * shaderChannelLength +  6] = b;
                    verticesList[i * shaderChannelLength +  7] = a;
                    verticesList[i * shaderChannelLength +  8] = nx;
                    verticesList[i * shaderChannelLength +  9] = ny;
                    verticesList[i * shaderChannelLength + 10] = nz;
                    verticesList[i * shaderChannelLength + 11] = u;
                    verticesList[i * shaderChannelLength + 12] = 1-v;
                }

                // Instantiate Vertex buffer from vertex data
                vertices = d3d11.Buffer.Create(device, d3d11.BindFlags.VertexBuffer, verticesList);
                verticesCount = verticesList.Length;
                indices = d3d11.Buffer.Create(device, d3d11.BindFlags.IndexBuffer, indicesList);
                indexCount = indicesList.Length;
            }

            vertexBufferBinding = new d3d11.VertexBufferBinding(vertices, shaderChannelLength * sizeof(float), 0);
        }


        protected void Unload()
        {
            vertices?.Dispose();
            indices?.Dispose();
            vertices = null;
            indices = null;
        }


        public void Render(d3d11.DeviceContext context)
        {
            context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            context.InputAssembler.SetIndexBuffer(indices, dxgi.Format.R32_UInt, 0);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.DrawIndexed(indexCount, 0, 0);
            //context.Draw(verticesCount, 0);
        }

        public void Dispose()
        {
            Unload();
        }

    }



    internal class Object3d:IDisposable
    {
        protected MaterialAsset material;
        protected ModelAsset model;
        protected d3d11.Buffer constantBuffer;

        public Matrix World { set; get; }


        public Object3d(d3d11.Device device, ModelAsset model, MaterialAsset mat)
        {
            this.material = mat;
            this.model = model;
            constantBuffer = new d3d11.Buffer(
                device,
                Utilities.SizeOf<Matrix>(),
                d3d11.ResourceUsage.Default,
                d3d11.BindFlags.ConstantBuffer,
                d3d11.CpuAccessFlags.None,
                d3d11.ResourceOptionFlags.None,
                0
            );
        }


        public void Dispose()
        {
            constantBuffer.Dispose();
        }


        internal void Render(Matrix viewProj, d3d11.DeviceContext context)
        {
            var worldViewProj = World * viewProj;
            worldViewProj.Transpose();

            context.UpdateSubresource(ref worldViewProj, constantBuffer);
            context.VertexShader.SetConstantBuffer(0, constantBuffer);

            material.Apply(context);
            model.Render(context);
        }
    }




    internal class MaterialAsset : IDisposable
    {
        private d3d11.VertexShader vertexShader;
        private d3d11.PixelShader pixelShader;
        private d3d11.InputLayout layout;

        public MaterialAsset(d3d11.Device device, string shaderFile):this(device, System.IO.File.ReadAllBytes(shaderFile), shaderFile)
        { }


        public MaterialAsset(d3d11.Device device, byte[] shaderBytes, string shaderName = "unknown")  // TODO: additional shader flags?
        {
            // Compile Vertex and Pixel shaders
            using (var vertexShaderByteCode = ShaderBytecode.Compile(shaderBytes, "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None, shaderName))
            using (var pixelShaderByteCode = ShaderBytecode.Compile(shaderBytes, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None, shaderName))
            using (var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode))
            {
                vertexShader = new d3d11.VertexShader(device, vertexShaderByteCode);
                pixelShader = new d3d11.PixelShader(device, pixelShaderByteCode);

                //var vsRefl = new ShaderReflection(vertexShaderByteCode);
                //vsRefl.Description.

                layout = new d3d11.InputLayout(
                    device,
                    signature,
                    new[]
                    {
                        new d3d11.InputElement("POSITION", 0, dxgi.Format.R32G32B32A32_Float,  d3d11.InputElement.AppendAligned, 0),
                        new d3d11.InputElement("COLOR",    0, dxgi.Format.R32G32B32A32_Float,  d3d11.InputElement.AppendAligned, 0),
                        new d3d11.InputElement("NORMAL",   0, dxgi.Format.R32G32B32_Float,     d3d11.InputElement.AppendAligned, 0),
                        new d3d11.InputElement("TEXCOORD", 0, dxgi.Format.R32G32_Float,        d3d11.InputElement.AppendAligned, 0)
                    }
                );
            }
        }


        public void Apply(d3d11.DeviceContext context)
        {
            context.InputAssembler.InputLayout = layout;
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);
        }


        public void Dispose()
        {
            vertexShader.Dispose();
            pixelShader.Dispose();
            layout.Dispose();
        }
    }


    internal partial class SceneSharpDX4 : IScene, IContentUpdatableFromMediaEngine
    {
        private ISceneHost host;
        private MaterialAsset mat;
        ModelAsset chairModel;
        Object3d chair1;
        Object3d chair2;
        ModelAsset sphereModel;
        Object3d sphere;
        TextureAsset chairTextureDiffuse;
        TextureAsset chairTextureNormal;
        private Matrix viewProj;

        public SceneSharpDX4(Func<IContentUpdatableFromMediaEngine, bool> contentRequested)
        {
            requestContentCallback = contentRequested;
        }

        private d3d11.Device device => host.Device;

        private d3d11.DeviceContext context => device.ImmediateContext;


        void IScene.Attach(ISceneHost host)
        {
            this.host = host;
            MediaDecoder.Instance.OnContentChanged += Instance_OnContentChanged;

            mat = new MaterialAsset(device, "Renderers/MiniTri.fx");

            chairModel = new ModelAsset(device, "Renderers/office_chair.obj");
            sphereModel = new ModelAsset(device, "Renderers/sphere.obj");

            chair1 = new Object3d(device, chairModel, mat);
            chair2 = new Object3d(device, chairModel, mat);
            sphere = new Object3d(device, sphereModel, mat);

            chairTextureDiffuse = new TextureAsset(device, "Renderers/office_chair_d.png");
            chairTextureNormal = new TextureAsset(device, "Renderers/office_chair_n.png");
        }

        void IScene.Detach()
        {
            MediaDecoder.Instance.OnContentChanged -= Instance_OnContentChanged;

            chairModel.Dispose();
            chair1.Dispose();
            chair2.Dispose();
            sphereModel.Dispose();
            sphere.Dispose();
            mat.Dispose();
        }

        void IScene.Render()
        {
            if (requestContent)
            {
                if (requestContentCallback(this))
                    requestContent = false;
            }

            context.PixelShader.SetShaderResource(0, textureView);
            context.PixelShader.SetSampler(0, sampler);
            sphere.Render(viewProj, context);

            context.PixelShader.SetShaderResource(0, chairTextureDiffuse.TextureView);
            context.PixelShader.SetShaderResource(1, chairTextureNormal.TextureView);
            context.PixelShader.SetSampler(0, sampler);
            context.PixelShader.SetSampler(1, sampler);
            chair1.Render(viewProj, context);
            chair2.Render(viewProj, context);

        }

        void IScene.Update(TimeSpan timeSpan)
        {
            float time = (float)timeSpan.TotalSeconds;

            var view = Matrix.LookAtRH(Vector3.Zero, Vector3.ForwardRH, Vector3.UnitY);
            var proj = Matrix.PerspectiveFovRH(72f * (float)Math.PI / 180f, 16f / 9f, 0.01f, 100.0f);
            viewProj = Matrix.Multiply(view, proj);
            chair1.World = Matrix.RotationZ(time * 2) * Matrix.RotationX(time) * Matrix.Translation(Vector3.ForwardRH * 5) * Matrix.Translation(Vector3.Left * 2);
            chair2.World = Matrix.RotationY(time /2 ) * Matrix.Translation(Vector3.ForwardRH * 5) * Matrix.Translation(Vector3.Right * 2);
            sphere.World = Matrix.RotationY(time / 5) /** Matrix.RotationX(time / 3) */ * Matrix.Scaling(50);
        }

        #region content updates

        bool requestContent = true;

        protected Func<IContentUpdatableFromMediaEngine, bool> requestContentCallback;
        private d3d11.SamplerState sampler;
        private d3d11.ShaderResourceView textureView;

        protected void Instance_OnContentChanged()
        {
            requestContent = true;
        }

        void IContentUpdatableFromMediaEngine.ClearContent()
        {
            throw new NotImplementedException();
        }

        void IContentUpdatableFromMediaEngine.ReceiveBitmap(Bitmap bitmap, MediaDecoder.ClipCoords coordsL, MediaDecoder.ClipCoords coordsR)
        {
            throw new NotImplementedException();
        }

        d3d11.Texture2D mainTexture;
        void IContentUpdatableFromMediaEngine.ReceiveTextures(d3d11.Texture2D textureL, d3d11.Texture2D textureR)
        {
            mainTexture?.Dispose();
            using (var resource = textureL.QueryInterface<SharpDX.DXGI.Resource>())
            {
                mainTexture = device.OpenSharedResource<d3d11.Texture2D>(resource.SharedHandle);
            };

            textureView?.Dispose();
            textureView = new d3d11.ShaderResourceView(device, mainTexture);

            sampler?.Dispose();
            sampler = new d3d11.SamplerState(device, new d3d11.SamplerStateDescription()
            {
                Filter = d3d11.Filter.MinMagMipLinear,
                AddressU = d3d11.TextureAddressMode.Clamp,
                AddressV = d3d11.TextureAddressMode.Clamp,
                AddressW = d3d11.TextureAddressMode.Clamp,
                BorderColor = SharpDX.Color.Black,
                ComparisonFunction = d3d11.Comparison.Never,
                MaximumAnisotropy = 16,
                MipLodBias = 0,
                MinimumLod = -float.MaxValue,
                MaximumLod = float.MaxValue
            });
        }

        void IContentUpdatableFromMediaEngine.SetProjection(ProjectionMode projection)
        {
            ;
        }
        #endregion
    }
}