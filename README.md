# *Computer Graphics Project*

## Goals

The goal of the project is to implement a real time ray tracer in `OpenGL`. Here is what we are going to do : 

* [x] Create a full screen quadrilateral.
* [x] Implement a camera controlled by keyboard and mouse.
* [x] Pass camera data to `fragment shader`.
* [x] Generate rays.
* [x] Static Objects.
* [x] Ray-Object intersection.
* [x] Static Lighting.
* [x] Blinn-Phong shading for object.
* [x] Shadows.
* [ ] Reflective Surfaces.
* [ ] Refractive surfaces.
* [ ] Dynamic object Creation. 
* [ ] Dynamic Lighting. (*If time permits*)
* [ ] Dynamic Material. (*If time permits*)

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

* Camera `position` and `orientation` is calculated in every frame.
* **Knowledge of previous frame `position` is necessary**.
* In this project, what we needed was only `camera position` and `camera target`. 
* Therefore, we did not create some abstract camera class. 
* We simply calculate `camera position` and `camera target` and update them as `uniform` to the `fragment shader`.
* Source for implementation : http://www.opengl-tutorial.org/beginners-tutorials/tutorial-6-keyboard-and-mouse/

##### Mouse Control

```c++
float horizontalAngle = 3.14f; //PI
float verticalAngle = 0.0f;
...
while(!glfwWindowShouldClose(window)){
	double xpos, ypos;
    glfwGetCursorPos(window, &xpos, &ypos);
        
    double currentTime = glfwGetTime();
    float deltaTime = float(currentTime - lastTime);
    horizontalAngle += mouseSpeed * deltaTime * float(double(display_w)/2 - xpos );
    verticalAngle   += mouseSpeed * deltaTime * float(double(display_h)/2 - ypos );
}
```

* They are for Orientation.
* Orientation is the angle from vertical and angle from horizontal.
* `Height/2` is taken as base for vertical. Coordinates' difference from it is measured as change in angle. Similarly `Width/2` is taken as base for horizontal angle.

##### Keyboard Control

```c++
...
glm::vec3 position = glm::vec3( 0, 0, 5 );
float speed = 3.0f;
...
while(!glfwWindowShouldClose(window)){
    glm::vec3 right = glm::vec3(
        sin(horizontalAngle - 3.14f/2.0f),
        0,
        cos(horizontalAngle - 3.14f/2.0f)
    );

    glm::vec3 direction(
        cos(verticalAngle) * sin(horizontalAngle),
        sin(verticalAngle),
        cos(verticalAngle) * cos(horizontalAngle)
    );
    glm::vec3 up = glm::cross( right, direction );
    
    if (glfwGetKey( window,GLFW_KEY_UP ) == GLFW_PRESS){
        position += direction * deltaTime * speed;
    }
    // Move backward
    if (glfwGetKey( window,GLFW_KEY_DOWN ) == GLFW_PRESS){
        position -= direction * deltaTime * speed;
    }
    // Strafe right
    if (glfwGetKey( window,GLFW_KEY_RIGHT ) == GLFW_PRESS){
        position += right * deltaTime * speed;
    }
    // Strafe left
    if (glfwGetKey( window,GLFW_KEY_LEFT ) == GLFW_PRESS){
        position -= right * deltaTime * speed;
    }
} 
```

##### Passing to the shader

* We pass camera position and camera target to the shader.

* `main.cpp`

  * ```c++
    GLuint u_camPositions = glGetUniformLocation(shaderProgram, "cameraPos");
    glm::vec3 camPos = position;
    glUniform3fv(u_camPositions, 1, glm::value_ptr(camPos));
    
    GLuint u_camTarget = glGetUniformLocation(shaderProgram, "camera_target");
    glm::vec3 camera_target =  position+direction;
    glUniform3fv(u_camTarget, 1, glm::value_ptr(camera_target));
    ```

* `fshader.fs`

  * ```c
    uniform vec3 cameraPos;
    uniform vec3 camera_target;
    ```

##### Issue

I think if we calculate orientation and position as shown above in the fragment shader, results will be better. This is because in every frame, we have to perform a cross product. GPU should be able to give better performance. (*I have no idea if the statement is correct or not.*) 

Also, I cannot think of a way to send the updated position back to the CPU side for 

### *Ray Generation*

Here is `struct ray`

```
struct Ray {
    vec3 origin;
    vec3 direction;
    float t;
    bool hit;
    int hit_object_index;
};
```

The first ray is calculated as shown below.

```c++
vec3 line_of_sight = camera_target - cameraPos;
vec3 w = -normalize(line_of_sight);
vec3 u = normalize(cross(camera_up, w));
vec3 v = normalize(cross(w, u));
float i = gl_FragCoord.x;
float j = gl_FragCoord.y;
vec3 dir = vec3(0.0, 0.0, 0.0);
dir += -w * focalDistance;
float xw = aspect*(i - SCR_WIDTH/2.0 + 0.5)/SCR_WIDTH;
float yw = (j - SCR_HEIGHT/2.0 + 0.5)/SCR_HEIGHT;
dir += u * xw;
dir += v * yw;
    
Ray r;
r.origin = cameraPos;
r.direction = normalize(dir);
r.t = FLT_MAX;
r.hit = false;
r.hit_object_index = -1;
```

* The source for this is lecture slides. The theory can be understood from Peter Shirley's book.
* Main thing is that we get `Pixel positions` from `gl_FragCoord` which is provided in `graphics pipeline`.

* We calculate the `Camera position` and `Camera target` every frame.

### *Ray-Object intersection*

