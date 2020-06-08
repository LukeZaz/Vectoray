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
using System.Linq;
using System.Runtime.InteropServices;

using static SDL2.SDL;
using static Vectoray.Extensions;

// TODO: Split this up into several partial classes/files
namespace Vectoray.Rendering.OpenGL
{
    #region Enum declaration

    // TODO: Mark versions as supported/unsupported by the engine as a whole. Also decide on what to/to not support and why.
    // TODO: Determine what 'supporting' an OpenGL version would entail.
    // TODO: Features made available via the engine for supported versions should check the active version before running.
    /// <summary>
    /// Represents various OpenGL versions from 1.0 through 4.6.
    /// 
    /// See the [Khronos OpenGL version history page] for more detail on what each version adds.
    /// 
    /// [Khronos OpenGL version history page]: https://www.khronos.org/opengl/wiki/History_of_OpenGL
    /// </summary>
    public enum GLVersion
    {
        GL_1_0,
        GL_1_1,
        GL_1_2,
        GL_1_3,
        GL_1_4,
        GL_1_5,
        GL_2_0,
        GL_2_1,
        GL_3_0,
        GL_3_1,
        GL_3_2,
        GL_3_3,
        GL_4_0,
        GL_4_1,
        GL_4_2,
        GL_4_3,
        GL_4_4,
        GL_4_5,
        GL_4_6
    }

    /// <summary>
    /// An enum of the seven OpenGL error flags.
    /// </summary>
    public enum ErrorCode
    {
        NO_ERROR = 0,
        INVALID_ENUM = 0x0500,
        INVALID_VALUE = 0x0501,
        INVALID_OPERATION = 0x0502,
        STACK_OVERFLOW = 0x0503,
        STACK_UNDERFLOW = 0x0504,
        OUT_OF_MEMORY = 0x0505,
        INVALID_FRAMEBUFFER_OPERATION = 0x0506
    }

    /// <summary>
    /// An enum of the five OpenGL connection info values.
    /// </summary>
    public enum ConnectionInfo
    {
        VENDOR = 0x1F00,
        RENDERER = 0x1F01,
        VERSION = 0x1F02,
        EXTENSIONS = 0x1F03,
        SHADING_LANGUAGE_VERSION = 0x8B8C,
    }

    #region glGet queries

    /// <summary>
    /// An enum of the various OpenGL boolean state variables that can be queried via `glGetBooleanv`.
    /// </summary>
    public enum BooleanQuery
    {
        // Uncategorized
        TRANSFORM_FEEDBACK_PAUSED = 0x8E23,
        TRANSFORM_FEEDBACK_ACTIVE = 0x8E24,

        DEBUG_OUTPUT_SYNCHRONOUS = 0x8242,
        DEBUG_OUTPUT = 0x92E0,

        TEXTURE_CUBE_MAP_SEAMLESS = 0x884F,
        SHADER_COMPILER = 0x8DFA,

        // Framebuffers
        DEPTH_TEST = 0x0B71,
        DEPTH_WRITEMASK = 0x0B72,
        STENCIL_TEST = 0x0B90,
        COLOR_WRITEMASK = 0x0C23,
        DOUBLEBUFFER = 0x0C32,
        STEREO = 0x0C33,

        // Multisampling
        MULTISAMPLE = 0x809D,
        SAMPLE_ALPHA_TO_COVERAGE = 0x809E,
        SAMPLE_ALPHA_TO_ONE = 0x809F,
        SAMPLE_COVERAGE = 0x80A0,
        SAMPLE_COVERAGE_INVERT = 0x80AB,
        SAMPLE_MASK = 0x8E51,

        // Pixel Operations
        DITHER = 0x0BD0,
        BLEND = 0x0BE2,
        COLOR_LOGIC_OP = 0x0BF2,

        // Pixel Transfer Operations
        UNPACK_SWAP_BYTES = 0x0CF0,
        UNPACK_LSB_FIRST = 0x0CF1,
        PACK_SWAP_BYTES = 0x0D00,
        PACK_LSB_FIRST = 0x0D01,

        // Rasterization
        LINE_SMOOTH = 0x0B20,
        POLYGON_SMOOTH = 0x0B41,
        CULL_FACE = 0x0B44,
        POLYGON_OFFSET_POINT = 0x2A01,
        POLYGON_OFFSET_LINE = 0x2A02,
        POLYGON_OFFSET_FILL = 0x8037,
        PROGRAM_POINT_SIZE = 0x8642,
        RASTERIZER_DISCARD = 0x8C89,

        // Transformation State
        CLIP_DISTANCE0 = 0x3000,
        CLIP_DISTANCE1 = 0x3001,
        CLIP_DISTANCE2 = 0x3002,
        CLIP_DISTANCE3 = 0x3003,
        CLIP_DISTANCE4 = 0x3004,
        CLIP_DISTANCE5 = 0x3005,
        CLIP_DISTANCE6 = 0x3006,
        CLIP_DISTANCE7 = 0x3007,
        DEPTH_CLAMP = 0x864F,

