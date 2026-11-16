"""
Planet Scrappers - procedural character and vehicle models.

Run headless (Blender 4.2+):
    blender -b --factory-startup --python tools/blender/build_models.py

Writes FBX files to Assets/Art/Models/. Everything is authored in Unity coordinates
(x right, y up, z forward, metres) and converted on the way in, so the numbers here
match the greybox builder. Mesh objects are named "<Part>__<MaterialKey>"; the Unity
builder assigns its own materials by that key, so no materials travel in the FBX.
Empties carry the pivot names the game scripts look for (Body, Head, Arm L, ...).
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Matrix, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "Models")


# ───────────────────────── coordinate helpers ─────────────────────────

def U(x, y, z):
    """Unity position -> Blender position. With this export (Y up, -Z forward) and
    Unity's bakeAxisConversion, a Blender point (x, y, z) imports at Unity (x, y, -z)."""
    return Vector((x, y, -z))


def US(sx, sy, sz):
    """Unity scale -> Blender scale."""
    return Vector((sx, sy, sz))


# Unity axis -> rotation that turns Blender's +Z primitive axis onto it
AXIS_ROT = {
    "z": Matrix.Identity(4),
    "x": Matrix.Rotation(math.radians(90), 4, "Y"),
    "y": Matrix.Rotation(math.radians(90), 4, "X"),
}


def unity_euler(ex=0.0, ey=0.0, ez=0.0):
    """Unity euler (degrees, applied Z then X then Y) -> Blender rotation matrix.
    The z mirror flips the sense of rotations about X and Y."""
    rx = Matrix.Rotation(math.radians(-ex), 4, "X")
    ry = Matrix.Rotation(math.radians(-ey), 4, "Y")
    rz = Matrix.Rotation(math.radians(ez), 4, "Z")
    return ry @ rx @ rz


# ───────────────────────── scene helpers ─────────────────────────

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