* Our objects are all spheres:

  * ```c
    struct Sphere {
        vec3 centre;
        float radius;
        int type_enum;
        int material_index;
    };
    ```

  * All objects are stored in a global array ` object_set`.

  * We use indices to access them.

  * Currently all objects are statically initialised at the start of fragment shader.

    * ```c
      void main(){
      	...
      	object_set[0].centre = vec3(0, 0, 0);
          object_set[0].radius = 1;
          object_set[0].type_enum = BLINN_PHONG;
          object_set[0].material_index = 0;
      	...
      }
      ```

    * Object set is a global array.

* Ray sphere intersection is calculated as below:

  * ```c
    void intersect(inout Ray r, int index) {
        Sphere s = object_set[index];
        float a = dot(r.direction,r.direction);
        float b = dot(r.direction, 2.0 * (r.origin-s.centre));
        float c = dot(s.centre, s.centre) + dot(r.origin,r.origin) +
                  -2.0*dot(r.origin,s.centre) - (s.radius*s.radius);
        
        float disc = b*b + (-4.0)*a*c;
        
        if (disc < 0){
            return;
        }
        
        float D = sqrt(disc);
    	float t1 = (-b +D)/(2.0*a);
    	float t2 = (-b -D)/(2.0*a);
    
        if(t1 < r.t && t1 > SMALLEST_DIST){
            //This check is to get the object closest to Ray origin.
            r.hit = true;
            r.t = t1;
            r.hit_object_index = index;
        }
    
        if(t2 < r.t && t2 > SMALLEST_DIST){
            //This check is to get the object closest to Ray origin.
            r.hit = true;
            r.t = t2;
            r.hit_object_index = index;
        }
    }
    ```

  * `inout` means a copy of ray is created. Any changes to this object also affects the original object.

* All `objects` are tested and the index of the one which is closest to Ray's `origin` will be stored in `hit_object_index` parameter.

  * ```c
    for(int i = 0 ; i < num_objects+num_lights; i++){
        intersect(r,i);
    }
    ```

### *Blinn-Phong shading for object*

#### Lighting

* Each light source is a point light.

  * ```c
    struct PointLight{
        vec3 position;
        int lightMaterial_index;
    };
    
    struct LightMaterial{
        vec3 intensity;
        vec3 color;
    };
    ```

* All lights are static. Created at the start of fragment shader.

  * ```c
    void main(){
        ...
        light_set[0].position = vec3(0,10,10);
        light_set[0].lightMaterial_index = 0;
        lightMaterial_set[0].intensity = vec3(1.0,1.0,1.0);
        lightMaterial_set[0].color = vec3(1.0,1.0,1.0);
        ...
    }
    ```

  * The are stored in a global array `light_set`.

  * Light material is also stored globally in an array.

* The theory is based on Peter Shirley's book.

* ```c
  struct BlinnPhongMaterial{
  
      float k_a;
      vec3 c_a; //ambient color
      
      //Diffuse
      float k_d;
      vec3 c_r; //diffuse reflectance
      
      //Specular
      float k_s; 
      vec3 c_p; // phong highlight
      int n; // phong exponent
  };
  ```

  * With each object, we store its material index.
  * All material is stored in a global array.

* If `ray.hit` is `true`, we shade as follows:

  * ```c
    vec4 shade_blinn_phong(inout Ray r){
    
        Sphere object = object_set[r.hit_object_index];
        BlinnPhongMaterial material = blinnPhong_set[object.material_index];
        
        vec3 color = vec3(0.0,0.0,0.0);
        vec3 intersectionPosition = r.origin + r.t*r.direction;
        vec3 v = normalize(r.origin-intersectionPosition);
        vec3 n = normalize(intersectionPosition - object.centre);
        
        for(int i = 0; i < num_lights; i++ ){
            PointLight light = light_set[i]; 
            LightMaterial lightMaterial =                                  lightMaterial_set[light.lightMaterial_index];
    
            vec3 l = normalize(light.position - intersectionPosition);
            vec3 h = normalize(v+l);
    
            
            float diff = max(dot(n,l),0);
            vec3 diff_color = material.c_r*lightMaterial.color*diff;
    
            vec3 ambient_color = material.c_r*material.c_a + world.ambience*world.ambient_color;
    
            float spec = pow(max(dot(n,h),0),material.n);  
            vec3 spec_color = spec*material.c_p*lightMaterial.color;
    
            vec3 L = lightMaterial.intensity*(material.k_a*ambient_color + 
                      material.k_d*diff_color + material.k_s*spec_color);
            color = color + L;
        }
        return vec4(color,1.0);
    }
    ```

### *Shadows*

* Calculation of shadows is simple, we create a new ray from the point of intersection directed towards the light source.

  * ```c
    for(int i = 0; i < num_lights; i++ ){
        //For each light source
    	...
    	PointLight light = light_set[i]; 
    	vec3 l = normalize(light.position - intersectionPosition);
    	Ray shadow_ray;
        shadow_ray.origin = intersectionPosition;
        shadow_ray.direction = l;
        shadow_ray.t = FLT_MAX;
        shadow_ray.hit = false;
        shadow_ray.hit_object_index = -1;
        ...
    }
    ```

* If the above ray intersects any object, 

  * Light is not reaching this object. Hence, it cannot be reflected from it.
  * Light based effects, `specular` and `diffuse` reflections, will not take place.
  * Only `ambience` will be present. (**SHADOW**)

  * ```c
    color = <0,0,0>;
    for(int i = 0; i < num_lights; i++ ){
    	...
    	for(int i = 0 ; i < num_objects; i++){
            intersect(shadow_ray,i);
        }
        if (shadow_ray.hit){
            color = color + ambient_color;
        } else {
            color = color + ambient_color + diffuse_color + specular_color;
        }
    }
    ```

