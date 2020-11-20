# *Computer Graphics Project*

## Goal

The goal of the project is to implement a real time ray tracer in `OpenGL`. Here is what we are going to do : 

* [x] Create a full screen quadrilateral.
* [x] Implement a camera controlled by keyboard and mouse.
* Pass camera data to `fragment shader`.
* Generate rays
* Ray-Object intersection
* Blinn-Phong shading for object.
* Shadows.

### Code organisation

* We are directly using `Lab 6` code of `CS333`.
* The above code includes all the libraries we need and is organised as we need.
* We use the following libraries:
  * `glfw`
  * `glm` (for mathematics)
  * `ImGui` 

* `CPU` side code is in the following:
  * `main.cpp`
  * `utils.cpp` and `utils.h`
* `GPU` side is in the following:
  * `fshader.fs` : Fragment shader
  * `vshader.vs` : Vertex shader

### *Quadrilateral*

Idea is following : 

* When each `pixel` is shaded by the `Fragment shader `, we will get `FragCoordinate` which will be pixel position.
* We will use this to generate rays and render color of each pixel.

### *Camera​*