        // Vertex Arrays
        PRIMITIVE_RESTART_FOR_PATCHES_SUPPORTED = 0x8221,
        PRIMITIVE_RESTART_FIXED_INDEX = 0x8D69,
        PRIMITIVE_RESTART = 0x8F9D,
    }

    /// <summary>
    /// An enum of the various OpenGL single-precision floating point state variables that can be queried via `glGetFloatv`.
    /// </summary>
    public enum FloatQuery
    {
        // Uncategorized
        DEPTH_CLEAR_VALUE = 0x0B73,
        COLOR_CLEAR_VALUE = 0x0C22,
        SAMPLE_COVERAGE_VALUE = 0x80AA,
        BLEND_COLOR = 0x8005,
        MAX_TEXTURE_LOD_BIAS = 0x84FD,

        // Rasterization
        POINT_SIZE = 0x0B11,
        POINT_SIZE_RANGE = 0x0B12,
        POINT_SIZE_GRANULARITY = 0x0B13,
        LINE_WIDTH = 0x0B21,
        SMOOTH_LINE_WIDTH_RANGE = 0x0B22,
        SMOOTH_LINE_WIDTH_GRANULARITY = 0x0B23,
        POLYGON_OFFSET_UNITS = 0x2A00,
        POLYGON_OFFSET_FACTOR = 0x8038,
        POINT_FADE_THRESHOLD_SIZE = 0x8128,
        ALIASED_LINE_WIDTH_RANGE = 0x846E,

        // Shader Execution
        MIN_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5B,
        MAX_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5C,

        // Tessellation Control Shaders
        PATCH_DEFAULT_INNER_LEVEL = 0x8E73,
        PATCH_DEFAULT_OUTER_LEVEL = 0x8E74,

        // Transformation State
        DEPTH_RANGE = 0x0B70,
        MAX_VIEWPORT_DIMS = 0x0D3A,
        VIEWPORT_BOUNDS_RANGE = 0x825D,
    }

    /// <summary>
    /// An enum of the various OpenGL 32-bit integer state variables that can be queried via `glGetIntegerv`.
    /// </summary>
    public enum IntegerQuery
    {
        // Uncategorized
        FRAGMENT_INTERPOLATION_OFFSET_BITS = 0x8E5D,
        MIN_MAP_BUFFER_ALIGNMENT = 0x90BC,

        // Context info
        MAJOR_VERSION = 0x821B,
        MINOR_VERSION = 0x821C,
        NUM_EXTENSIONS = 0x821D,
        CONTEXT_FLAGS = 0x821E,
        NUM_SHADING_LANGUAGE_VERSIONS = 0x82E9,

        // Buffer binding info
        VERTEX_ARRAY_BINDING = 0x85B5,
        ARRAY_BUFFER_BINDING = 0x8894,
        ELEMENT_ARRAY_BUFFER_BINDING = 0x8895,
        VERTEX_ATTRIB_ARRAY_BUFFER_BINDING = 0x889F,
        UNIFORM_BUFFER_BINDING = 0x8A28,
        TEXTURE_BUFFER_BINDING = 0x8C2A,
        TRANSFORM_FEEDBACK_BUFFER_BINDING = 0x8C8F,
        COPY_READ_BUFFER_BINDING = 0x8F36,
        COPY_WRITE_BUFFER_BINDING = 0x8F37,
        DRAW_INDIRECT_BUFFER_BINDING = 0x8F43,
        SHADER_STORAGE_BUFFER_BINDING = 0x90D3,
        QUERY_BUFFER_BINDING = 0x9193,
        ATOMIC_COUNTER_BUFFER_BINDING = 0x92C1,

        MAX_UNIFORM_BUFFER_BINDINGS = 0x8A2F,
        MAX_TRANSFORM_FEEDBACK_BUFFERS = 0x8E70,
        MAX_SHADER_STORAGE_BUFFER_BINDINGS = 0x90DD,
        MAX_ATOMIC_COUNTER_BUFFER_BINDINGS = 0x92DC,

        MAX_TRANSFORM_FEEDBACK_SEPARATE_COMPONENTS = 0x8C80,
        MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS = 0x8C8A,
        MAX_TRANSFORM_FEEDBACK_SEPARATE_ATTRIBS = 0x8C8B,

        // Debug output info
        DEBUG_NEXT_LOGGED_MESSAGE_LENGTH = 0x8243,
        DEBUG_GROUP_STACK_DEPTH = 0x826D,
        MAX_DEBUG_GROUP_STACK_DEPTH = 0x826C,
        MAX_LABEL_LENGTH = 0x82E8,
        MAX_DEBUG_MESSAGE_LENGTH = 0x9143,
        MAX_DEBUG_LOGGED_MESSAGES = 0x9144,
        DEBUG_LOGGED_MESSAGES = 0x9145,

