using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Windows;
using SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.D3DCompiler;

namespace SharpDXGame
{
    /**
     * IDisposable inherited to ensure objecst are disposed correctly.
     */
    public class Game : IDisposable
    {
        #region Window and device properties
        // The colour of the background
        public SharpDX.Mathematics.Interop.RawColor4 BackgroundColour { get; set; } = 
            new SharpDX.Mathematics.Interop.RawColor4(0, 0.5F, 0.35F, 0);
        /* 
         * RenderForm, a subclass to Windows.Form, provides a window w/ borders, title etc.
         * but also provides a render loop optimized for 3D graphics.
         */
        private RenderForm renderForm;
        
        // Width, height of the window
        private const int Width = 1280;
        private const int Height = 720;

        private D3D11.Device device;
        private D3D11.DeviceContext deviceContext;
        private SwapChain swapChain;

        // Holds the render target view
        private D3D11.RenderTargetView renderTargetView;
        #endregion

        /**
         * Three types of buffer in DirectX; Vertex, Index, Constant
         *  The data in buffers are automatically copied from system memory to video memory by DirectX
            when the rendering requires the data
         */
        #region Buffers
        // Holds data for each vertex
        private D3D11.Buffer vertexBuffer;
        #endregion

        #region Shaders
        private D3D11.VertexShader vertexShader;
        private D3D11.PixelShader pixelShader;
        #endregion

        #region Input elements, signature, layout
        private D3D11.InputElement[] inputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement(
                "POSITION",                 // A semantic, used to match with the input signature in the shader
                0,                          // The semantic slot to use (e.g. if multiple POSITION semantics were used)
                Format.R32G32B32_Float,     // The data type of the element, in this case 3 floats as the position for vertices
                0,                          // The offset (in bytes) in the structs where the data starts
                0,                          // Identifies the input-assembler
                D3D11.InputClassification.PerVertexData,
                0),                         // Irrelevant for per-vertex data                    
            
            new D3D11.InputElement(
                "COLOR",
                0,
                Format.R32G32B32A32_Float,
                12,
                0,
                D3D11.InputClassification.PerVertexData,
                0)
        };

        // Holds the input signature to the Game class
        private ShaderSignature inputSignature;

        private D3D11.InputLayout inputLayout;
        #endregion

        #region Triangle vertices
        private float[] vertices = new float[]
        {
            // Co-ordinates     // Colour
            0, 0.5F, 0,         1, 0, 0, 1,
            0.5F, -0.5F, 0,     0, 1, 0, 1,
            -0.5F, -0.5F, 0,    0, 0, 1, 1
        };
        #endregion

        /// <summary>
        /// Sets up a window and the device resources
        /// </summary>
        public Game()
        {
            // Instantiates the window
            renderForm = new RenderForm("SharpDX Demo");
            renderForm.ClientSize = new Size(Width, Height);
            renderForm.AllowUserResizing = false;

            InitializeDeviceResources();
            InitializeShaders();
            InitializeTriangle();
        }
        
        /// <summary>
        /// Begins rendering
        /// </summary>
        public void Run()
        {
            /**
             * renderForm - the RenderForm
             * RenderCallBack - the method to be called each frame
             */
            RenderLoop.Run(renderForm, RenderCallback);
        }

        private void RenderCallback()
        {
            Draw();
        }

        private void Draw()
        {
            deviceContext.OutputMerger.SetRenderTargets(renderTargetView);
            // Sets all the elements in renderTargetView (currently the back buffer) to the value of BackgroundColour
            deviceContext.ClearRenderTargetView(renderTargetView, BackgroundColour);

            deviceContext.InputAssembler.SetVertexBuffers(
                0,                                          // The first input slot for binding
                new D3D11.VertexBufferBinding(
                    vertexBuffer,                           // The vertex buffer holding the triangle vertex data
                    28,                                     // The size in bytes of each vertex
                    0)
                );
            deviceContext.Draw(vertices.Count(), 0);

            // Swaps the back buffer with the front buffer, thus making it visible
            swapChain.Present(1, PresentFlags.None);
        }

