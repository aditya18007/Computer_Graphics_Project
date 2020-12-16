#version 330 core


float M_PI = radians(180.0);

in vec4 gl_FragCoord;

uniform vec3 cameraPos;
uniform vec3 camera_target;

const int SCR_WIDTH = 1800;
const int SCR_HEIGHT = 1000;

const vec3 camera_up = vec3(0.0, 1.0, 0.0);

const float camera_fovy =  45.0;
const float focalHeight = 1.0;
float aspect = float(SCR_WIDTH)/float(SCR_HEIGHT);
float focalWidth = focalHeight * aspect; //Height * Aspect ratio
float focalDistance = focalHeight/(2.0 * tan(camera_fovy * M_PI/(180.0 * 2.0)));

const float SMALLEST_DIST = 1e-4;
const vec3 SMALLEST_DIST_vector = vec3(
    SMALLEST_DIST,
    SMALLEST_DIST,
    SMALLEST_DIST
);
const float FLT_MAX =  3.402823466e+38;
const float t = FLT_MAX;

out vec4 pixel_color;

struct World{
    vec3 bgcolor;
    vec3 ambient_color;
    float ambience;
};
World world;

struct Ray {
        vec3 origin;
        vec3 direction;
        float t;
        bool hit;
        int hit_object_index;
};

//Object Type
const int SQUARE = 1;
const int SPHERE = 2;

//Material Type
const int BLINN_PHONG = 1;
const int REFLECTIVE = 2;
const int REFRACTIVE = 3;

struct Object{
    int object_type;
    int object_index;
    int material_type;
    int material_index;
    vec3 point_of_intersection;
    vec3 normal;
};

struct Square {
        vec3 A;
        vec3 B;
        vec3 C;
        vec3 D;
        vec3 normal;
};

//        D----------A
//        |          |
//        |          |
//        C----------B
// Normal coming out of screen
const int num_squares = 3;
Square square_set[num_squares];

struct Sphere {
        vec3 centre;
        float radius;
};
const int num_spheres = 1;
Sphere sphere_set[num_spheres];

const int num_objects = num_squares+num_spheres;
Object object_set[num_objects];

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

const int num_blinnPhongMaterial = 2;
BlinnPhongMaterial blinnPhong_set[num_blinnPhongMaterial];

struct ReflectiveMaterial{
    float k_a;
    float k_d;
    float k_s;

    vec3 color; //ambient color
    int n; // phong exponent
};
const int num_reflective = 1;
ReflectiveMaterial reflective_set[num_reflective];

struct PointLight{
    vec3 position;
    vec3 intensity;
    vec3 color;
};

const int num_lights = 2;
PointLight light_set[num_lights];

void intersect_sphere(inout Ray r, int sphere_index, int object_index);
void intersect_square(inout Ray r, int square_index, int object_index);
vec4 shade_blinn_phong(inout Ray r);
vec4 shade_reflective(inout Ray r);

Ray new_ray(vec3 e, vec3 d){
    Ray r;
    r.origin = e;
    r.direction = normalize(d);
    r.t = FLT_MAX;
    r.hit = false;
    r.hit_object_index = -1;
    return r;
}

void world_setup(){
    world.bgcolor = vec3(0.0,0.0,0.0);
    world.ambient_color = vec3(0.28,0.28,0.28);
    world.ambience = 0.8;
}
void material_setup(){
    int i = 0;
    blinnPhong_set[i].k_a = 0.3;
    blinnPhong_set[i].c_a =  vec3(1.0, 0.0, 0.9);
    blinnPhong_set[i].k_d = 0.2;
    blinnPhong_set[i].c_r = vec3(0.0, 0.0, 1.0); 
    blinnPhong_set[i].k_s = 0.9; 
    blinnPhong_set[i].c_p = vec3(1.0,1.0,1.0); 
    blinnPhong_set[i++].n = 100; 
    
    blinnPhong_set[i].k_a = 0.3;
    blinnPhong_set[i].c_a =  vec3(1.0, 0.43, 0.14);
    blinnPhong_set[i].k_d = 0.8;
    blinnPhong_set[i].c_r = vec3(1.0, 0.43, 0.14); 
    blinnPhong_set[i].k_s = 0.2; 
    blinnPhong_set[i].c_p = vec3(1.0,1.0,1.0); 
    blinnPhong_set[i++].n = 2; 

    reflective_set[0].k_a = 0.0;
    reflective_set[0].k_d = 0.1;
    reflective_set[0].k_s = 0.9;
    reflective_set[0].color = vec3(0.4, 0.4, 0.4);
    reflective_set[0].n = 1000;
}