        // Hints
        LINE_SMOOTH_HINT = 0x0C52,
        POLYGON_SMOOTH_HINT = 0x0C53,
        TEXTURE_COMPRESSION_HINT = 0x84EF,
        FRAGMENT_SHADER_DERIVATIVE_HINT = 0x8B8B,

        // Framebuffers
        READ_BUFFER = 0x0C02,
        DRAW_FRAMEBUFFER_BINDING = 0x8CA6,
        RENDERBUFFER_BINDING = 0x8CA7,
        READ_FRAMEBUFFER_BINDING = 0x8CAA,
        DEPTH_FUNC = 0x0B74,

        MAX_DRAW_BUFFERS = 0x8824,
        MAX_DUAL_SOURCE_DRAW_BUFFERS = 0x88FC,
        MAX_COLOR_ATTACHMENTS = 0x8CDF,
        MAX_SAMPLES = 0x8D57,
        MAX_COLOR_TEXTURE_SAMPLES = 0x910E,
        MAX_DEPTH_TEXTURE_SAMPLES = 0x910F,
        MAX_INTEGER_SAMPLES = 0x9110,
        MAX_FRAMEBUFFER_WIDTH = 0x9315,
        MAX_FRAMEBUFFER_HEIGHT = 0x9316,
        MAX_FRAMEBUFFER_LAYERS = 0x9317,
        MAX_FRAMEBUFFER_SAMPLES = 0x9318,

        STENCIL_BACK_FUNC = 0x8800,
        STENCIL_BACK_FAIL = 0x8801,
        STENCIL_BACK_PASS_DEPTH_FAIL = 0x8802,
        STENCIL_BACK_PASS_DEPTH_PASS = 0x8803,
        STENCIL_BACK_REF = 0x8CA3,
        STENCIL_BACK_VALUE_MASK = 0x8CA4,
        STENCIL_BACK_WRITEMASK = 0x8CA5,

        STENCIL_CLEAR_VALUE = 0x0B91,
        STENCIL_FUNC = 0x0B92,
        STENCIL_VALUE_MASK = 0x0B93,
        STENCIL_FAIL = 0x0B94,
        STENCIL_PASS_DEPTH_FAIL = 0x0B95,
        STENCIL_PASS_DEPTH_PASS = 0x0B96,
        STENCIL_REF = 0x0B97,
        STENCIL_WRITEMASK = 0x0B98,

        DRAW_BUFFER = 0x0C01,
        DRAW_BUFFER0 = 0x8825,
        DRAW_BUFFER1 = 0x8826,
        DRAW_BUFFER2 = 0x8827,
        DRAW_BUFFER3 = 0x8828,
        DRAW_BUFFER4 = 0x8829,
        DRAW_BUFFER5 = 0x882A,
        DRAW_BUFFER6 = 0x882B,
        DRAW_BUFFER7 = 0x882C,
        DRAW_BUFFER8 = 0x882D,
        DRAW_BUFFER9 = 0x882E,
        DRAW_BUFFER10 = 0x882F,
        DRAW_BUFFER11 = 0x8830,
        DRAW_BUFFER12 = 0x8831,
        DRAW_BUFFER13 = 0x8832,
        DRAW_BUFFER14 = 0x8833,
        DRAW_BUFFER15 = 0x8834,

        // Multisampling
        SAMPLE_BUFFERS = 0x80A8,
        SAMPLES = 0x80A9,
        MAX_SAMPLE_MASK_WORDS = 0x8E59,

        // Pixel Operations
        LOGIC_OP_MODE = 0x0BF0,
        BLEND_EQUATION_RGB = 0x8009,
        BLEND_DST_RGB = 0x80C8,
        BLEND_SRC_RGB = 0x80C9,
        BLEND_DST_ALPHA = 0x80CA,
        BLEND_SRC_ALPHA = 0x80CB,
        BLEND_EQUATION_ALPHA = 0x883D,

        // Pixel Transfer Operations
        PIXEL_PACK_BUFFER_BINDING = 0x88ED,
        PIXEL_UNPACK_BUFFER_BINDING = 0x88EF,

        CLAMP_READ_COLOR = 0x891C,
        IMPLEMENTATION_COLOR_READ_TYPE = 0x8B9A,
        IMPLEMENTATION_COLOR_READ_FORMAT = 0x8B9B,

        UNPACK_ROW_LENGTH = 0x0CF2,
        UNPACK_SKIP_ROWS = 0x0CF3,
        UNPACK_SKIP_PIXELS = 0x0CF4,
        UNPACK_ALIGNMENT = 0x0CF5,
        UNPACK_SKIP_IMAGES = 0x806D,
        UNPACK_IMAGE_HEIGHT = 0x806E,
        UNPACK_COMPRESSED_BLOCK_WIDTH = 0x9127,
        UNPACK_COMPRESSED_BLOCK_HEIGHT = 0x9128,
        UNPACK_COMPRESSED_BLOCK_DEPTH = 0x9129,
        UNPACK_COMPRESSED_BLOCK_SIZE = 0x912A,