class Model:
    """Collects parts per (pivot, material) and joins them on finish."""

    def __init__(self, name):
        self.name = name
        self.root = self.empty(name, None, (0, 0, 0))
        self.parts = {}

    def empty(self, name, parent, pos):
        e = bpy.data.objects.new(name, None)
        e.empty_display_size = 0.08
        bpy.context.scene.collection.objects.link(e)
        # Pivots carry translation only, relative to their parent, so the FBX
        # hierarchy imports with identity rotations.
        world = U(*pos)
        if parent is not None:
            e.parent = parent
            e.location = world - parent.matrix_world.translation
        else:
            e.location = world
        bpy.context.view_layer.update()
        return e

    def _add(self, obj, pivot, mat):
        self.parts.setdefault((pivot.name, mat), []).append(obj)

    def _place(self, obj, pos, rot):
        obj.matrix_world = Matrix.Translation(U(*pos)) @ rot

    # primitives -----------------------------------------------------------

    def box(self, pivot, mat, pos, size, bevel=0.02, euler=(0, 0, 0)):
        me = bpy.data.meshes.new("box")
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        s = US(*size)
        for v in bm.verts:
            v.co = Vector((v.co.x * s.x, v.co.y * s.y, v.co.z * s.z))
        if bevel > 0:
            bmesh.ops.bevel(bm, geom=bm.edges[:], offset=min(bevel, min(size) * 0.45), segments=3,
                            profile=0.5, affect="EDGES")
        bm.to_mesh(me)
        bm.free()
        obj = self._obj(me)
        self._place(obj, pos, unity_euler(*euler))
        self._add(obj, pivot, mat)
        return obj

    def sphere(self, pivot, mat, pos, size, euler=(0, 0, 0), segs=32):
        me = bpy.data.meshes.new("sphere")
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=segs // 2, radius=0.5)
        s = US(*size)
        for v in bm.verts:
            v.co = Vector((v.co.x * s.x, v.co.y * s.y, v.co.z * s.z))
        bm.to_mesh(me)
        bm.free()
        obj = self._obj(me)
        self._place(obj, pos, unity_euler(*euler))
        self._add(obj, pivot, mat)
        return obj

    def cyl(self, pivot, mat, pos, radius, length, axis="y", radius2=None, segs=28, bevel=0.01, euler=(0, 0, 0)):
        me = bpy.data.meshes.new("cyl")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=radius,
                              radius2=radius if radius2 is None else radius2, depth=length)
        if bevel > 0:
            rim = [e for e in bm.edges if len(e.link_faces) == 2 and
                   (abs(e.link_faces[0].normal.z) > 0.9) != (abs(e.link_faces[1].normal.z) > 0.9)]
            bmesh.ops.bevel(bm, geom=rim, offset=min(bevel, length * 0.3, radius * 0.3), segments=2,
                            profile=0.5, affect="EDGES")
        bm.to_mesh(me)
        bm.free()
        obj = self._obj(me)
        self._place(obj, pos, unity_euler(*euler) @ AXIS_ROT[axis])
        self._add(obj, pivot, mat)
        return obj

    def capsule(self, pivot, mat, pos, radius, length, axis="y", squash=(1.0, 1.0), euler=(0, 0, 0)):
        """Capsule of total length `length` along a Unity axis; squash scales the cross-section."""
        me = bpy.data.meshes.new("capsule")
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=28, v_segments=16, radius=radius)
        half = max(0.0, length / 2 - radius)
        for v in bm.verts:
            v.co.x *= squash[0]
            v.co.y *= squash[1]
            if v.co.z > 1e-5:
                v.co.z += half
            elif v.co.z < -1e-5:
                v.co.z -= half
        bm.to_mesh(me)
        bm.free()
        obj = self._obj(me)
        self._place(obj, pos, unity_euler(*euler) @ AXIS_ROT[axis])
        self._add(obj, pivot, mat)
        return obj

    def torus(self, pivot, mat, pos, major, minor, axis="y", euler=(0, 0, 0)):
        bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=32, minor_segments=10)
        obj = bpy.context.active_object
        obj.matrix_world = Matrix.Translation(U(*pos)) @ unity_euler(*euler) @ AXIS_ROT[axis]
        self._add(obj, pivot, mat)
        return obj

    def rod(self, pivot, mat, a, b, radius, segs=16):
        """Cylinder from Unity point a to Unity point b. Returns the rotation used."""
        pa, pb = U(*a), U(*b)
        d = pb - pa
        rot = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
        me = bpy.data.meshes.new("rod")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=radius, radius2=radius, depth=d.length)
        bm.to_mesh(me)
        bm.free()
        obj = self._obj(me)
        obj.matrix_world = Matrix.Translation((pa + pb) / 2) @ rot
        self._add(obj, pivot, mat)
        return rot

    def box_along(self, pivot, mat, centre, rot, size_xyz, bevel=0.02):
        """Box in a rod's frame: size along Blender local x, y and the rod (z)."""
        me = bpy.data.meshes.new("box")
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co = Vector((v.co.x * size_xyz[0], v.co.y * size_xyz[1], v.co.z * size_xyz[2]))
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=min(bevel, min(size_xyz) * 0.45), segments=3, profile=0.5, affect="EDGES")
        bm.to_mesh(me)
        bm.free()
        obj = self._obj(me)
        obj.matrix_world = Matrix.Translation(U(*centre)) @ rot
        self._add(obj, pivot, mat)

    def _obj(self, me):
        obj = bpy.data.objects.new("part", me)
        bpy.context.scene.collection.objects.link(obj)
        return obj

    # finish ---------------------------------------------------------------

    def finish(self):
        for (pivot_name, mat), objs in self.parts.items():
            bpy.ops.object.select_all(action="DESELECT")
            for o in objs:
                o.select_set(True)
            bpy.context.view_layer.objects.active = objs[0]
            if len(objs) > 1:
                bpy.ops.object.join()
            joined = bpy.context.view_layer.objects.active
            safe = pivot_name.replace(" ", "")
            joined.name = f"{safe}_{mat}__{mat}"
            joined.data.name = joined.name
            # Bake the mesh into its pivot's space: the object sits exactly on the pivot.
            pivot = bpy.data.objects[pivot_name]
            bpy.context.view_layer.update()
            joined.data.transform(pivot.matrix_world.inverted() @ joined.matrix_world)
            joined.parent = pivot
            joined.matrix_parent_inverse = Matrix.Identity(4)
            joined.matrix_basis = Matrix.Identity(4)
            try:
                bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))
            except Exception:
                bpy.ops.object.shade_smooth()

    def export(self, filename):
        self.finish()
        os.makedirs(OUT, exist_ok=True)
        path = os.path.join(OUT, filename)
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.export_scene.fbx(
            filepath=path, use_selection=False, object_types={"EMPTY", "MESH"},
            apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
            axis_forward="-Z", axis_up="Y", bake_space_transform=False,
            use_mesh_modifiers=True, mesh_smooth_type="FACE", add_leaf_bones=False,
            bake_anim=False, use_custom_props=False)
        print(f"[models] wrote {path}")


