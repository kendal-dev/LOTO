import bpy, re
from mathutils import Matrix, Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.usd_import(filepath=r"C:\Users\denze\OneDrive\Documentos\Abstracthon\LOTO\modelos\fuente\Molino.usdz")
sc = bpy.context.scene
M3 = Matrix(((1,0,0),(0,0,1),(0,1,0)))  # Y-up -Z front (Fusion) -> Z-up -Y front; mirror so +X is right seen from front
M4 = M3.to_4x4()
objs = {o.name:o for o in sc.objects}
for o in list(objs.values()):
    if o.type!='MESH': continue
    w = o.matrix_world.copy()
    t = w.translation.copy()
    lin3 = w.copy(); lin3.translation = (0,0,0)
    o.parent = None
    o.matrix_world = Matrix.Identity(4)
    o.data.transform(M4 @ lin3)
    o.location = M4 @ t
    o.data.flip_normals()
for n,o in objs.items():
    if o.type=='EMPTY': bpy.data.objects.remove(o)
def join(target, others):
    bpy.ops.object.select_all(action='DESELECT')
    for n in others: objs[n].select_set(True)
    objs[target].select_set(True); bpy.context.view_layer.objects.active = objs[target]
    bpy.ops.object.join()
join('Guard', ['GuardLip','Belt','MotorPulley'])
join('Body', ['BodyShaft'])
for n in ('LampPoint','LabelPoint'):
    o=objs[n]; c=sum((v.co for v in o.data.vertices), Vector())/len(o.data.vertices)
    o.data.transform(Matrix.Translation(-c)); o.location = o.location + c
root = bpy.data.objects.new('Molino', None); sc.collection.objects.link(root)
piv = bpy.data.objects.new('RotorPivot', None); sc.collection.objects.link(piv)
rot = objs['Rotor']; piv.location = rot.location.copy(); piv.parent = root
rot.parent = piv; rot.location = (0,0,0)
for n in ['Base','Body','Hopper','Motor','Guard','LampPoint','LabelPoint']:
    o = objs[n]; o.parent = root
    # origin at piece's own location is world origin; fine
for m in bpy.data.materials:
    h = re.search(r'LOTO([0-9A-F]{6})', m.name)
    if not h: continue
    hx = h.group(1); m.name = 'M_'+hx
    m.use_nodes = True
    b = next(x for x in m.node_tree.nodes if x.type=='BSDF_PRINCIPLED')
    def lin(c):
        c/=255; return c/12.92 if c<=0.04045 else ((c+0.055)/1.055)**2.4
    b.inputs['Base Color'].default_value = (lin(int(hx[0:2],16)),lin(int(hx[2:4],16)),lin(int(hx[4:6],16)),1)
    b.inputs['Alpha'].default_value = 1.0
    b.inputs['Metallic'].default_value = 0.0
    b.inputs['Roughness'].default_value = 0.5
    for l in list(m.node_tree.links):
        if l.to_node==b and l.to_socket.name in ('Base Color','Alpha'): m.node_tree.links.remove(l)
bpy.context.view_layer.update()
for o in sc.objects:
    print("OBJ", o.name, o.type, o.parent.name if o.parent else None, tuple(round(v,3) for v in o.matrix_world.translation), tuple(round(v,3) for v in o.dimensions))
bpy.ops.wm.save_as_mainfile(filepath=r"C:\Users\denze\OneDrive\Documentos\Abstracthon\LOTO\modelos\fuente\Molino.blend")
bpy.ops.export_scene.fbx(filepath=r"C:\Users\denze\OneDrive\Documentos\Abstracthon\LOTO\unity\Assets\ThirdParty\Molino\Molino.fbx",
    object_types={'EMPTY','MESH'}, global_scale=1.0, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, bake_space_transform=False, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False)
# verify
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=r"C:\Users\denze\OneDrive\Documentos\Abstracthon\LOTO\unity\Assets\ThirdParty\Molino\Molino.fbx")
tris=0
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        o.data.calc_loop_triangles(); tris+=len(o.data.loop_triangles)
    print("FBX", o.name, o.type, o.parent.name if o.parent else None, tuple(round(v,3) for v in o.matrix_world.translation), tuple(round(v,3) for v in o.dimensions))
print("TRIS", tris)