        PACK_ROW_LENGTH = 0x0D02,
        PACK_SKIP_ROWS = 0x0D03,
        PACK_SKIP_PIXELS = 0x0D04,
        PACK_ALIGNMENT = 0x0D05,
        PACK_SKIP_IMAGES = 0x806B,
        PACK_IMAGE_HEIGHT = 0x806C,
        PACK_COMPRESSED_BLOCK_WIDTH = 0x912B,
        PACK_COMPRESSED_BLOCK_HEIGHT = 0x912C,
        PACK_COMPRESSED_BLOCK_DEPTH = 0x912D,
        PACK_COMPRESSED_BLOCK_SIZE = 0x912E,

        // Programs
        PROGRAM_PIPELINE_BINDING = 0x825A,
        NUM_PROGRAM_BINARY_FORMATS = 0x87FE,
        PROGRAM_BINARY_FORMATS = 0x87FF,
        CURRENT_PROGRAM = 0x8B8D,
        MIN_PROGRAM_TEXEL_OFFSET = 0x8904,
        UNIFORM_BUFFER_OFFSET_ALIGNMENT = 0x8A34,
        SHADER_BINARY_FORMATS = 0x8DF8,
        NUM_SHADER_BINARY_FORMATS = 0x8DF9,
        SHADER_STORAGE_BUFFER_OFFSET_ALIGNMENT = 0x90DF,

        MAX_UNIFORM_LOCATIONS = 0x826E,
        MAX_VERTEX_ATTRIB_RELATIVE_OFFSET = 0x82D9,
        MAX_VERTEX_ATTRIB_BINDINGS = 0x82DA,
        MAX_VERTEX_ATTRIB_STRIDE = 0x82E5,
        MAX_PROGRAM_TEXEL_OFFSET = 0x8905,
        MAX_UNIFORM_BLOCK_SIZE = 0x8A30,
        MAX_SUBROUTINES = 0x8DE7,
        MAX_SUBROUTINE_UNIFORM_LOCATIONS = 0x8DE8,
        MAX_VARYING_VECTORS = 0x8DFC,
        MAX_IMAGE_UNITS = 0x8F38,
        MAX_COMBINED_SHADER_OUTPUT_RESOURCES = 0x8F39,
        MAX_IMAGE_SAMPLES = 0x906D,
        MAX_ATOMIC_COUNTER_BUFFER_SIZE = 0x92D8,

        // Provoking Vertices
        LAYER_PROVOKING_VERTEX = 0x825E,
        VIEWPORT_INDEX_PROVOKING_VERTEX = 0x825F,
        PROVOKING_VERTEX = 0x8E4F,

        // Rasterization
        POLYGON_MODE = 0x0B40,
        CULL_FACE_MODE = 0x0B45,
        FRONT_FACE = 0x0B46,
        SUBPIXEL_BITS = 0x0D50,
        POINT_SPRITE_COORD_ORIGIN = 0x8CA0,

        // Shaders
        MAX_COMPUTE_ATOMIC_COUNTER_BUFFERS = 0x8264,
        MAX_VERTEX_ATOMIC_COUNTER_BUFFERS = 0x92CC,
        MAX_TESS_CONTROL_ATOMIC_COUNTER_BUFFERS = 0x92CD,
        MAX_TESS_EVALUATION_ATOMIC_COUNTER_BUFFERS = 0x92CE,
        MAX_GEOMETRY_ATOMIC_COUNTER_BUFFERS = 0x92CF,
        MAX_FRAGMENT_ATOMIC_COUNTER_BUFFERS = 0x92D0,
        MAX_COMBINED_ATOMIC_COUNTER_BUFFERS = 0x92D1,

        MAX_COMPUTE_ATOMIC_COUNTERS = 0x8265,
        MAX_VERTEX_ATOMIC_COUNTERS = 0x92D2,
        MAX_TESS_CONTROL_ATOMIC_COUNTERS = 0x92D3,
        MAX_TESS_EVALUATION_ATOMIC_COUNTERS = 0x92D4,
        MAX_GEOMETRY_ATOMIC_COUNTERS = 0x92D5,
        MAX_FRAGMENT_ATOMIC_COUNTERS = 0x92D6,
        MAX_COMBINED_ATOMIC_COUNTERS = 0x92D7,

        MAX_COMBINED_COMPUTE_UNIFORM_COMPONENTS = 0x8266,
        MAX_COMBINED_VERTEX_UNIFORM_COMPONENTS = 0x8A31,
        MAX_COMBINED_GEOMETRY_UNIFORM_COMPONENTS = 0x8A32,
        MAX_COMBINED_FRAGMENT_UNIFORM_COMPONENTS = 0x8A33,
        MAX_COMBINED_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E1E,
        MAX_COMBINED_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E1F,