# ───────────────────────── Rookie ─────────────────────────

def build_rookie():
    reset()
    m = Model("Rookie")
    body = m.empty("Body", m.root, (0, 0, 0))

    # Torso: a softened barrel with a darker yoke and panel seams
    m.capsule(body, "SuitOrange", (0, 1.27, 0), 0.31, 0.78, squash=(1.0, 0.78))
    m.capsule(body, "SuitOrangeDark", (0, 1.5, 0.0), 0.27, 0.5, axis="x", squash=(0.62, 0.75))
    m.cyl(body, "SuitGrey", (0, 1.62, 0), 0.2, 0.08, radius2=0.18)                       # neck ring
    m.torus(body, "MetalDark", (0, 1.66, 0), 0.19, 0.025)
    # belt with pouches
    m.cyl(body, "Rubber", (0, 1.0, 0), 0.33, 0.1, segs=36)
    m.box(body, "MetalDark", (0, 1.0, 0.3), (0.16, 0.1, 0.05), bevel=0.015)
    for x in (-0.2, 0.2):
        m.box(body, "SuitOrangeDark", (x, 0.99, 0.25), (0.13, 0.14, 0.08), bevel=0.025)
    # harness straps over the chest
    for x in (-0.14, 0.14):
        m.box(body, "Rubber", (x, 1.33, 0.235), (0.07, 0.5, 0.03), bevel=0.01, euler=(-8, 0, 0))
        m.box(body, "SuitOrangeDark", (x * 1.05, 1.42, 0.25), (0.12, 0.09, 0.04), bevel=0.015)
    m.box(body, "Rubber", (0, 1.25, 0.25), (0.34, 0.06, 0.03), bevel=0.01)              # cross strap
    m.box(body, "SuitGrey", (0, 1.25, 0.27), (0.09, 0.08, 0.02), bevel=0.01)            # buckle
    # life-support pack with tanks, vents and a status light
    m.box(body, "Rubber", (0, 1.32, -0.3), (0.52, 0.6, 0.24), bevel=0.06)
    m.box(body, "SuitGrey", (0, 1.52, -0.42), (0.36, 0.08, 0.04), bevel=0.015)
    for x in (-0.13, 0.13):
        m.capsule(body, "MetalDark", (x, 1.32, -0.44), 0.075, 0.5)
        m.torus(body, "SuitGrey", (x, 1.48, -0.44), 0.075, 0.012)
    m.sphere(body, "BotEye", (0, 1.62, -0.4), (0.05, 0.05, 0.05))

    # Helmet: round dome, wide visor with a grey rim, ear modules and a small antenna
    head = m.empty("Head", body, (0, 1.88, 0))
    m.sphere(head, "SuitOrange", (0, 1.88, 0), (0.62, 0.6, 0.6))
    m.sphere(head, "VisorBlack", (0, 1.865, 0.12), (0.5, 0.42, 0.38))
    m.torus(head, "SuitGrey", (0, 1.865, 0.2), 0.215, 0.022, axis="z")
    m.box(head, "SuitOrangeDark", (0, 2.15, 0.02), (0.08, 0.06, 0.42), bevel=0.025)    # crest ridge
    for x in (-0.31, 0.31):
        m.cyl(head, "SuitGrey", (x, 1.88, 0), 0.1, 0.06, axis="x")
        m.cyl(head, "Rubber", (x * 1.09, 1.88, 0), 0.055, 0.04, axis="x")
    m.cyl(head, "MetalDark", (-0.33, 2.02, -0.05), 0.012, 0.3)
    m.sphere(head, "TrimOrange", (-0.33, 2.18, -0.05), (0.04, 0.04, 0.04))

    # Left arm: suit sleeve, cuff, gloved hand with a thumb
    arm_l = m.empty("Arm L", body, (-0.4, 1.52, 0))
    m.sphere(arm_l, "SuitOrangeDark", (-0.4, 1.53, 0), (0.24, 0.22, 0.24))
    m.capsule(arm_l, "SuitOrange", (-0.41, 1.27, 0), 0.105, 0.5)
    m.cyl(arm_l, "SuitOrangeDark", (-0.41, 1.06, 0), 0.115, 0.06)
    m.sphere(arm_l, "Rubber", (-0.41, 0.95, 0.01), (0.17, 0.18, 0.15))
    m.capsule(arm_l, "Rubber", (-0.36, 0.97, 0.06), 0.035, 0.1, euler=(0, 0, -30))

    # Right arm: the mechanical salvage arm with gears, pistons and a claw tool mount
    arm_r = m.empty("Salvage Arm R", body, (0.4, 1.52, 0))
    m.cyl(arm_r, "Metal", (0.44, 1.52, 0), 0.14, 0.07, axis="x", segs=12, bevel=0.0)    # gear (12 teeth look)
    m.cyl(arm_r, "TrimYellow", (0.48, 1.52, 0), 0.08, 0.04, axis="x")
    m.capsule(arm_r, "Metal", (0.4, 1.3, 0), 0.06, 0.42)
    m.cyl(arm_r, "MetalDark", (0.47, 1.3, -0.02), 0.022, 0.32)                          # piston
    m.cyl(arm_r, "Metal", (0.44, 1.1, 0), 0.105, 0.06, axis="x", segs=12, bevel=0.0)     # elbow gear
    m.cyl(arm_r, "TrimYellow", (0.48, 1.1, 0), 0.06, 0.04, axis="x")
    m.capsule(arm_r, "MetalDark", (0.4, 0.94, 0), 0.07, 0.34, squash=(1.0, 1.1))
    m.box(arm_r, "TrimOrange", (0.4, 0.96, 0.07), (0.08, 0.16, 0.02), bevel=0.008)
    m.cyl(arm_r, "Metal", (0.4, 0.8, 0), 0.07, 0.05)                                     # wrist collar
    tool = m.empty("Tool Mount", arm_r, (0.4, 0.78, 0))
    for (dx, dz, ez, ex) in ((-0.05, 0.03, 14, 0), (0.05, 0.03, -14, 0), (0, -0.05, 0, -14)):
        m.box(tool, "MetalDark", (0.4 + dx, 0.71, dz), (0.035, 0.13, 0.035), bevel=0.01, euler=(ex, 0, ez))
        m.sphere(tool, "Metal", (0.4 + dx * 1.3, 0.645, dz * 1.3), (0.04, 0.04, 0.04))

    # Legs: trousers, knee pads, chunky boots with soles
    def leg(side, x):
        lg = m.empty("Leg " + side, m.root, (x, 0.92, 0))
        m.capsule(lg, "SuitOrange", (x, 0.55, 0), 0.13, 0.82)
        m.sphere(lg, "SuitOrangeDark", (x, 0.52, 0.1), (0.2, 0.18, 0.12))
        m.cyl(lg, "SuitOrangeDark", (x, 0.24, 0), 0.135, 0.06)
        m.box(lg, "Rubber", (x, 0.12, 0.04), (0.26, 0.2, 0.36), bevel=0.07)
        m.box(lg, "MetalDark", (x, 0.025, 0.04), (0.28, 0.05, 0.4), bevel=0.02)
        m.box(lg, "SuitGrey", (x, 0.15, 0.19), (0.16, 0.06, 0.04), bevel=0.015)
        m.empty("Foot " + side, lg, (x, 0.0, 0.04))
    leg("L", -0.14)
    leg("R", 0.14)
    m.export("Rookie.fbx")