        /// <summary>
        /// Releases all window and device -related resources
        /// </summary>
        public void Dispose()
        {
            inputLayout.Dispose();
            inputSignature.Dispose();
            vertexBuffer.Dispose();
            vertexShader.Dispose();
            pixelShader.Dispose();
            renderTargetView.Dispose();
            swapChain.Dispose();
            device.Dispose();
            deviceContext.Dispose();
            renderForm.Dispose();
        }

        private void InitializeDeviceResources()
        {
            // Create description for the back buffer
            ModeDescription backBufferDesc = new ModeDescription(
                Width, Height,
                new Rational(60, 1),    // Refresh rate in Hz. 60/1 = 60Hz
                Format.R8G8B8A8_UNorm); // Format of the back buffer; RGBA channel using 32-bit unsigned int

            // Create a descriptor for the swap chain
            SwapChainDescription swapChainDesc = new SwapChainDescription()
            {
                // To research different options for these parameters, search e.g. MSDN DXGI USAGE
                ModeDescription = backBufferDesc,                   // We provide the back buffer description
                SampleDescription = new SampleDescription(1, 0),    // Descriptor for multi-sampling; we specify one level (i.e. no multi-sampling)
                Usage = Usage.RenderTargetOutput,                   // Specifies if / how the CPU can access the back buffer; we are rendering to it, so we specify it as RenderTargetOutput
                BufferCount = 1,                                    // The number of buffers (we just need 1)
                OutputHandle = renderForm.Handle,                   // The handle to the window to render in
                IsWindowed = true                                   // Fullscreen vs windowed mode
            };

            // Create device and swap chain
            D3D11.Device.CreateWithSwapChain(
                DriverType.Hardware,            // Specifies to use the GPU
                D3D11.DeviceCreationFlags.None, // Choose not to use any special flags (flags: MSDN D3D11_CREATE_DEVICE_FLAG)
                swapChainDesc,                  // Description of the swap chain
                out device, out swapChain);     // Variables to store our swap chain and device in

            // Sets the device context to an immediate context; used to perform rendering you want immediately submitted to the device
            deviceContext = device.ImmediateContext;

            // Create a render target view from a back buffer
            using (D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0)) // Specify the type of back buffer; in this case, a Texture2D
            {
                renderTargetView = new D3D11.RenderTargetView(device, backBuffer);
            }

            // Set viewport
            deviceContext.Rasterizer.SetViewport(0, 0, Width, Height);
        }

        private void InitializeTriangle()
        {
            vertexBuffer = D3D11.Buffer.Create(
                device,                         // The D3D device to use
                D3D11.BindFlags.VertexBuffer,   // The type of buffer to create 
                vertices);                      // The initial data to load into the buffer
        }

        private void InitializeShaders()
        {
            // Compile shader code
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(
                "vertexShader.hlsl",    // File to compile
                "main",                 // Name of the entry point method in the shader code
                "vs_4_0",               // HLSL version
                ShaderFlags.Debug       // Set the compilation to debug mode
                ))
            {
                inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                vertexShader = new D3D11.VertexShader(device, vertexShaderByteCode);
            }

            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(
                "pixelShader.hlsl",    
                "main",                 
                "ps_4_0",
                ShaderFlags.Debug       
                ))
            {
                pixelShader = new D3D11.PixelShader(device, pixelShaderByteCode);
            }

            // Configure the device context to use the shaders
            deviceContext.VertexShader.Set(vertexShader);
            deviceContext.PixelShader.Set(pixelShader);

            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList; // (MSDN Primitive Topologies)

            // Configure and assign input layout
            inputLayout = new D3D11.InputLayout(device, inputSignature, inputElements);
            deviceContext.InputAssembler.InputLayout = inputLayout;
        }
    }
}
