using System;
using SharpDX.Direct3D;
using d3d11 = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;
using assFlags = Assimp.PostProcessSteps;

namespace Bivrost.Bivrost360Player
{
    internal class MeshAsset : Asset
    {
        protected d3d11.Buffer vertices;
        protected d3d11.Buffer indices;
        protected int verticesCount;
        protected int indexCount;
        protected d3d11.VertexBufferBinding vertexBufferBinding;    // TODO: This might be recreated each frame as in samples
        private string objFile;
        const int shaderChannelLength = 19; // TODO: from InputLayout


        public MeshAsset(string objFile):base("Model", objFile)
        {
            this.objFile = objFile;
        }


        public override void Load(d3d11.Device device)
        {
            Assimp.Scene scene;
            using (var ctx = new Assimp.AssimpContext())
            {
                scene = ctx.ImportFile(objFile, assFlags.MakeLeftHanded | assFlags.CalculateTangentSpace);
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
                        v = 1 - mesh.TextureCoordinateChannels[0][i].Y;
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

                    float tgx = 0;
                    float tgy = 0;
                    float tgz = 0;
                    float btgx = 0;
                    float btgy = 0;
                    float btgz = 0;
                    if (mesh.HasTangentBasis)
                    {
                        tgx = mesh.Tangents[i].X;
                        tgy = mesh.Tangents[i].Y;
                        tgz = mesh.Tangents[i].Z;
                        btgx = mesh.BiTangents[i].X;
                        btgy = mesh.BiTangents[i].Y;
                        btgz = mesh.BiTangents[i].Z;
                    }

                    // TODO: from InputLayout?
                    verticesList[i * shaderChannelLength +  0] = x; // position
                    verticesList[i * shaderChannelLength +  1] = y;
                    verticesList[i * shaderChannelLength +  2] = z;
                    verticesList[i * shaderChannelLength +  3] = w;
                    verticesList[i * shaderChannelLength +  4] = r; // vertex color
                    verticesList[i * shaderChannelLength +  5] = g;
                    verticesList[i * shaderChannelLength +  6] = b;
                    verticesList[i * shaderChannelLength +  7] = a;
                    verticesList[i * shaderChannelLength +  8] = nx; // normals
                    verticesList[i * shaderChannelLength +  9] = ny;
                    verticesList[i * shaderChannelLength + 10] = nz;
                    verticesList[i * shaderChannelLength + 11] = u;  // uv
                    verticesList[i * shaderChannelLength + 12] = v;
                    verticesList[i * shaderChannelLength + 13] = tgx; // tangents
                    verticesList[i * shaderChannelLength + 14] = tgy;
                    verticesList[i * shaderChannelLength + 15] = tgz;
                    verticesList[i * shaderChannelLength + 16] = btgx; // bitangents
                    verticesList[i * shaderChannelLength + 17] = btgy;
                    verticesList[i * shaderChannelLength + 18] = btgz;
                }

                // Instantiate Vertex buffer from vertex data
                vertices = d3d11.Buffer.Create(device, d3d11.BindFlags.VertexBuffer, verticesList);
                verticesCount = verticesList.Length;
                indices = d3d11.Buffer.Create(device, d3d11.BindFlags.IndexBuffer, indicesList);
                indexCount = indicesList.Length;
            }

            vertexBufferBinding = new d3d11.VertexBufferBinding(vertices, shaderChannelLength * sizeof(float), 0);
        }


        protected override void Unload()
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
        }
        

    }
}