# ───────────────────────── SCRAP-BOT ─────────────────────────

def build_scrapbot():
    reset()
    m = Model("ScrapBot")
    r = m.root
    # rounded-box head
    m.box(r, "BotWhite", (0, 0.18, 0), (0.62, 0.48, 0.48), bevel=0.14)
    m.box(r, "VisorBlack", (0, 0.17, 0.2), (0.46, 0.3, 0.1), bevel=0.06)
    m.box(r, "BotEye", (-0.09, 0.2, 0.25), (0.11, 0.04, 0.02), bevel=0.01, euler=(0, 0, 14))
    m.box(r, "BotEye", (0.09, 0.2, 0.25), (0.11, 0.04, 0.02), bevel=0.01, euler=(0, 0, -14))
    m.box(r, "BotEye", (0, 0.11, 0.25), (0.08, 0.02, 0.02), bevel=0.008)               # small mouth line
    m.box(r, "BotYellow", (0, 0.43, 0.02), (0.18, 0.05, 0.3), bevel=0.02)
    m.cyl(r, "MetalDark", (0.18, 0.5, -0.08), 0.012, 0.14)
    m.sphere(r, "BotEye", (0.18, 0.58, -0.08), (0.045, 0.045, 0.045))
    for x in (-0.315, 0.315):
        m.cyl(r, "BotYellow", (x, 0.18, 0), 0.1, 0.05, axis="x")
        m.cyl(r, "Rubber", (x * 1.07, 0.18, 0), 0.05, 0.03, axis="x")
        m.sphere(r, "Rubber", (x * 0.8, -0.1, 0), (0.12, 0.12, 0.12))
        m.capsule(r, "BotWhite", (x * 1.06, -0.27, 0.01), 0.06, 0.3, squash=(1.0, 0.75), euler=(0, 0, 20 if x > 0 else -20))
    m.cyl(r, "Rubber", (0, -0.02, 0), 0.1, 0.08)                                          # neck
    m.sphere(r, "BotYellow", (0, -0.2, 0), (0.42, 0.32, 0.36))
    m.torus(r, "MetalDark", (0, -0.2, 0), 0.2, 0.015)
    m.sphere(r, "BotEye", (0, -0.17, 0.17), (0.09, 0.09, 0.05))
    m.capsule(r, "BotWhite", (0, -0.42, 0), 0.14, 0.3, squash=(1.0, 0.9))
    m.cyl(r, "MetalDark", (0, -0.56, 0), 0.07, 0.06, radius2=0.04)                        # thruster
    m.sphere(r, "BotEye", (0, -0.6, 0), (0.06, 0.03, 0.06))

    # Cleaning broom held in the right flipper, angled down and forward
    hand = (0.37, -0.4, 0.06)
    top = (0.33, -0.1, -0.06)
    foot = (0.56, -1.75, 0.52)
    rot = m.rod(r, "Metal", top, foot, 0.022)
    m.sphere(r, "Rubber", hand, (0.09, 0.09, 0.09))                                       # grip
    m.sphere(r, "TrimOrange", top, (0.05, 0.05, 0.05))                                    # end cap
    d = (U(*foot) - U(*top)).normalized()
    head = U(*foot)
    bristle = head + d * 0.07
    m.box_along(r, "TrimOrange", (head.x, head.y, -head.z), rot, (0.42, 0.1, 0.08), bevel=0.025)
    m.box_along(r, "TrimYellow", (bristle.x, bristle.y, -bristle.z), rot, (0.4, 0.08, 0.1), bevel=0.01)
    m.export("ScrapBot.fbx")