        MAX_VERTEX_IMAGE_UNIFORMS = 0x90CA,
        MAX_TESS_CONTROL_IMAGE_UNIFORMS = 0x90CB,
        MAX_TESS_EVALUATION_IMAGE_UNIFORMS = 0x90CC,
        MAX_GEOMETRY_IMAGE_UNIFORMS = 0x90CD,
        MAX_FRAGMENT_IMAGE_UNIFORMS = 0x90CE,
        MAX_COMBINED_IMAGE_UNIFORMS = 0x90CF,
        MAX_COMPUTE_IMAGE_UNIFORMS = 0x91BD,

        MAX_VERTEX_SHADER_STORAGE_BLOCKS = 0x90D6,
        MAX_GEOMETRY_SHADER_STORAGE_BLOCKS = 0x90D7,
        MAX_TESS_CONTROL_SHADER_STORAGE_BLOCKS = 0x90D8,
        MAX_TESS_EVALUATION_SHADER_STORAGE_BLOCKS = 0x90D9,
        MAX_FRAGMENT_SHADER_STORAGE_BLOCKS = 0x90DA,
        MAX_COMPUTE_SHADER_STORAGE_BLOCKS = 0x90DB,
        MAX_COMBINED_SHADER_STORAGE_BLOCKS = 0x90DC,

        MAX_COMPUTE_UNIFORM_COMPONENTS = 0x8263,
        MAX_FRAGMENT_UNIFORM_COMPONENTS = 0x8B49,
        MAX_VERTEX_UNIFORM_COMPONENTS = 0x8B4A,
        MAX_GEOMETRY_UNIFORM_COMPONENTS = 0x8DDF,
        MAX_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E7F,
        MAX_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E80,

        MAX_TEXTURE_IMAGE_UNITS = 0x8872,
        MAX_VERTEX_TEXTURE_IMAGE_UNITS = 0x8B4C,
        MAX_COMBINED_TEXTURE_IMAGE_UNITS = 0x8B4D,
        MAX_GEOMETRY_TEXTURE_IMAGE_UNITS = 0x8C29,
        MAX_TESS_CONTROL_TEXTURE_IMAGE_UNITS = 0x8E81,
        MAX_TESS_EVALUATION_TEXTURE_IMAGE_UNITS = 0x8E82,
        MAX_COMPUTE_TEXTURE_IMAGE_UNITS = 0x91BC,

        MAX_VERTEX_UNIFORM_BLOCKS = 0x8A2B,
        MAX_GEOMETRY_UNIFORM_BLOCKS = 0x8A2C,
        MAX_FRAGMENT_UNIFORM_BLOCKS = 0x8A2D,
        MAX_COMBINED_UNIFORM_BLOCKS = 0x8A2E,
        MAX_TESS_CONTROL_UNIFORM_BLOCKS = 0x8E89,
        MAX_TESS_EVALUATION_UNIFORM_BLOCKS = 0x8E8A,
        MAX_COMPUTE_UNIFORM_BLOCKS = 0x91BB,

        // Compute Shaders
        MAX_COMPUTE_SHARED_MEMORY_SIZE = 0x8262,
        MAX_COMPUTE_WORK_GROUP_INVOCATIONS = 0x90EB,
        DISPATCH_INDIRECT_BUFFER_BINDING = 0x90EF,

        // Fragment Shaders
        MAX_FRAGMENT_UNIFORM_VECTORS = 0x8DFD,
        MIN_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5E,
        MAX_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5F,
        MAX_FRAGMENT_INPUT_COMPONENTS = 0x9125,

        // Geometry Shaders
        MAX_GEOMETRY_OUTPUT_VERTICES = 0x8DE0,
        MAX_GEOMETRY_TOTAL_OUTPUT_COMPONENTS = 0x8DE1,
        MAX_GEOMETRY_SHADER_INVOCATIONS = 0x8E5A,
        MAX_VERTEX_STREAMS = 0x8E71,
        MAX_GEOMETRY_INPUT_COMPONENTS = 0x9123,
        MAX_GEOMETRY_OUTPUT_COMPONENTS = 0x9124,

        // Tessellation Control Shaders
        MAX_TESS_CONTROL_INPUT_COMPONENTS = 0x886C,
        PATCH_VERTICES = 0x8E72,
        MAX_PATCH_VERTICES = 0x8E7D,
        MAX_TESS_GEN_LEVEL = 0x8E7E,
        MAX_TESS_CONTROL_OUTPUT_COMPONENTS = 0x8E83,
        MAX_TESS_PATCH_COMPONENTS = 0x8E84,
        MAX_TESS_CONTROL_TOTAL_OUTPUT_COMPONENTS = 0x8E85,

        // Tessellation Evaluation Shaders
        MAX_TESS_EVALUATION_INPUT_COMPONENTS = 0x886D,
        MAX_TESS_EVALUATION_OUTPUT_COMPONENTS = 0x8E86,

