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

float fractalBox(vec3 pos){
	pos /= 2.0;
	float ov = 1.0/3.0;
	float yOffset = 0.74;
	//float yOffset = 1.25;
	vec3 offset = vec3(2.8,0.5,0.025);
	//vec3 offset = vec3(2.8,0.5,0.0);
	//vec3 offset = vec3(2.1,2.1,0.4);
	//vec3 offset = vec3(1.59,0.2,0.065);
	//vec3 offset = vec3(2.59,0.5,0.05);
	//vec3 offset = vec3(2.028,1.0,0.48);
	//vec3 offset = vec3(1.659,0.02,0.0765);
	//vec3 offset = vec3(1.45,0.01,0.17);
	//float scale = 2.5 + (3.25-2.5) * cos(iTime*0.1);
	float scale = 1.75 + (2.0-1.75) * cos(iTime*0.01);
	float r;
	int i = 0;

	while (i < ITERATIONS && dot(pos, pos) < 10000.0){
		pos.xy = abs(pos.xy);
		if(pos.y > pos.x) pos.xy = pos.yx;
		pos.y = yOffset - abs(pos.y - yOffset);
		pos.x += ov;
		if(pos.z > pos.x) pos.xz = pos.zx;
		pos.x -= 2*ov;
		if(pos.z > pos.x) pos.xz = pos.zx;
		pos.x += ov;
		pos = scale * (pos - offset) + offset;
		r = dot(pos, pos);
		++i;
	}
	return abs(length(pos) - length(offset)) * pow(scale, float(-i));
}

float DE(vec3 p) {
	return fractalBox(p);
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