void light_setup(){
    int i = 0;
    light_set[i].position = cameraPos;
    light_set[i].intensity = vec3(1.0,1.0,1.0);
    light_set[i++].color = vec3(1.0,1.0,1.0);
    
    light_set[i].position = vec3(-10,10,10);
    light_set[i].intensity = vec3(1.0,1.0,1.0);
    light_set[i++].color = vec3(1.0,1.0,1.0);
}

void square_setup(){
    int i = 0;
    square_set[i].A = vec3(  4.0 ,  4.0, -4.0);
    square_set[i].B = vec3(  4.0 , -4.0, -4.0);
    square_set[i].C = vec3( -4.0 , -4.0, -4.0);
    square_set[i].D = vec3( -4.0 ,  4.0, -4.0);
    square_set[i].normal = cross(
        square_set[i].A - square_set[i].B,
        square_set[i].D - square_set[i].A
    );

    //Left
    square_set[++i].A = vec3( -4.0 ,  4.0, -4.0);
    square_set[i].B   = vec3( -4.0 , -4.0, -4.0);
    square_set[i].C   = vec3( -4.0 , -4.0,  4.0);
    square_set[i].D   = vec3( -4.0 , 4.0,  4.0);
    square_set[i].normal = cross(
        square_set[i].A - square_set[i].B,
        square_set[i].D - square_set[i].A
    );

    //Right
    square_set[++i].A = vec3(  4.0 ,  4.0,  4.0);
    square_set[i].B   = vec3(  4.0 , -4.0,  4.0);
    square_set[i].C   = vec3(  4.0 , -4.0, -4.0);
    square_set[i].D   = vec3(  4.0 ,  4.0, -4.0);
    square_set[i].normal = cross(
        square_set[i].A - square_set[i].B,
        square_set[i].D - square_set[i].A
    );
}

void sphere_setup(){
    int i = 0;
    sphere_set[i].centre = vec3(0.0,0.0,0.0);
    sphere_set[i++].radius = 1.0;
}

void object_setup(){
    int i = 0;

    int i_square = 0;
    //Back
    object_set[i].object_type = SQUARE;
    object_set[i].object_index = i_square++;
    object_set[i].material_type = REFLECTIVE;
    object_set[i].material_index = 0;
    object_set[i].point_of_intersection = vec3(0.0,0.0,0.0);
    object_set[i++].normal = vec3(0.0,0.0,0.0);

    //Left
    object_set[i].object_type = SQUARE;
    object_set[i].object_index = i_square++;
    object_set[i].material_type = BLINN_PHONG;
    object_set[i].material_index = 0;
    object_set[i].point_of_intersection = vec3(0.0,0.0,0.0);
    object_set[i++].normal = vec3(0.0,0.0,0.0);

    //Right
    object_set[i].object_type = SQUARE;
    object_set[i].object_index = i_square++;
    object_set[i].material_type = BLINN_PHONG;
    object_set[i].material_index = 1;
    object_set[i].point_of_intersection = vec3(0.0,0.0,0.0);
    object_set[i++].normal = vec3(0.0,0.0,0.0);
    square_setup();

    int i_sphere = 0;
    object_set[i].object_type = SPHERE;
    object_set[i].object_index = i_sphere++;
    object_set[i].material_type = BLINN_PHONG;
    object_set[i].material_index = 0;
    object_set[i].point_of_intersection = vec3(0.0,0.0,0.0);
    object_set[i++].normal = vec3(0.0,0.0,0.0);

    sphere_setup();
}
void static_setup(){
    world_setup();
    material_setup();
    light_setup();
    object_setup();
}

Ray getMainRay(){
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
    
    return new_ray(cameraPos,dir);
}

void main() {
    static_setup();
    Ray R = getMainRay();
    for(int i = 0 ; i < num_objects ; i++){
        switch(object_set[i].object_type){
            case SQUARE:
                intersect_square(R, 
                object_set[i].object_index,
                i);
                break;
            
            case SPHERE:
                intersect_sphere(R,
                object_set[i].object_index,
                i);
                break;
        }
    }
    if(R.hit){
        switch(object_set[R.hit_object_index].material_type){
            case BLINN_PHONG:
                pixel_color = shade_blinn_phong(R);
                break;
            
            case REFLECTIVE:
                pixel_color = shade_reflective(R);
                break;
        }
    } else {
        pixel_color = vec4(world.bgcolor,1.0);
    }
}