        // Vertex Shaders
        MAX_VERTEX_ATTRIBS = 0x8869,
        MAX_VERTEX_UNIFORM_VECTORS = 0x8DFB,
        MAX_VERTEX_OUTPUT_COMPONENTS = 0x9122,

        // Textures
        MAX_TEXTURE_SIZE = 0x0D33,
        MAX_3D_TEXTURE_SIZE = 0x8073,
        ACTIVE_TEXTURE = 0x84E0,
        MAX_RENDERBUFFER_SIZE = 0x84E8,
        MAX_RECTANGLE_TEXTURE_SIZE = 0x84F8,
        MAX_CUBE_MAP_TEXTURE_SIZE = 0x851C,
        NUM_COMPRESSED_TEXTURE_FORMATS = 0x86A2,
        COMPRESSED_TEXTURE_FORMATS = 0x86A3,
        MAX_ARRAY_TEXTURE_LAYERS = 0x88FF,
        SAMPLER_BINDING = 0x8919,
        AX_TEXTURE_BUFFER_SIZE = 0x8C2B,
        TEXTURE_BUFFER_OFFSET_ALIGNMENT = 0x919F,

        TEXTURE_BINDING_1D = 0x8068,
        TEXTURE_BINDING_2D = 0x8069,
        TEXTURE_BINDING_3D = 0x806A,
        TEXTURE_BINDING_RECTANGLE = 0x84F6,
        TEXTURE_BINDING_CUBE_MAP = 0x8514,
        TEXTURE_BINDING_1D_ARRAY = 0x8C1C,
        TEXTURE_BINDING_2D_ARRAY = 0x8C1D,
        TEXTURE_BINDING_BUFFER = 0x8C2C,
        TEXTURE_BINDING_2D_MULTISAMPLE = 0x9104,
        TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY = 0x9105,

        // Transformation State
        VIEWPORT = 0x0BA2,
        MAX_CLIP_DISTANCES = 0x0D32,
        MAX_VIEWPORTS = 0x825B,
        VIEWPORT_SUBPIXEL_BITS = 0x825C,
        TRANSFORM_FEEDBACK_BINDING = 0x8E25,

        // Vertex Arrays
        MAX_ELEMENTS_VERTICES = 0x80E8,
        MAX_ELEMENTS_INDICES = 0x80E9,
        PRIMITIVE_RESTART_INDEX = 0x8F9E,
    }

    /// <summary>
    /// An enum of the various OpenGL 64-bit integer state variables that can be queried via `glGetInteger64v`.
    /// </summary>
    public enum IntegerQuery64
    {
        // Uncategorized
        TIMESTAMP = 0x8E28,
        MAX_ELEMENT_INDEX = 0x8D6B,
        MAX_SHADER_STORAGE_BLOCK_SIZE = 0x90DE,
        MAX_SERVER_WAIT_TIMEOUT = 0x9111,
    }

    /// <summary>
    /// An enum of the various indexed OpenGL boolean state variables that can be queried via `glGetBooleani_v`.
    /// </summary>
    public enum IndexedBooleanQuery
    {
        SCISSOR_TEST = 0x0C11,
        COLOR_WRITEMASK = 0x0C23,
        IMAGE_BINDING_LAYERED = 0x8F3C,
        BLEND = 0x0BE2,
    }

    /// <summary>
    /// An enum of the various indexed OpenGL single-precision
    /// floating point state variables that can be queried via `glGetFloati_v`.
    /// </summary>
    public enum IndexedFloatQuery
    {
        SCISSOR_BOX = 0x0C10,
        BLEND_COLOR = 0x8005,
    }

    /// <summary>
    /// An enum of the various indexed OpenGL 32-bit integer state variables that can be queried via `glGetIntegeri_v`.
    /// </summary>
    public enum IndexedIntegerQuery
    {
        // Uncategorized
        VIEWPORT = 0x0BA2,
        VERTEX_BINDING_DIVISOR = 0x82D6,
        VERTEX_BINDING_STRIDE = 0x82D8,
        SAMPLE_MASK_VALUE = 0x8E52,

        // Buffer binding info
        UNIFORM_BUFFER_BINDING = 0x8A28,
        TRANSFORM_FEEDBACK_BUFFER_BINDING = 0x8C8F,
        SHADER_STORAGE_BUFFER_BINDING = 0x90D3,
        ATOMIC_COUNTER_BUFFER_BINDING = 0x92C1,

        UNIFORM_BUFFER_START = 0x8A29,
        TRANSFORM_FEEDBACK_BUFFER_START = 0x8C84,
        SHADER_STORAGE_BUFFER_START = 0x90D4,
        ATOMIC_COUNTER_BUFFER_START = 0x92C2,

        UNIFORM_BUFFER_SIZE = 0x8A2A,
        TRANSFORM_FEEDBACK_BUFFER_SIZE = 0x8C85,
        SHADER_STORAGE_BUFFER_SIZE = 0x90D5,
        ATOMIC_COUNTER_BUFFER_SIZE = 0x92C3,

