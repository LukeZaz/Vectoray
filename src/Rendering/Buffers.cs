/*
Vectoray; Home-brew 3D C# game engine.
Copyright (C) 2020 LukeZaz

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Runtime.InteropServices;
using Vectoray.Rendering.OpenGL;

namespace Vectoray.Rendering
{
    #region Enum declarations

    /// <summary>
    /// An enum of the various OpenGL buffer target values.
    /// </summary>
    public enum BufferTarget
    {
        ARRAY_BUFFER = 0x8892,
        ELEMENT_ARRAY_BUFFER = 0x8893,
        PIXEL_PACK_BUFFER = 0x88EB,
        PIXEL_UNPACK_BUFFER = 0x88EC,
        UNIFORM_BUFFER = 0x8A11,
        TEXTURE_BUFFER = 0x8C2A,
        TRANSFORM_FEEDBACK_BUFFER = 0x8C8E,
        COPY_READ_BUFFER = 0x8F36,
        COPY_WRITE_BUFFER = 0x8F37,
        DRAW_INDIRECT_BUFFER = 0x8F3F,
        SHADER_STORAGE_BUFFER = 0x90D2,
        DISPATCH_INDIRECT_BUFFER = 0x90EE,
        QUERY_BUFFER = 0x9192,
        ATOMIC_COUNTER_BUFFER = 0x92C0,
    }

    /// <summary>
    /// An enum of the various usage hints that can be provided to OpenGL regarding buffer object data stores.
    /// </summary>
    public enum BufferUsageHint
    {
        STREAM_DRAW = 0x88E0,
        STREAM_READ = 0x88E1,
        STREAM_COPY = 0x88E2,
        STATIC_DRAW = 0x88E4,
        STATIC_READ = 0x88E5,
        STATIC_COPY = 0x88E6,
        DYNAMIC_DRAW = 0x88E8,
        DYNAMIC_READ = 0x88E9,
        DYNAMIC_COPY = 0x88EA,
    }

    #endregion

    /// <summary>
    /// Base interface for all OpenGL buffer types.
    /// </summary>
    public interface IBuffer
    {
        void BindTo(BufferTarget target);
        void Unbind();

        void SetDataStore<T>(T[] data, BufferUsageHint usage) where T : unmanaged;
    }

    public partial class Renderer
    {
        private class GLBuffer : IBuffer
        {
            #region Variable declaration

            public readonly uint id;
            public readonly Renderer ParentRenderer;
            public BufferTarget? CurrentBinding { get; private set; } = null;

            #endregion

            #region Lifecycle functionality

            private GLBuffer(Renderer renderer, uint id) => (ParentRenderer, this.id) = (renderer, id);

            public static Result<GLBuffer, BufferException> CreateNew(Renderer renderer)
            {
                if (renderer == null)
                    return new BufferException(BufferExceptionType.RendererNull,
                        "Failed to create a buffer as the provided Renderer instance was null.").Invalid();

                // Default to using Direct State Access in all buffer operations if at all possible.
                uint[] buffer = new uint[1];
                if (renderer.contextVersion >= GLVersion.GL_4_5) renderer._glCreateBuffers(1, buffer);
                else renderer._glGenBuffers(1, buffer);
                return new GLBuffer(renderer, buffer[0]).Valid();
            }

            ~GLBuffer()
            {
                // This gets unbound first as a safety step to ensure the parent renderer doesn't have any dead buffers
                // floating around. Realistically, this won't happen, because the renderer tracking this at all
                // should prevent this from being GC'd, but I want to be sure.
                Unbind();
                // This method silently ignores invalid buffer object names, so this will be fine even if
                // the buffer was never properly initialized.
                ParentRenderer._glDeleteBuffers(1, new[] { id });
            }

            #endregion

            /// <summary>
            /// Binds this buffer to a specified OpenGL buffer target. Any other buffer currently bound will
            /// be unbound as a result of this.
            /// </summary>
            /// <param name="target">The buffer target to bind this buffer to.</param>
            public void BindTo(BufferTarget target)
            {
                // While not necessary to OpenGL itself, we explicitly
                // unbind first to make sure the other buffer object's wrapper knows it is no longer bound.
                ParentRenderer.UnbindBufferTarget(target);
                ParentRenderer.BindBuffer(target, this);
                CurrentBinding = target;
            }

            /// <summary>
            /// Unbind this buffer object from the target it is currently bound to, if any.
            /// The accuracy of the binding tracked will be verified before unbinding to ensure other buffers are not affected.
            /// </summary>
            public void Unbind()
            {
                if (CurrentBinding is BufferTarget binding)
                {
                    // Ensure that CurrentBinding is accurate for safety's sake before unbinding.
                    if (ParentRenderer.currentBufferBindings[binding] == this)
                    {
                        // ParentRenderer.UnbindBufferTarget *could* be used here, but that would cause this method
                        // to loop around once, as UnbindBufferTarget calls this one to ensure the buffer wrapper
                        // knows the buffer is unbound.
                        CurrentBinding = null;
                        ParentRenderer.currentBufferBindings[binding] = null;
                        ParentRenderer._glBindBuffer(binding, 0);
                    }
                    else Debug.LogError(
                        $"Attempted to unbind buffer object '{id}', but its binding to target `{binding}` could not be verified; the "
                        + $"parent Renderer instance reported the bound buffer as '{ParentRenderer.currentBufferBindings[binding].id}'"
                        + " instead.");
                }
                else Debug.LogWarning($"Attempted to unbind buffer object '{id}', but it seems to already be unbound "
                        + "as its wrapper's CurrentBinding property is null.");
            }

            /// <summary>
            /// Creates a new data store for the this buffer and fills it with `data`, while also
            /// providing a usage hint for this data to OpenGL. Old data stores will be deleted in this process.
            /// </summary>
            /// <param name="data">The data to use to create the new data store for the buffer.</param>
            /// <param name="usage">The usage hint to give to OpenGL for optimization purposes.</param>
            /// <typeparam name="T">The type of the data.</typeparam>
            public void SetDataStore<T>(T[] data, BufferUsageHint usage)
                where T : unmanaged
            {
                // TODO: Test the shit outta this. I'm not sure the marshaling works. It should, but still...
                // Default to using Direct State Access in all buffer operations if at all possible.
                if (ParentRenderer.contextVersion >= GLVersion.GL_4_5)
                {
                    int dataSize = Marshal.SizeOf<T>() * data.Length;
                    IntPtr dataPtr = Marshal.AllocHGlobal(dataSize);

                    for (int i = 0; i < data.Length; i++)
                        Marshal.StructureToPtr(data[i], dataPtr + (Marshal.SizeOf<T>() * i), false);
                    ParentRenderer._glNamedBufferData(id, dataSize, dataPtr, usage);
                    // Marshal.DestroyStructure is not needed as the unmanaged generic constraint
                    // prevents reference types from being passed to begin with.
                    Marshal.FreeHGlobal(dataPtr);
                }
                else if (CurrentBinding is BufferTarget binding)
                {
                    // Ensure that CurrentBinding is accurate for safety's sake before setting the data store.
                    if (ParentRenderer.currentBufferBindings[binding] != this)
                    {
                        Debug.LogError($"Could not set the data store of buffer object '{id}' as its binding to "
                            + $"buffer target `{binding}` could not be verified; parent Renderer reported bound buffer "
                            + $"as '{ParentRenderer.currentBufferBindings[binding].id}' instead.");
                        return;
                    }

                    int dataSize = Marshal.SizeOf<T>() * data.Length;
                    IntPtr dataPtr = Marshal.AllocHGlobal(dataSize);

                    for (int i = 0; i < data.Length; i++)
                        Marshal.StructureToPtr(data[i], dataPtr + (Marshal.SizeOf<T>() * i), false);
                    ParentRenderer._glBufferData(binding, dataSize, dataPtr, usage);
                    Marshal.FreeHGlobal(dataPtr);
                }
                else Debug.LogError($"Could not set the data store of buffer object '{id}' as it must be bound first "
                    + $"when using OpenGL versions prior to 4.5. (Currently using {ParentRenderer.contextVersion.AsString()})");
            }
        }

        /// <summary>
        /// Create a new OpenGL buffer using this renderer as the parent context.
        /// </summary>
        /// <returns>A wrapper representing the newly-created OpenGL buffer object.</returns>
        public Result<IBuffer, BufferException> CreateBuffer() => GLBuffer.CreateNew(this).MapValue(item => (IBuffer)item);

        /// <summary>
        /// Binds a given OpenGL buffer object to one of the various buffer targets.
        /// </summary>
        /// <param name="target">The OpenGL buffer target to bind the buffer to.</param>
        /// <param name="buffer">The OpenGL buffer object to bind.
        /// A value of zero represents no buffer and will unbind any currently bound without replacing them.</param>
        private void BindBuffer(BufferTarget target, GLBuffer buffer)
        {
            _glBindBuffer(target, buffer.id);
            ErrorCode error = GetError();
            if (error != ErrorCode.INVALID_VALUE) currentBufferBindings[target] = buffer;

            error.LogIfError(
                e => $"Encountered unexpected OpenGL error `{e}` while binding buffer '{buffer.id}'. "
                    + e switch
                    {
                        ErrorCode.INVALID_VALUE
                            => "This can be caused if the provided object ID was not one previously returned by glGenBuffers.",
                        _ => "This should be impossible; perhaps an earlier error went uncaught?",
                    });
        }

        /// <summary>
        /// Unbinds any buffer bound to the provided OpenGL buffer target.
        /// </summary>
        /// <param name="target">The buffer target to unbind any currently bound buffer from.</param>
        public void UnbindBufferTarget(BufferTarget target)
        {
            if (currentBufferBindings[target] is GLBuffer buffer)
            {
                currentBufferBindings[target] = null;
                buffer.Unbind();
            }
            else
                _glBindBuffer(target, 0);

            GetError().LogIfError(
                e => $"Encountered unexpected error `{e}` while unbinding buffer target `{target}`. "
                    + "This should be impossible; perhaps an earlier error went uncaught?"
            );
        }
    }

    #region Exception definitions

    /// <summary>
    /// Base exception type used by `IBuffer`-based classes for `Result` error types.
    /// </summary>
    public class BufferException : ExceptionEnum<BufferExceptionType>
    {
        public BufferException(BufferExceptionType type) : base(type) { }
        public BufferException(BufferExceptionType type, string message) : base(type, message) { }
        public BufferException(BufferExceptionType type, string message, Exception inner) : base(type, message, inner) { }
    }

    public enum BufferExceptionType
    {
        Default,
        RendererNull
    }

    #endregion
}