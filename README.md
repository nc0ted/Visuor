# Visuor

Lightweight, high-performance audio visualization library for Unity.

## Features

- **FFT Analysis**: Configurable samples and FFT windows.
- **Scaling Strategies**: Linear, Logarithmic, and Musical (frequency-weighted).
- **Band Buffer**: Physics-based smoothing with gravity and fall speed.
- **Performance**: Powered by Unity **Job System** and **Burst Compiler**.
- **Universal**: Supports both **3D Objects** and **uGUI**.

## Usage

1. **Setup**: Attach `AudioVisualizerSetup` to an object in your scene. 
   *(It will automatically add `AudioParse` and `BandsVisualizer`)*.
2. **Assign Prefab**: Create a prefab with the `Band` script and assign it to the setup.
3. **Generate**: Click **Setup Normal** or **Setup Reflected** in the Inspector.
4. **Customize**: Tweak `Indent`, `Scale Multiplier`, and `Buffer Physics` for the perfect look.
