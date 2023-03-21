#version 330 core

#define PRECISION 0.0001
#define MAX_STEPS 512
#define ITERATIONS 20
#define Bailout 2
#define Power 8


uniform vec2 iResolution;
uniform float iTime;
uniform vec3 cam_pos;
uniform vec3 cam_rot;

out vec4 FragColor;

float MandelBulb(vec3 pos) {
	vec3 z = pos;
	float dr = 1.0;
	float r = 0.0;
	for (int i = 0; i < ITERATIONS ; i++) {
		r = length(z);
		if (r>Bailout) break;
		
		// convert to polar coordinates
		float theta = acos(z.z/r);
		float phi = atan(z.y,z.x);
		dr =  pow( r, Power-1.0)*Power*dr + 1.0;
		
		// scale and rotate the point
		float zr = pow(r,Power);
		theta = theta*Power;
		phi = phi*Power;
		
		// convert back to cartesian coordinates
		z = pos + zr * vec3(sin(theta)*cos(phi), 
                            sin(phi)*sin(theta),
                            cos(theta));
	}
	return 0.5*log(r)*r/dr;
}

float DE(vec3 p) {
	return MandelBulb(p);
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

	//lighting
	vec3 rayP = rayOrigin + dist.y * rayDirection;
	vec3 normal = calcNormal(rayP);
    
    vec2 uv = gl_FragCoord.xy / iResolution.xy;

	vec4 color = mix(vec4(0.2,0.01,0.08,1.),vec4(0.06,0.02,0.11,1),uv.y);
    
	float colourA = clamp(dot(light, normal*.8+.5), 0.0, 1.0);
	float colourB = clamp(dist.y/16.0, 0.0, 1.0);
	vec3 colourMix = clamp(colourA * vec3(142, 68, 173)/255.0 + colourB * vec3(52, 152, 219)/255.0, 0.0, 1.0);

	// float distOcclusion = 1.0 - dist.y * 2.0 / 256.0;
    // float diffLighting = colourA;
    // float specLighting = pow(clamp(dot(normal, normalize(light)),0.0,1.0), 32.0);

    // float combinedShading = diffLighting * 0.1 + distOcclusion * 0.8 + specLighting * 0.15 + 0.1;

	color = vec4(colourMix.xyz,1);

	float rim = dist.x / 70.;

	FragColor = mix(color, vec4(1.), 0.1) * rim;
}