# <Maquina>.usdz (Fusion, Y arriba, frente -Z, cm) -> <Maquina>.fbx para Unity
# Uso: blender -b --factory-startup --python convertir_maquinas_2_3_fbx.py -- Prensa|Neumatica
import bpy, re, os, sys
from mathutils import Matrix, Vector

BASE = r"C:\Users\denze\OneDrive\Documentos\Abstracthon\LOTO"
CONFIG = {
    'Prensa': {
        'joins': {'PumpUnit': ['PumpLever'], 'GaugeBody': ['GaugeDial'], 'SafetyBlock': ['SafetyStripes']},
        'pivots': {'RamPivot': 'Ram', 'GaugeNeedlePivot': 'GaugeNeedle', 'BleedValvePivot': 'BleedValve'},
        'points': ['BlockPoint', 'LockPoint', 'LabelPoint'],
    },
    'Neumatica': {
        'joins': {'GaugeBody': ['GaugeDial'], 'Rod': ['RodHead'], 'Conveyor': ['ConveyorFrame']},
        'pivots': {'ValveHandlePivot': 'ValveHandle', 'GaugeNeedlePivot': 'GaugeNeedle', 'RodPivot': 'Rod'},
        'points': ['LockPoint', 'LabelPoint'],
    },
}
name = sys.argv[sys.argv.index('--') + 1]
cfg = CONFIG[name]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.usd_import(filepath=BASE + rf"\Modelos\Prototipo1\fuente\{name}.usdz")
sc = bpy.context.scene
M4 = Matrix(((1, 0, 0), (0, 0, 1), (0, 1, 0))).to_4x4()  # Y arriba / frente -Z -> Z arriba / frente -Y
objs = {o.name: o for o in sc.objects}
for o in list(objs.values()):
    if o.type != 'MESH':
        continue
    w = o.matrix_world.copy(); t = w.translation.copy(); l = w.copy(); l.translation = (0, 0, 0)
    o.parent = None; o.matrix_world = Matrix.Identity(4)
    o.data.transform(M4 @ l); o.location = M4 @ t; o.data.flip_normals()
for o in list(objs.values()):
    if o.type == 'EMPTY':
        bpy.data.objects.remove(o)
objs = {o.name: o for o in sc.objects}

for target, others in cfg['joins'].items():
    bpy.ops.object.select_all(action='DESELECT')
    for n in others:
        objs[n].select_set(True)
    objs[target].select_set(True); bpy.context.view_layer.objects.active = objs[target]
    bpy.ops.object.join()
    objs[target].data.name = target

root = bpy.data.objects.new(name, None); sc.collection.objects.link(root)
for piv_name, child in cfg['pivots'].items():
    piv = bpy.data.objects.new(piv_name, None); sc.collection.objects.link(piv)
    piv.parent = root; piv.location = objs[child].location.copy()
    objs[child].parent = piv; objs[child].location = (0, 0, 0)
for p in cfg['points']:
    o = objs[p]; c = sum((v.co for v in o.data.vertices), Vector()) / len(o.data.vertices)
    pos = o.location + c; bpy.data.objects.remove(o)
    e = bpy.data.objects.new(p, None); sc.collection.objects.link(e); e.parent = root; e.location = pos
skip = set(cfg['pivots'].values()) | set(cfg['points'])
for n in cfg['joins']:
    skip |= set(cfg['joins'][n])
for o in sc.objects:
    if o.type == 'MESH' and o.parent is None:
        o.parent = root

def lin(c):
    c /= 255
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
for m in bpy.data.materials:
    h = re.search(r'LOTO([0-9A-F]{6})', m.name)
    if not h:
        continue
    hx = h.group(1); m.name = 'M_' + hx; m.use_nodes = True
    b = next(x for x in m.node_tree.nodes if x.type == 'BSDF_PRINCIPLED')
    for l in list(m.node_tree.links):
        if l.to_node == b:
            m.node_tree.links.remove(l)
    b.inputs['Base Color'].default_value = (lin(int(hx[0:2], 16)), lin(int(hx[2:4], 16)), lin(int(hx[4:6], 16)), 1)
    b.inputs['Alpha'].default_value = 1.0; b.inputs['Metallic'].default_value = 0.0; b.inputs['Roughness'].default_value = 0.5

bpy.ops.wm.save_as_mainfile(filepath=BASE + rf"\Modelos\Prototipo1\fuente\{name}.blend")
out = BASE + rf"\unity\Assets\ThirdParty\{name}\{name}.fbx"
os.makedirs(os.path.dirname(out), exist_ok=True)
bpy.ops.export_scene.fbx(filepath=out, object_types={'EMPTY', 'MESH'}, global_scale=1.0,
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    bake_space_transform=False, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)

# verificacion: reimportar y listar
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=out)
tris = 0
for o in sorted(bpy.context.scene.objects, key=lambda o: o.name):
    if o.type == 'MESH':
        o.data.calc_loop_triangles(); tris += len(o.data.loop_triangles)
    print("FBX", o.name, o.type, o.parent.name if o.parent else None,
          tuple(round(v, 4) for v in o.matrix_world.translation), tuple(round(v, 3) for v in o.dimensions))
print("TRIS", tris)
