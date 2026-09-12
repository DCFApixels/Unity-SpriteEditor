import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';

// CPU reference/contract checks, not a substitute for compiling and rendering HLSL in Unity.
const source = name => readFileSync(new URL(`../src/FXPresets/${name}.hlsl`, import.meta.url), 'utf8');
const spherize = (p, strength) => {
    const scale = Math.pow(Math.max(p[0] ** 2 + p[1] ** 2, 1e-12), .5 * (2 ** strength - 1));
    return p.map(x => x * scale);
};
const twirl = (p, degrees) => {
    const a = -degrees * Math.PI / 180 * Math.hypot(...p);
    return [Math.cos(a) * p[0] - Math.sin(a) * p[1], Math.sin(a) * p[0] + Math.cos(a) * p[1]];
};
const near = (a, b) => assert.ok(Math.abs(a - b) < 1e-9, `${a} != ${b}`);

test('distortions are catalog effects using both Transform 2D mappings without clipping or fade', () => {
    for (const name of ['Spherize', 'Twirl']) {
        const code = source(name);
        assert.equal(code.split(/\r?\n/)[0], `// @whimtex-effect Distortion/${name}`);
        assert.match(code, /@param transform2D _Area/);
        assert.match(code, /_Area_ToLocal\(uv\)/);
        assert.match(code, /_Area_ToInput\(/);
        assert.match(code, /return SampleInput\(/);
        assert.doesNotMatch(code, /\b(?:saturate|clamp|lerp|smoothstep|clip|discard)\s*\(/);
        assert.equal((code.match(/\bif\s*\(/g) ?? []).length, 1, 'Only the zero-strength identity branch');
    }
    assert.match(source('Spherize'), /exp2\(_Strength\)/);
    assert.match(source('Spherize'), /pow\(max\(dot\(p, p\), 1e-12\), 0\.5 \* \(exponent - 1\.0\)\)/);
    assert.match(source('Twirl'), /-radians\(_Angle\) \* length\(p\)/);
});

test('spherize is finite at center and monotonic, including outside normalized bounds', () => {
    for (const strength of [-1, -.5, 0, .5, 1]) {
        assert.deepEqual(spherize([0, 0], strength), [0, 0]);
        let previous = -1;
        for (const radius of [1e-9, .01, .25, .5, 1, 1.01, 2, 10, 100]) {
            const [mapped] = spherize([radius, 0], strength);
            assert.ok(Number.isFinite(mapped) && mapped > previous);
            if (radius >= .01) near(mapped, radius ** (2 ** strength));
            previous = mapped;
        }
    }
    assert.ok(spherize([.5, 0], .5)[0] < .5, 'Positive strength samples closer to center (bulge)');
    assert.ok(spherize([.5, 0], -.5)[0] > .5, 'Negative strength pinches');
    assert.ok(spherize([2, 0], .5)[0] > 2, 'No identity fallback outside the frame');
});

test('twirl preserves radius, reverses with angle sign and continues outside the frame', () => {
    for (const p of [[0, 0], [.2, -.4], [1, 0], [2, 3], [-10, 6]]) {
        for (const angle of [-720, -180, 0, 90, 720]) {
            const mapped = twirl(p, angle);
            near(Math.hypot(...mapped), Math.hypot(...p));
            twirl(mapped, -angle).forEach((value, i) => near(value, p[i]));
        }
    }
    const outside = twirl([2, 0], 45);
    near(outside[0], 0);
    near(outside[1], -2);
});
