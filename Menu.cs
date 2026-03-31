using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing;
using ClickableTransparentOverlay;

namespace RecoilOverlay
{
    public class Menu : Overlay
    {
        [DllImport("user32.dll")]
        public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

        public bool IsVisible = true;
        private bool _streamProof = false;

        // --- Dragging & Smoothing ---
        private Vector2 _currentPos = new Vector2(100, 100);
        private Vector2 _targetPos = new Vector2(100, 100);
        private Vector2 _dragOffset = new Vector2(0, 0);
        private bool _isDragging = false;
        private float _smoothSpeed = 0.15f;

        // --- Tab Transition Variables ---
        private string _activeTab = "Main";
        private float _tabAlpha = 0.0f;
        private float _fadeSpeed = 5.0f; // Increase for faster transitions

        // Colors
        private Vector4 _purpleAccent = new Vector4(0.53f, 0.16f, 0.94f, 1.00f);
        private Vector4 _darkBg = new Vector4(0.06f, 0.06f, 0.06f, 0.94f);

        public new async Task Run()
        {
            await Start();
            Size = new Size(3840, 2160);
        }

        protected override void Render()
        {
            if (!IsVisible) return;

            // Smooth Movement Logic
            if (Vector2.Distance(_currentPos, _targetPos) > 0.1f)
            {
                _currentPos = Vector2.Lerp(_currentPos, _targetPos, _smoothSpeed);
            }

            // --- Tab Fade Logic ---
            if (_tabAlpha < 1.0f)
            {
                _tabAlpha += ImGui.GetIO().DeltaTime * _fadeSpeed;
                if (_tabAlpha > 1.0f) _tabAlpha = 1.0f;
            }

            var style = ImGui.GetStyle();
            var colors = style.Colors;

            style.WindowRounding = 0.0f;
            style.FrameRounding = 2.0f;
            colors[(int)ImGuiCol.WindowBg] = _darkBg;
            colors[(int)ImGuiCol.CheckMark] = _purpleAccent;
            colors[(int)ImGuiCol.SliderGrab] = _purpleAccent;
            colors[(int)ImGuiCol.Header] = new Vector4(0.10f, 0.10f, 0.10f, 1.00f);
            colors[(int)ImGuiCol.HeaderHovered] = _purpleAccent * 0.8f;
            colors[(int)ImGuiCol.HeaderActive] = _purpleAccent;
            colors[(int)ImGuiCol.ButtonHovered] = _purpleAccent;

            ImGui.SetNextWindowPos(_currentPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(300, 380), ImGuiCond.Always);

            ImGui.Begin("Main", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove);

            // Dragging Logic
            if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _isDragging = true;
                _dragOffset = ImGui.GetMousePos() - _currentPos;
            }
            if (_isDragging)
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left)) _targetPos = ImGui.GetMousePos() - _dragOffset;
                else _isDragging = false;
            }

            if (ImGui.BeginTabBar("Tabs"))
            {
                // --- Main Tab ---
                if (ImGui.BeginTabItem("Main"))
                {
                    if (_activeTab != "Main") { _activeTab = "Main"; _tabAlpha = 0.0f; } // Reset alpha on switch

                    ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _tabAlpha); // Apply fade

                    ImGui.TextColored(Program.Settings.Enabled ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1),
                        Program.Settings.Enabled ? "STATUS: ACTIVE" : "STATUS: INACTIVE");
                    ImGui.TextDisabled("Toggle: [ ; ] | Hide: [ Alt ]");
                    ImGui.Separator();
                    if (ImGui.Checkbox("Stream Proof", ref _streamProof))
                    {
                        SetWindowDisplayAffinity(Process.GetCurrentProcess().MainWindowHandle, _streamProof ? 0x00000011u : 0x00000000u);
                    }
                    ImGui.Separator();
                    ImGui.SliderInt("Vertical", ref Program.Settings.Vertical, 0, 50);
                    ImGui.SliderInt("Horizontal", ref Program.Settings.Horizontal, -20, 20);
                    ImGui.SliderInt("Jitter", ref Program.Settings.Jitter, 0, 10);
                    ImGui.SliderInt("Delay", ref Program.Settings.Delay, 1, 50);

                    ImGui.PopStyleVar();
                    ImGui.EndTabItem();
                }

                // --- Configs Tab ---
                if (ImGui.BeginTabItem("Configs"))
                {
                    if (_activeTab != "Configs") { _activeTab = "Configs"; _tabAlpha = 0.0f; } // Reset alpha on switch

                    ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _tabAlpha); // Apply fade

                    if (ImGui.Button("Reset Defaults", new Vector2(-1, 0))) { Program.ApplyConfig(0, 0, 0, 10); }
                    ImGui.Separator();
                    if (ImGui.Button("Hammer AR", new Vector2(-1, 0))) { Program.ApplyConfig(3, -2, 1, 10); }
                    if (ImGui.Button("Blue AR", new Vector2(-1, 0))) { Program.ApplyConfig(5, 0, 1, 11); }
                    if (ImGui.Button("Scar", new Vector2(-1, 0))) { Program.ApplyConfig(4, 0, 0, 10); }
                    if (ImGui.Button("Stinger", new Vector2(-1, 0))) { Program.ApplyConfig(17, -1, 1, 7); }

                    ImGui.PopStyleVar();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
            ImGui.End();
        }
    }
}