vec4 shade_reflective(inout Ray r){

    Object object = object_set[r.hit_object_index];
    ReflectiveMaterial material = reflective_set[object.material_index];
    vec3 color = vec3(0.0,0.0,0.0) ;
    vec3 point_of_intersection = object.point_of_intersection;

    vec3 v = normalize(r.origin-point_of_intersection);
    vec3 n = normalize(object.normal);
    for(int i = 0; i < num_lights; i++ ){
        PointLight light = light_set[i]; 
        vec3 l = normalize(light.position - point_of_intersection);
        vec3 h = normalize(v+l);

        float spec = pow(max(dot(n,h),0),material.n);  
        vec3 spec_color = spec*light.color;
        float diff = max(dot(n,l),0);
        vec3 reflection_direction =  -v - 2*dot(-v,n)*n;
        Ray reflected_ray_1 = new_ray(point_of_intersection+SMALLEST_DIST_vector,
            reflection_direction
        );
        for(int i = 0 ; i < num_objects ; i++){
            switch(object_set[i].object_type){
                case SQUARE:
                    intersect_square(reflected_ray_1, 
                    object_set[i].object_index,
                    i);
                    break;
                
                case SPHERE:
                    intersect_sphere(reflected_ray_1,
                    object_set[i].object_index,
                    i);
                    break;
            }
        }
        if(reflected_ray_1.hit){
            switch(object_set[reflected_ray_1.hit_object_index].material_type){
                case BLINN_PHONG:
                color = color+shade_blinn_phong(reflected_ray_1).xyz;
                break;
                
                case REFLECTIVE:
                    color = color+material.color*diff;
                    break;
            }
        } else {
            color = color+material.color*diff; 
        }
        color = color + material.k_s*(spec_color*light.color);
    }
    return vec4(color,1.0);
}

vec4 shade_blinn_phong(inout Ray r){
    Object object = object_set[r.hit_object_index];
    BlinnPhongMaterial material = blinnPhong_set[object.material_index];
    vec3 color = vec3(0.0,0.0,0.0);
    vec3 point_of_intersection = object.point_of_intersection;

    vec3 v = normalize(r.origin-point_of_intersection);
    vec3 n = normalize(object.normal);
    for(int i = 0; i < num_lights; i++ ){
        PointLight light = light_set[i]; 
        vec3 l = normalize(light.position - point_of_intersection);
        vec3 h = normalize(v+l);

        float diff = max(dot(n,l),0);
        vec3 diff_color = material.c_r*diff;

        vec3 ambient_color = material.c_r;

        float spec = pow(max(dot(n,h),0),material.n);  
        vec3 spec_color = spec*light.color;

        vec3 L = (material.k_a*ambient_color + 
                  material.k_d*diff_color + material.k_s*spec_color);
        
        Ray shadow_ray = new_ray(point_of_intersection+SMALLEST_DIST_vector,l);
        for(int i = 0 ; i < num_objects ; i++){
            switch(object_set[i].object_type){
                case SQUARE:
                    intersect_square(shadow_ray, 
                    object_set[i].object_index,
                    i);
                    break;
                
                case SPHERE:
                    intersect_sphere(shadow_ray,
                    object_set[i].object_index,
                    i);
                    break;
            }
        }
        if(shadow_ray.hit){
            color = color + material.k_a*ambient_color;
        } else {
            color = color + L;
        }
    }
    return vec4(color,1.0);
}
void intersect_square(inout Ray r, int square_index, int object_index){
    Square sqr = square_set[square_index];
    vec3 p1 = sqr.A;
    vec3 e = r.origin;
    vec3 d = r.direction;
    vec3 n = sqr.normal;
    float t = dot(p1-e,n)/dot(d,n);
    if (t < 0){
        return;
    }
    if(t < SMALLEST_DIST){
        return;
    }

    if (t >= r.t){
        return;
    }

    vec3 intersection_point = e+t*d;
    vec3 v = intersection_point - p1;
    vec3 e1 = sqr.D-sqr.A;
    vec3 e2 = sqr.B-sqr.A;
    float width = length(e1);
    float height = length(e2);
    float proj1 = dot(v,e1)/width;
    float proj2 = dot(v,e2)/height;
    if(proj1 < 0){
        return;
    }
    if (proj2 < 0){
        return;
    }
    if((proj1 < width ) && (proj2 < height)){
        r.hit = true;
        r.t = t;
        r.hit_object_index = object_index; 
        object_set[object_index].point_of_intersection = intersection_point;
        object_set[object_index].normal = n;
    }
}

void intersect_sphere(inout Ray r, int sphere_index, int object_index) {
    Sphere s = sphere_set[sphere_index];
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

    vec3 e = r.origin;
    vec3 d = r.direction;
    
    if(t1 < r.t && t1 > SMALLEST_DIST){
        r.hit = true;
        r.t = t1;
        r.hit_object_index = object_index;    
        vec3 intersection_point = e+t1*d; 
        object_set[object_index].point_of_intersection = intersection_point;
        object_set[object_index].normal = intersection_point-s.centre;
    }

    if(t2 < r.t && t2 > SMALLEST_DIST){
        r.hit = true;
        r.t = t2;
        r.hit_object_index = object_index;
        vec3 intersection_point = e+t2*d; 
        object_set[object_index].point_of_intersection = intersection_point;
        object_set[object_index].normal = intersection_point-s.centre;
    }
}