        // Image State
        IMAGE_BINDING_NAME = 0x8F3A,
        IMAGE_BINDING_LEVEL = 0x8F3B,
        IMAGE_BINDING_LAYER = 0x8F3D,
        IMAGE_BINDING_ACCESS = 0x8F3E,
        IMAGE_BINDING_FORMAT = 0x906E,

        // Pixel Operations
        BLEND_EQUATION_RGB = 0x8009,
        BLEND_DST_RGB = 0x80C8,
        BLEND_SRC_RGB = 0x80C9,
        BLEND_DST_ALPHA = 0x80CA,
        BLEND_SRC_ALPHA = 0x80CB,
        BLEND_EQUATION_ALPHA = 0x883D,

        // Compute Shaders
        MAX_COMPUTE_WORK_GROUP_COUNT = 0x91BE,
        MAX_COMPUTE_WORK_GROUP_SIZE = 0x91BF,
    }

    /// <summary>
    /// An enum of the various indexed OpenGL 64-bit integer state variables that can be queried via `glGetInteger64i_v`.
    /// </summary>
    public enum IndexedIntegerQuery64
    {
        VERTEX_BINDING_OFFSET = 0x82D7,
    }

    #endregion

    /// <summary>
    /// An enum of the various data types accepted for vertex attributes in OpenGL. Note that
    /// this enum does *not* include types for which there is no C# equivalent, such as GLfixed or GLhalf.
    /// </summary>
    public enum VertexDataType
    {
        BYTE = 0x1400,
        UNSIGNED_BYTE = 0x1401,
        SHORT = 0x1402,
        UNSIGNED_SHORT = 0x1403,
        INT = 0x1404,
        UNSIGNED_INT = 0x1405,
        FLOAT = 0x1406,
        DOUBLE = 0x140A,
    }

    /// <summary>
    /// An enum of the three OpenGL buffer bit flags.
    /// </summary>
    public enum ClearMask
    {
        DEPTH_BUFFER_BIT = 0x00000100,
        STENCIL_BUFFER_BIT = 0x00000400,
        COLOR_BUFFER_BIT = 0x00004000
    }

    /// <summary>
    /// An enum of the various OpenGL rendering modes usable with `glDrawArrays`.
    /// </summary>
    public enum DrawMode
    {
        POINTS = 0x0000,
        LINES = 0x0001,
        LINE_LOOP = 0x0002,
        LINE_STRIP = 0x0003,
        TRIANGLES = 0x0004,
        TRIANGLE_STRIP = 0x0005,
        TRIANGLE_FAN = 0x0006,
        LINES_ADJACENCY = 0x000A,
        LINE_STRIP_ADJACENCY = 0x000B,
        TRIANGLES_ADJACENCY = 0x000C,
        TRIANGLE_STRIP_ADJACENCY = 0x000D,
    }

    #endregion

    /// <summary>
    /// General global-use OpenGL methods and functionality.
    /// If you're looking for functionality to do with instances of OpenGL contexts,
    /// create an instance of the Renderer class and use that instead.
    /// </summary>
    public static class GL
    {
        #region Variable & property declaration

        /// <summary>
        /// A constant to represent OpenGL's GL_FALSE.
        /// </summary>
        public const int FALSE = 0;
        /// <summary>
        /// A constant to represent OpenGL's GL_TRUE.
        /// </summary>
        public const int TRUE = 1;

        /// <summary>
        /// Whether or not the OpenGL configuration attributes have been set before.
        /// 
        /// In this version of Vectoray, changing these attributes is not currently possible.
        /// While this may change, doing so will likely only affect newly-created
        /// OpenGL-compatible windows. See: https://wiki.libsdl.org/SDL_GLattr#OpenGL
        /// </summary>
        public static bool ConfigAttributesSet { get; private set; } = false;

        #endregion

        // TODO: Unit testing

