#version 330 core

float M_PI = radians(180.0);

in vec4 gl_FragCoord;

uniform vec3 cameraPos;
uniform vec3 camera_target;

// vec3 cameraPos = vec3(0,10,10);
// vec3 camera_target = vec3(0,0,0);

int SCR_WIDTH = 1800;
int SCR_HEIGHT = 1000;

vec3 camera_up = vec3(0.0, 1.0, 0.0);

float camera_fovy =  45.0;
float focalHeight = 1.0;
float aspect = float(SCR_WIDTH)/float(SCR_HEIGHT);
float focalWidth = focalHeight * aspect; //Height * Aspect ratio
float focalDistance = focalHeight/(2.0 * tan(camera_fovy * M_PI/(180.0 * 2.0)));

float SMALLEST_DIST = 1e-4;
float FLT_MAX =  3.402823466e+38;
float t = FLT_MAX;

out vec4 color;

struct PointLight{
    vec3 position;
    vec3 intensity;
    vec3 color;
};

struct World{
    vec3 bgcolor;
};

struct BasicMaterial{
    //
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

struct Ray {
        vec3 origin;
        vec3 direction;
        float t;
        bool hit;
        int object;
};
    
struct Sphere {
        vec3 centre;
        float radius;
        int material;
};

const int num_materials = 2;
const int num_objects = 3;
const int num_lights = 2;

BasicMaterial material_set[num_materials];
Sphere object_set[num_objects];
PointLight light_set[num_lights];
World world;
void intersect(inout Ray r, int s);
vec4 shade(inout Ray r);

void main() {

    world.bgcolor = vec3(0.28, 0.28, 0.28);

    material_set[0].k_a = 0.3;
    material_set[0].c_a =  vec3(1.0, 0.43, 0.14);
    material_set[0].k_d = 0.8;
    material_set[0].c_r = vec3(1.0, 0.43, 0.14); 
    material_set[0].k_s = 0.2; 
    material_set[0].c_p = vec3(1.0,1.0,1.0); 
    material_set[0].n = 2; 

    material_set[1].k_a = 0.3;
    material_set[1].c_a =  vec3(1.0, 0.0, 0.9);
    material_set[1].k_d = 0.2;
    material_set[1].c_r = vec3(0.0, 0.0, 1.0); 
    material_set[1].k_s = 0.9; 
    material_set[1].c_p = vec3(1.0,1.0,1.0); 
    material_set[1].n = 100; 

    object_set[0].centre = vec3(2, 0, 0);
    object_set[0].radius = 1;
    object_set[0].material = 0;

    object_set[1].centre = vec3(-2, 0, 0);
    object_set[1].radius = 1;
    object_set[1].material = 1;   

    object_set[2].centre = vec3(0, 2, 0);
    object_set[2].radius = 1;
    object_set[2].material = 1;   

    light_set[0].position = vec3(0,10,10);
    light_set[0].intensity = vec3(1.0,1.0,1.0);
    light_set[0].color = vec3(1.0,1.0,1.0);

    light_set[1].position = vec3(-10,10,10);
    light_set[1].intensity = vec3(1.0,1.0,1.0);
    light_set[1].color = vec3(1.0,0.0,0.0);
    
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
    r.object = -1;
    
    for(int i = 0 ; i < num_objects; i++){
        intersect(r,i);
    }

    if (r.hit){
        color = shade(r);
    } else {
        color = vec4(world.bgcolor,1.0);
    }
};

vec4 shade(inout Ray r){

    Sphere object = object_set[r.object];
    BasicMaterial material = material_set[object.material];
    
    vec3 color = vec3(0.0,0.0,0.0);
    vec3 intersectionPosition = r.origin + r.t*r.direction;
    vec3 v = normalize(r.origin-intersectionPosition);
    vec3 n = normalize(intersectionPosition - object.centre);
    for(int i = 0; i < num_lights; i++ ){
        PointLight light = light_set[i]; 
        vec3 l = normalize(light.position - intersectionPosition);
        vec3 h = normalize(v+l);

        float diff = max(dot(n,l),0);
        vec3 diff_color = material.c_r*light.color*diff;

        vec3 ambient_color = material.c_r*material.c_a;

        float spec = pow(max(dot(n,h),0),material.n);  
        vec3 spec_color = spec*material.c_p*light.color;

        vec3 L = light.intensity*(material.k_a*ambient_color + 
                  material.k_d*diff_color + material.k_s*spec_color);
        color = color + L;
    }
    return vec4(color,1.0);
}
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
        r.hit = true;
        r.t = t1;
        r.object = index;
    }

    if(t2 < r.t && t2 > SMALLEST_DIST){
        r.hit = true;
        r.t = t2;
        r.object = index;
    }
}