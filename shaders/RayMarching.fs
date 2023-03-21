#version 330 core

#define PRECISION 0.0001
#define MAX_STEPS 512


uniform vec2 iResolution;
uniform float iTime;
uniform vec3 cam_pos;
uniform vec3 cam_rot;

out vec4 FragColor;

float DE(vec3 p) {
	float displacement = sin(iTime*0.1 * p.x) * cos(iTime*0.1 * p.y) * sin(iTime*0.1 * p.z) * 0.5;
	//float displacement = exp(sin(iTime * p.x)) - log(cos(iTime*0.5 * p.z)) * iTime * p.y*0.25;
	//float displacement = exp(sin(iTime * p.x)) - log(cos(iTime*0.5 * p.y)) * iTime * p.z*0.25;
	//float displacement = sin(p.x*p.y * iTime * 0.5) * -sin(p.y+p.z) * cos(iTime * 0.5 * p.z / p.x);
	float d = length(mod(p, 2.5) - 2.5/2.0) - 0.4;
	return d + displacement;
}

vec2 RayMarching(vec3 rayOrigin, vec3 rayDirection){
    float depth = 0.0;
	int i = 0;

	for(;i < MAX_STEPS; ++i){
		float precis = PRECISION * depth;
		float dist = DE(rayOrigin + depth * rayDirection);

		if (dist < precis || depth > 20.0) break;

		depth += dist;
	}

	return vec2(i, depth);
}

vec3 calcNormal(vec3 p) {
	float e = 2e-6f;
	float n = DE(p);
    return normalize(vec3(
        DE(p + vec3(e, 0, 0)) - n,
        DE(p + vec3(0, e, 0)) - n,
        DE(p + vec3(0, 0, e)) - n
    ));
}

void main() {

	vec2 p = ((-iResolution.xy + 2.0 * (gl_FragCoord.xy)) / iResolution.y);
	vec3 rayOrigin = vec3(0.0, 0.0, 4.0) + cam_pos;
	float cr = 0.0;
	vec3 cw = normalize(cam_rot);
	vec3 cp = vec3(sin(cr), cos(cr), 0.0);
	vec3 cu = normalize(cross(cw, cp));
	vec3 cv = normalize(cross(cu, cw));
	mat3 ca = mat3(cu, cv, cw);
    vec3 rayDirection = ca * normalize(vec3(p.xy, 2.0 * 1.0));

	vec2 dist = RayMarching(rayOrigin, rayDirection);

    //lights, point light at player position
	vec3 light = -normalize(rayDirection);

	vec3 col;

	//lighting
	vec3 rayP = rayOrigin + dist.y * rayDirection;
	vec3 normal = calcNormal(rayP);

	float distOcclusion = 1.0 - dist.y * 2.0 / 256.0;
	float diffLighting = clamp(dot(light, normal), 0.0, 1.0);
	float specLighting = pow(clamp(dot(normal, normalize(light-rayDirection)),0.0,1.0), 32.0);

	float combinedShading = diffLighting * 0.1 + distOcclusion * 0.8 + specLighting * 0.15 + 0.1;

	col = (sin(vec3(.3, .35, .85) * sqrt(dist.x/MAX_STEPS) * 40)*.5+.5) * combinedShading;
	//col = vec3(1-(dist.x/MAX_STEPS)) * combinedShading;
	
	//apply fog
	float fogStrength = exp(-dist.y*0.01);
	col = vec3(0.1)*(1.0-fogStrength) + col*fogStrength;

	//contrast enhancing
	col = vec3(col.x*col.x,col.y*col.y,col.z*col.z);

	FragColor = vec4(col, 1.0);
}