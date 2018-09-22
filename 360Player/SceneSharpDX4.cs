using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using d3d11 = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;

namespace Bivrost.Bivrost360Player
{

    internal class Model3d : IDisposable
    {

        protected d3d11.Buffer vertices;
        protected d3d11.Buffer indices;
        protected int verticesCount;
        protected d3d11.VertexBufferBinding vertexBufferBinding;
        const int shaderChannelLength = 13;


        public Model3d(d3d11.Device device, string objFile)
        {
            Load(device, objFile);
        }


        protected void Load(d3d11.Device device, string objFile)
        {
            Assimp.Scene scene;
            using (var ctx = new Assimp.AssimpContext())
            {
                scene = ctx.ImportFile(objFile, Assimp.PostProcessSteps.MakeLeftHanded);
                Assimp.Mesh mesh = scene.Meshes[0];

                float[] indicesList = new float[mesh.FaceCount * 3];
                for (int i = 0; i < mesh.FaceCount; i++)
                {
                    var face = mesh.Faces[i];
                    if (face.IndexCount != 3) throw new FormatException("Supports only objects with 3-vertex faces");
                    indicesList[i * 3] = face.Indices[0];
                    indicesList[i * 3 + 1] = face.Indices[1];
                    indicesList[i * 3 + 2] = face.Indices[2];
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
                    float ny = 0;
                    float nz = 0;
                    if(mesh.HasNormals)
                    {
                        nx = mesh.Normals[i].X;
                        ny = mesh.Normals[i].Y;
                        nz = mesh.Normals[i].Z;
                    }

                    verticesList[i * 13 +  0] = x;
                    verticesList[i * 13 +  1] = y;
                    verticesList[i * 13 +  2] = z;
                    verticesList[i * 13 +  3] = w;
                    verticesList[i * 13 +  4] = r;
                    verticesList[i * 13 +  5] = g;
                    verticesList[i * 13 +  6] = b;
                    verticesList[i * 13 +  7] = a;
                    verticesList[i * 13 +  8] = nx;
                    verticesList[i * 13 +  9] = ny;
                    verticesList[i * 13 + 10] = nz;
                    verticesList[i * 13 + 11] = u;
                    verticesList[i * 13 + 12] = v;
                }

                // Instantiate Vertex buffer from vertex data
                vertices = d3d11.Buffer.Create(device, d3d11.BindFlags.VertexBuffer, verticesList);
                verticesCount = verticesList.Length;
                indices = d3d11.Buffer.Create(device, d3d11.BindFlags.IndexBuffer, indicesList);
            }

            vertexBufferBinding = new d3d11.VertexBufferBinding(vertices, 13 * sizeof(float), 0);
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
            // Prepare All the stages
            context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            context.InputAssembler.SetIndexBuffer(indices, dxgi.Format.R32_UInt, 0);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Draw(verticesCount, 0);
        }

        public void Dispose()
        {
            Unload();
        }

    }



    internal class Object3d:IDisposable
    {
        protected Material material;
        protected Model3d model;
        protected d3d11.Buffer constantBuffer;

        public Matrix World { set; get; }


        public Object3d(d3d11.Device device, Model3d model, Material mat)
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



    internal class Material : IDisposable
    {
        private d3d11.VertexShader vertexShader;
        private d3d11.PixelShader pixelShader;
        private d3d11.InputLayout layout;

        public Material(d3d11.Device device, string shaderFile)  // TODO: additional shader flags?
        {
            // Compile Vertex and Pixel shaders
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shaderFile, "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None))
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shaderFile, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None))
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


    internal class SceneSharpDX4 : IScene
    {
        private ISceneHost host;
        private Material mat;
        Model3d chairModel;
        Object3d chair1;
        Object3d chair2;
        Model3d teapotModel;
        Object3d teapot;
        private Matrix viewProj;

        private d3d11.Device device => host.Device;

        private d3d11.DeviceContext context => device.ImmediateContext;


        void IScene.Attach(ISceneHost host)
        {
            this.host = host;

            mat = new Material(device, "MiniTri.fx");

            chairModel = new Model3d(device, "office_chair.obj");
            teapotModel = new Model3d(device, "teapot.obj");

            chair1 = new Object3d(device, chairModel, mat);
            chair2 = new Object3d(device, chairModel, mat);
            teapot = new Object3d(device, teapotModel, mat);
        }

        void IScene.Detach()
        {
            chairModel.Dispose();
            chair1.Dispose();
            chair2.Dispose();
            teapotModel.Dispose();
            teapot.Dispose();
            mat.Dispose();
        }

        void IScene.Render()
        {
            chair1.Render(viewProj, context);
            chair2.Render(viewProj, context);
            teapot.Render(viewProj, context);
        }

        void IScene.Update(TimeSpan timeSpan)
        {
            float time = (float)timeSpan.TotalSeconds;

            var view = Matrix.LookAtRH(Vector3.Zero, -Vector3.ForwardRH, Vector3.UnitY);
            var proj = Matrix.PerspectiveFovRH(72f * (float)Math.PI / 180f, 16f / 9f, 0.0001f, 50.0f);
            viewProj = Matrix.Multiply(view, proj);
            chair1.World = Matrix.RotationZ(time * 2) * Matrix.RotationX(time) * Matrix.Translation(Vector3.BackwardRH * 5) * Matrix.Translation(Vector3.Left);
            chair2.World = Matrix.RotationZ(time * 2) * Matrix.RotationX(time) * Matrix.Translation(Vector3.BackwardRH * 5) * Matrix.Translation(Vector3.Right);
            teapot.World = Matrix.RotationZ(time * 2) * Matrix.RotationX(time) * Matrix.Translation(Vector3.BackwardRH * 5) * Matrix.Translation(Vector3.Up);
        }
    }
}