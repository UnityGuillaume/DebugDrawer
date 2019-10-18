# Debug Drawer For Unity

This is an experiment in writing a small package to offer debug drawing capability in Unity.

The MVP package will offer :

- Drawing line, filled quad, wire quad drawing
- Text in world space
- Drawing image/texture (special filled quad)

Contrary to the Debug, Handles and Gizmo helper already in Unity, those are drawn in the game view.
They are also kept in developement build and can be toggled on/off (keyboard input, todo maybe a 
small console?)

Each call only last *one update*, so it's a immediate mode debug drawing, stop calling the draw command
and it won't be drawn next update.

All call to debug drawing command (e.g. `DebugDrawer.DrawLine(a,b,Color.red)`) are automatically stripped
in build not made with the `Developement build` option checked in the Unity build windows.

## Note on implementation

The system rely on a couple of meshes (line, filled quad, texture etc.) that contain vertices data
for all lines, all quad etc. asked. All call to a draw command only queue vertices info in lists, 
the mesh themselves are created only when rendering occurs.

The package is planned to work both on _Legacy_ and _Render Pipeline_ renderer of Unity, by either
hooking into `Camera.onPostRender` for _Legacy_ or `RenderPipelineManager.endCameraRendering` for 
Scriptable Render Pipeline.

It also use a custom PlayerLoop step to clear all the list at the Initialization of the Update phase
so all debug drawing asked last exactly one update cycle before being cleared.