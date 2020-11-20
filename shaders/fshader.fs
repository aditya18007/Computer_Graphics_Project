#version 330 core

float M_PI = radians(180.0);

in vec4 gl_FragCoord;

uniform vec3 cameraPos;
uniform vec3 camera_target;

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

struct World{
    vec3 bgcolor;
};

struct Material{
    vec3 color;
};

struct Ray {
        vec3 origin;
        vec3 direction;
};
    
struct Sphere {
        vec3 origin;
        float radius;
        Material m;
};
    
bool intersect(Ray r, Sphere s);

void main() {
    
    World world;
    world.bgcolor = vec3(0.28, 0.28, 0.28);
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
    
    Material m;
    m.color =  vec3(0.1, 0.7, 0.0);
    Sphere s;
    s.origin = vec3(2, 0, -10);
    s.radius = 3;
    s.m = m;

    if (intersect(r,s)){
        color = vec4(s.m.color,1.0);
    }        
    else{
        color = vec4(world.bgcolor,1.0);
    }
};

 bool intersect(Ray r, Sphere s) {
    float a = dot(r.direction,r.direction);
    float b = dot(r.direction, 2.0 * (r.origin-s.origin));
    float c = dot(s.origin, s.origin) + dot(r.origin,r.origin) +-2.0*dot(r.origin,s.origin) - (s.radius*s.radius);
    
    float disc = b*b + (-4.0)*a*c;
    
    if (disc < 0){
        return false;
    }
    return true;
}