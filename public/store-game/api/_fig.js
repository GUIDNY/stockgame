/* A scanned figure, made light enough for a mall full of them: Tripo's rigged GLB (about 4 MB, 40k faces, 2k
   textures) to about 1 MB: the mesh simplified to a third, the colour texture to 1024 JPEG, the normal map to 512,
   what the game never uses (metal-roughness, occlusion, animations) dropped. Positions stay plain floats: the
   skeleton mapping reads them directly. */
// loaded as ES modules: the server's Node cannot require() them (glTF Transform's core pulls in an .mjs)
let LIBS=null;
const libs = () => LIBS || (LIBS = Promise.all([import('@gltf-transform/core'), import('@gltf-transform/extensions'), import('@gltf-transform/functions'), import('meshoptimizer'), import('sharp')])
  .then(([core, ext, fn, mo, sh]) => ({ NodeIO: core.NodeIO, ALL_EXTENSIONS: ext.ALL_EXTENSIONS, fn, MeshoptSimplifier: mo.MeshoptSimplifier, sharp: sh.default || sh })));

module.exports = async function packFigure(buf, ratio = 0.35) {
  const { NodeIO, ALL_EXTENSIONS, fn: { dedup, prune, weld, simplify, textureCompress }, MeshoptSimplifier, sharp } = await libs();
  await MeshoptSimplifier.ready;
  const io = new NodeIO().registerExtensions(ALL_EXTENSIONS), doc = await io.readBinary(new Uint8Array(buf)), root = doc.getRoot();
  root.listAnimations().forEach(a => a.dispose());
  root.listMaterials().forEach(m => { m.setMetallicRoughnessTexture(null); m.setOcclusionTexture(null); m.setMetallicFactor(0); m.setRoughnessFactor(0.8); });
  await doc.transform(dedup(), prune(), weld(), simplify({ simplifier: MeshoptSimplifier, ratio, error: 0.002 }), prune(),
    textureCompress({ encoder: sharp, targetFormat: 'jpeg', resize: [1024, 1024], quality: 82, slots: /^baseColorTexture$/ }),
    textureCompress({ encoder: sharp, targetFormat: 'jpeg', resize: [512, 512], quality: 85, slots: /^normalTexture$/ }));
  return Buffer.from(await io.writeBinary(doc));
};
