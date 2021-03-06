﻿using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using WindViewer.Editor.Renderer;

namespace WindViewer.Editor.Tools
{
    public sealed class DebugRenderer : IRenderer
    {
        class Instance
        {
            public Instance(Vector3 pos, Quaternion rot, Vector3 scale)
            {
                Position = pos;
                Rotation = rot;
                Scale = scale;
            }

            public Vector3 Position { get; private set; }
            public Quaternion Rotation { get; private set; }
            public Vector3 Scale { get; private set; }
        }

        private static DebugRenderer _singleton;
        private Dictionary<IRenderable, List<Instance>> _renderList;

        //Cached instance of our debug shapes.
        private Cube _cubeShape;

        public DebugRenderer()
        {
            _renderList = new Dictionary<IRenderable, List<Instance>>();

            if (_singleton != null)
                throw new Exception("Attempted to create multiple DebugRenderers!");
            _singleton = this;

            InitializeShader("shaders/vs.glsl", "shaders/fs_solid.glsl");

            _cubeShape = new Cube();
            _cubeShape.UpdateBuffers();

            //We kind of need these.
            GL.FrontFace(FrontFaceDirection.Cw);
            GL.CullFace(CullFaceMode.FrontAndBack);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
        }


        public static void DrawWireCube(Vector3 position) { DrawWireCube(position, Quaternion.Identity, Vector3.One);}
        public static void DrawWireCube(Vector3 position, Quaternion rotation) { DrawWireCube(position, rotation, Vector3.One);}
        public static void DrawWireCube(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!_singleton._renderList.ContainsKey(_singleton._cubeShape))
                _singleton._renderList.Add(_singleton._cubeShape, new List<Instance>());

            _singleton._renderList[_singleton._cubeShape].Add(new Instance(position, rotation, scale));
        }

        public override void ClearRenderableList()
        {
            _renderList.Clear();
        }

        public override void Render(Camera camera, float aspectRatio)
        {
            GL.UseProgram(_programId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); //Clear any previously bound buffer

            //Enable Attributes for this shader
            GL.EnableVertexAttribArray((int)ShaderAttributeIds.Position);

            foreach (KeyValuePair<IRenderable, List<Instance>> pair in _renderList)
            {
                //Bind the buffers of the instance we're drawing.
                pair.Key.BindBuffers();

                //Now update the vertex attribute to point to the newly bound buffer.
                GL.VertexAttribPointer((int)ShaderAttributeIds.Position, 3, VertexAttribPointerType.Float, false, 0, 0);


                //Then draw each instance of it.
                foreach (Instance instance in pair.Value)
                {
                    //Create Model matrix based on instance position instead.
                    Matrix4 worldMatrix = Matrix4.CreateScale(instance.Scale) *
                                          Matrix4.CreateFromQuaternion(instance.Rotation) *
                                          Matrix4.CreateTranslation(instance.Position);

                    //ToDo: Camera should really own the view and proj matrix.
                    Matrix4 projMatrix = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4f, aspectRatio, 0.01f, 8000f);

                    Matrix4 modelViewProjectionMatrix = worldMatrix * (camera.GetViewMatrix() * projMatrix);

                    //Upload the WVP to the GPU
                    GL.UniformMatrix4(_uniformMVP, false, ref modelViewProjectionMatrix);

                    //Finally render the object.
                    _cubeShape.Render();
                }
            }

            GL.DisableVertexAttribArray((int)ShaderAttributeIds.Position);
            GL.Flush();
        }

        public override void PreUpdate()
        {
            base.PreUpdate();
            _renderList.Clear();
        }
    }
}