        // TODO: Running this > 1x is probably already functional, but it should be tested before being allowed.
        /// <summary>
        /// Sets various global OpenGL attributes that need to be set before
        /// initial window creation in order to be properly applied.
        /// 
        /// **This can only be done once,** as the current version of Vectoray does not yet allow them to change.
        /// This is likely to be fixed later, however some attributes will still require jumping through
        /// some extra hoops to change (namely the OpenGL profile mask).
        /// </summary>
        /// <param name="version">The OpenGL version to use. Must be set before any OpenGL windows can be made.</param>
        /// <param name="profileMask">The OpenGL profile to use. Must be set before any OpenGL windows can be made.</param>
        /// <param name="attributes">An array of various other OpenGL attributes to be set.</param>
        public static void SetConfigAttributes(
            GLVersion version,
            SDL_GLprofile profileMask,
            params (SDL_GLattr attrib, int value)[] attributes)
        {
            if (ConfigAttributesSet)
            {
                Debug.LogError("Cannot set global OpenGL configuration attributes when they have already been set.");
                return;
            }

            // Vital attributes are set first in case they fail.
            SetConfigAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, version.GetMajor());
            SetConfigAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, version.GetMinor());
            SetConfigAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)profileMask);

            foreach ((SDL_GLattr attrib, int value) in attributes)
                SetConfigAttribute(attrib, value);

            ConfigAttributesSet = true;

            static void SetConfigAttribute(SDL_GLattr attribute, int value)
            {
                if (SDL_GL_SetAttribute(attribute, value) != 0)
                {
                    // Since OpenGL's version and profile mask are required in order to properly create
                    // GL contexts via SDL, failing to set these should be considered huge errors that break the program.
                    switch (attribute)
                    {
                        case SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION:
                        case SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION:
                        case SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK:
                            throw new VitalAttributeNotSetException(
                                $"Failed to set vital OpenGL configuration attribute '{GetAttrName(attribute)}'."
                              + $" SDL error: {SDL_GetError()}");
                        default:
                            Debug.LogError(
                                $"Failed to set non-vital OpenGL configuration attribute '{GetAttrName(attribute)}'."
                              + $" SDL error: {SDL_GetError()}"
                            );
                            return;
                    }
                }

                // Check if the attribute has the expected value, then warn the user if it does not.
                // As detailed by the warning message, this is in most cases not a problem. When setting
                // OpenGL attributes, some are treated as 'minimum requests'; e.g., you may ask for a 16-bit depth buffer,
                // but get a 24-bit one. Anything less than what you asked for *usually* causes a failure
                // during context creation.
                // See: https://wiki.libsdl.org/SDL_GLattr#OpenGL
                if (SDL_GL_GetAttribute(attribute, out int attribVal) != 0)
                {
                    Debug.LogError(
                        $"Failed to retrieve the value of OpenGL configuration attribute '{GetAttrName(attribute)}'"
                      + $" after setting it. SDL error: {SDL_GetError()}"
                    );
                    return;
                }

                if (attribVal != value)
                {
                    Debug.LogWarning(
                        $"OpenGL configuration attribute '{GetAttrName(attribute)}' was set to {value},"
                      + $" but then found to instead have a value of {attribVal}."
                      + "\nThis is not usually a problem, but you should keep this in mind when using this attribute."
                    );
                }

                static string GetAttrName(SDL_GLattr attrib) =>
                    Enum.GetName(typeof(SDL_GLattr), attrib);
            }
        }

        /// <summary>
        /// Gets the major version number of this OpenGL version enum.
        /// </summary>
        /// <param name="version">The version to retrieve the major version number of.</param>
        /// <returns>The major version number of this OpenGL version enum.</returns>
        public static int GetMajor(this GLVersion version)
        {
            return (int)version switch
            {
                int v when v.IsWithin(0..6) => 1,
                int v when v.IsWithin(6..8) => 2,
                int v when v.IsWithin(8..12) => 3,
                int v when v.IsWithin(12..19) => 4,
                _ => throw new InvalidOperationException("Cannot retrieve version info;"
                    + " OpenGL version is unrecognized or invalid.")
            };
        }

        /// <summary>
        /// Gets the minor version number of this OpenGL version enum.
        /// </summary>
        /// <param name="version">The version to retrieve the minor version number of.</param>
        /// <returns>The minor version number of this OpenGL version enum.</returns>
        public static int GetMinor(this GLVersion version)
        {
            return (int)version switch
            {
                int v when new[] { 0, 6, 8, 12 }.Contains(v) => 0,
                int v when new[] { 1, 7, 9, 13 }.Contains(v) => 1,
                int v when new[] { 2, 10, 14 }.Contains(v) => 2,
                int v when new[] { 3, 11, 15 }.Contains(v) => 3,
                int v when new[] { 4, 16 }.Contains(v) => 4,
                int v when new[] { 5, 17 }.Contains(v) => 5,
                18 => 6,
                _ => throw new InvalidOperationException("Cannot retrieve version info;"
                    + " OpenGL version is unrecognized or invalid.")
            };
        }

        /// <summary>
        /// Get the string representation of this OpenGL version constant.
        /// </summary>
        /// <param name="version">The version constant to get the string representation of.</param>
        /// <returns>The string representation of the provided OpenGL version constant.</returns>
        public static string AsString(this GLVersion version)
            => $"{version.GetMajor()}.{version.GetMinor()}";
    }

    #region Exception declaration

    /// <summary>
    /// An exception thrown whenever setting the value of a vital OpenGL configuration attribute has failed.
    /// </summary>
    public class VitalAttributeNotSetException : Exception
    {
        public VitalAttributeNotSetException() : base() { }
        public VitalAttributeNotSetException(string message) : base(message) { }
        public VitalAttributeNotSetException(string message, Exception inner) : base(message, inner) { }
    }

    #endregion
}