# ───────────────────────── NPCs ─────────────────────────

def human_base(m, suit, suit_dark, boots="Rubber"):
    """Shared NPC body: torso, legs, arms. Returns (body, head, arm_l, arm_r)."""
    body = m.empty("Body", m.root, (0, 0, 0))
    m.capsule(body, suit, (0, 1.27, 0), 0.27, 0.74, squash=(1.0, 0.72))
    m.cyl(body, suit_dark, (0, 1.0, 0), 0.29, 0.09)
    for x in (-0.1, 0.1):
        m.capsule(body, suit, (x, 0.55, 0), 0.115, 0.82)
        m.box(body, boots, (x, 0.09, 0.04), (0.2, 0.18, 0.32), bevel=0.06)
    head = m.empty("Head", body, (0, 1.78, 0))
    m.sphere(head, "Skin", (0, 1.78, 0.01), (0.27, 0.32, 0.3))
    m.sphere(head, "Skin", (0, 1.73, 0.13), (0.06, 0.06, 0.06))                           # nose
    for x in (-0.06, 0.06):
        m.sphere(head, "VisorBlack", (x, 1.8, 0.14), (0.04, 0.05, 0.03))
    m.box(head, "Leather", (0, 1.69, 0.13), (0.08, 0.015, 0.02), bevel=0.006)           # mouth
    m.cyl(body, "Skin", (0, 1.6, 0), 0.075, 0.12)
    arm_l = m.empty("Arm L", body, (-0.34, 1.48, 0))
    arm_r = m.empty("Arm R", body, (0.34, 1.48, 0))
    return body, head, arm_l, arm_r


