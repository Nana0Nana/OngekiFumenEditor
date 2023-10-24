﻿//#define OGL_LOG
using OngekiFumenEditor.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OngekiFumenEditor.Kernel.Graphics
{
    [Export(typeof(IDrawingManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DefaultDrawingManager : IDrawingManager
    {
        TaskCompletionSource initTaskSource = new TaskCompletionSource();
        bool startedInit = false;

        public Task CheckOrInitGraphics()
        {
            if (!startedInit)
            {
                startedInit = true;
                Dispatcher.CurrentDispatcher.InvokeAsync(OnInitOpenGL);
            }

            return initTaskSource.Task;
        }

        private void OnInitOpenGL()
        {
            if (Properties.ProgramSetting.Default.OutputGraphicsLog)
            {
                GL.DebugMessageCallback(OnOpenGLDebugLog, IntPtr.Zero);
                GL.Enable(EnableCap.DebugOutput);
                if (Properties.ProgramSetting.Default.GraphicsLogSynchronous)
                    GL.Enable(EnableCap.DebugOutputSynchronous);
            }

            GL.ClearColor(System.Drawing.Color.Black);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            Log.LogInfo($"Prepare OpenGL version : {GL.GetInteger(GetPName.MajorVersion)}.{GL.GetInteger(GetPName.MinorVersion)}");

            initTaskSource.SetResult();
        }

        private static void OnOpenGLDebugLog(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            if (id == 131185)
                return;

            var str = Marshal.PtrToStringAnsi(message, length);
            Log.LogDebug($"[{source}.{type}]{id}:  {str}");
            if (str.Contains("error") || type == DebugType.DebugTypeError)
                throw new Exception($"OnOpenGLDebugLog()检测到一个错误: {str}");
        }

        public Task WaitForGraphicsInitializationDone(CancellationToken cancellation)
        {
            return initTaskSource.Task;
        }

        public Task CreateGraphicsContext(GLWpfControl glView, CancellationToken cancellation = default)
        {
            var isCompatability = Properties.ProgramSetting.Default.GraphicsCompatability;
            var isOutputLog = Properties.ProgramSetting.Default.OutputGraphicsLog;

            var flag = isOutputLog ? ContextFlags.Debug : ContextFlags.Default;

            var setting = isCompatability ? new GLWpfControlSettings()
            {
                MajorVersion = 3,
                MinorVersion = 3,
                GraphicsContextFlags = flag | ContextFlags.ForwardCompatible,
                GraphicsProfile = ContextProfile.Compatability
            } : new GLWpfControlSettings()
            {
                MajorVersion = 4,
                MinorVersion = 5,
                GraphicsContextFlags = flag,
                GraphicsProfile = ContextProfile.Core
            };

            Log.LogDebug($"GraphicsCompatability: {isCompatability}");
            Log.LogDebug($"OutputGraphicsLog: {isOutputLog}");

            Log.LogDebug($"GLWpfControlSettings.Version: {setting.MajorVersion}.{setting.MinorVersion}");
            Log.LogDebug($"GLWpfControlSettings.GraphicsContextFlags: {setting.GraphicsContextFlags}");
            Log.LogDebug($"GLWpfControlSettings.GraphicsProfile: {setting.GraphicsProfile}");

            glView.Start(setting);

            return Task.CompletedTask;
        }
    }
}