def build_nova():
    reset()
    m = Model("Nova")
    body, head, arm_l, arm_r = human_base(m, "TrimTeal", "TealDark")
    # orange armour: chest plate, shoulder guards, knee guards
    m.box(body, "TrimOrange", (0, 1.34, 0.17), (0.42, 0.34, 0.1), bevel=0.05)
    m.box(body, "TrimYellow", (0, 1.34, 0.225), (0.1, 0.1, 0.02), bevel=0.02)
    for x in (-0.1, 0.1):
        m.sphere(body, "TrimOrange", (x, 0.52, 0.09), (0.16, 0.15, 0.09))
    m.box(body, "Leather", (0.22, 0.95, 0.12), (0.12, 0.14, 0.06), bevel=0.02)         # tool pouch
    # red hair: back volume and a side-swept fringe, plus a goggle band
    m.sphere(head, "Hair", (0, 1.86, -0.04), (0.31, 0.27, 0.3))
    m.sphere(head, "Hair", (0, 1.73, -0.12), (0.26, 0.3, 0.16))
    m.sphere(head, "Hair", (0.06, 1.92, 0.1), (0.2, 0.08, 0.14), euler=(0, 0, -12))
    m.torus(head, "MetalDark", (0, 1.86, 0.0), 0.15, 0.018, euler=(-10, 0, 0))
    for x in (-0.065, 0.065):
        m.cyl(head, "MetalDark", (x, 1.9, 0.14), 0.05, 0.04, axis="z")
        m.cyl(head, "Glass", (x, 1.9, 0.16), 0.038, 0.01, axis="z")
    for arm, x, sx in ((arm_l, -0.34, -1), (arm_r, 0.34, 1)):
        m.sphere(arm, "TrimOrange", (x, 1.5, 0), (0.22, 0.17, 0.22))
        m.capsule(arm, "TrimTeal", (x + 0.02 * sx, 1.22, 0), 0.085, 0.5)
        m.sphere(arm, "Leather", (x + 0.03 * sx, 0.97, 0.01), (0.12, 0.14, 0.11))
    # a spanner in the right hand
    m.box(arm_r, "Metal", (0.39, 0.92, 0.1), (0.04, 0.03, 0.3), bevel=0.01)
    m.torus(arm_r, "Metal", (0.39, 0.92, 0.26), 0.04, 0.014)
    m.export("Nova.fbx")


def build_bolt():
    reset()
    m = Model("CaptainBolt")
    body, head, arm_l, arm_r = human_base(m, "SuitOrange", "SuitOrangeDark")
    # bigger frame, teal patches, gold badge, yellow shoulder pads
    m.capsule(body, "SuitOrange", (0, 1.3, 0), 0.31, 0.6, squash=(1.0, 0.8))
    for x in (-0.12, 0.12):
        m.box(body, "TrimTeal", (x, 1.36, 0.23), (0.12, 0.12, 0.03), bevel=0.015)
        m.box(body, "TrimTeal", (x, 0.6, 0.11), (0.1, 0.12, 0.03), bevel=0.015)
    m.cyl(body, "Gold", (-0.13, 1.44, 0.25), 0.045, 0.02, axis="z")
    m.box(body, "Leather", (0, 1.0, 0.27), (0.12, 0.08, 0.03), bevel=0.01)
    # grey beard and cap with headset
    m.sphere(head, "HairGrey", (0, 1.68, 0.09), (0.24, 0.15, 0.16))
    m.box(head, "HairGrey", (0, 1.735, 0.15), (0.14, 0.035, 0.025), bevel=0.012)        # moustache
    m.sphere(head, "TrimTeal", (0, 1.9, 0), (0.3, 0.16, 0.3))
    m.box(head, "TrimTeal", (0, 1.88, 0.16), (0.24, 0.025, 0.12), bevel=0.01)            # cap peak
    m.torus(head, "Rubber", (0, 1.83, 0), 0.155, 0.014, axis="z")
    for x in (-0.155, 0.155):
        m.cyl(head, "Rubber", (x, 1.78, 0), 0.06, 0.05, axis="x")
    m.cyl(head, "Rubber", (-0.165, 1.75, 0.07), 0.009, 0.14, axis="z")                  # mic boom
    m.sphere(head, "Rubber", (-0.165, 1.75, 0.145), (0.035, 0.035, 0.035))
    for arm, x in ((arm_l, -0.36), (arm_r, 0.36)):
        m.box(arm, "TrimYellow", (x, 1.53, 0), (0.24, 0.1, 0.26), bevel=0.04)
        m.capsule(arm, "SuitOrange", (x, 1.24, 0), 0.095, 0.5)
    m.sphere(arm_l, "Leather", (-0.36, 0.98, 0.01), (0.13, 0.14, 0.12))
    # yellow robot hand on the right
    m.cyl(arm_r, "Metal", (0.36, 1.02, 0), 0.075, 0.06)
    m.box(arm_r, "TrimYellow", (0.36, 0.94, 0.0), (0.13, 0.12, 0.1), bevel=0.03)
    for dx in (-0.04, 0.0, 0.04):
        m.capsule(arm_r, "MetalDark", (0.36 + dx, 0.86, 0.02), 0.018, 0.09)
    m.export("CaptainBolt.fbx")


# ───────────────────────── SH-01 salvage truck ─────────────────────────

def build_truck():
    reset()
    m = Model("SH01Truck")
    r = m.root
    # chassis and white body with an orange belt-line stripe
    m.box(r, "MetalDark", (0, 0.62, 0), (2.0, 0.3, 4.6), bevel=0.05)
    m.box(r, "BaseWhite", (0, 1.15, 1.35), (2.3, 1.05, 2.1), bevel=0.18)                 # cab lower
    m.box(r, "BaseWhite", (0, 1.95, 1.2), (2.2, 0.75, 1.7), bevel=0.22)                  # cab upper
    m.box(r, "Glass", (0, 1.98, 2.02), (1.95, 0.55, 0.08), bevel=0.04, euler=(-12, 0, 0))
    for x in (-1.11, 1.11):
        m.box(r, "Glass", (x, 2.0, 1.25), (0.06, 0.5, 1.2), bevel=0.03)
    m.box(r, "TrimOrange", (0, 1.25, 1.35), (2.34, 0.26, 2.14), bevel=0.06)
    m.box(r, "Rubber", (0, 0.75, 2.42), (2.2, 0.32, 0.16), bevel=0.05)                   # bumper
    for x in (-0.78, 0.78):
        m.box(r, "Headlight", (x, 1.05, 2.42), (0.36, 0.18, 0.04), bevel=0.03)
    m.box(r, "MetalDark", (0, 1.02, 2.42), (0.7, 0.24, 0.04), bevel=0.02)                # grille
    # light bar on the cab roof
    m.box(r, "MetalDark", (0, 2.38, 1.2), (1.4, 0.08, 0.28), bevel=0.02)
    for i, x in enumerate((-0.5, -0.17, 0.17, 0.5)):
        m.box(r, "TrimOrange" if i % 3 == 0 else "Headlight", (x, 2.47, 1.2), (0.28, 0.12, 0.2), bevel=0.03)
    # rear deck and rusty scrap bin with ribs
    m.box(r, "BaseWhite", (0, 1.0, -1.15), (2.3, 0.5, 2.6), bevel=0.1)
    m.box(r, "TrimOrange", (0, 1.0, -1.15), (2.34, 0.18, 2.64), bevel=0.04)
    m.box(r, "Rust", (0, 1.75, -1.2), (2.1, 1.0, 2.3), bevel=0.06)
    m.box(r, "RustDark", (0, 2.2, -1.2), (1.9, 0.12, 2.1), bevel=0.02)                  # scrap inside
    for z in (-2.2, -1.2, -0.2):
        for x in (-1.06, 1.06):
            m.box(r, "RustDark", (x, 1.75, z), (0.06, 1.0, 0.1), bevel=0.015)
    # magnet crane behind the cab
    m.cyl(r, "TrimYellow", (0.7, 1.5, 0.05), 0.18, 0.3)
    m.box(r, "TrimYellow", (0.7, 2.33, -0.28), (0.16, 0.16, 1.5), bevel=0.04, euler=(64, 0, 0))
    m.sphere(r, "MetalDark", (0.7, 3.0, -0.6), (0.2, 0.2, 0.2))
    m.box(r, "TrimYellow", (0.7, 2.95, -1.05), (0.14, 0.14, 0.95), bevel=0.04, euler=(-6, 0, 0))
    m.cyl(r, "MetalDark", (0.7, 2.73, -1.5), 0.012, 0.34)
    m.cyl(r, "MetalDark", (0.7, 2.52, -1.5), 0.32, 0.12)
    m.cyl(r, "TrimOrange", (0.7, 2.45, -1.5), 0.33, 0.03)
    # wheels (spin about their own X axis) with hubs and arches
    for (x, z, name) in ((-1.15, 1.6, "Wheel FL"), (1.15, 1.6, "Wheel FR"), (-1.15, -1.6, "Wheel RL"), (1.15, -1.6, "Wheel RR")):
        w = m.empty(name, r, (x, 0.45, z))
        m.cyl(w, "Rubber", (x, 0.45, z), 0.45, 0.36, axis="x", segs=32, bevel=0.06)
        m.cyl(w, "Metal", (x + (0.17 if x > 0 else -0.17), 0.45, z), 0.24, 0.05, axis="x")
        m.cyl(w, "TrimOrange", (x + (0.2 if x > 0 else -0.2), 0.45, z), 0.08, 0.04, axis="x")
        m.box(r, "BaseWhite", (x * 0.97, 0.95, z), (0.42, 0.12, 1.15), bevel=0.05)
    m.export("SH01Truck.fbx")


if __name__ == "__main__":
    build_rookie()
    build_scrapbot()
    build_nova()
    build_bolt()
    build_truck()
    print("[models] done")
