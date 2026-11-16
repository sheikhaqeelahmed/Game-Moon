"""
Planet Scrappers - procedural props for the later zones (art pass 2b).

Run headless (Blender 4.2+):
    blender -b --factory-startup --python tools/blender/build_props_late.py

Iron District, Spaceport Graveyard, Deep Core and Mega-Factory props. Same conventions as
build_props.py: Unity coordinates, part groups P00..Pnn removed in order by destruction,
"<Part>__<MaterialKey>" mesh names. Sizes match the gameplay colliders in the Unity builder.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_models import reset  # noqa: E402
from build_props import Prop, PROPS  # noqa: E402


def streaks(p, group, spots, mat="RustDark"):
    """Thin vertical rust streaks: (x, top_y, z, length, face) with face 'x' or 'z'."""
    for (x, top, z, length, face) in spots:
        size = (0.02, length, 0.08) if face == "x" else (0.08, length, 0.02)
        p.box(group, mat, (x, top - length / 2, z), size, bevel=0.0)


# ───────────────────────── Iron District ─────────────────────────

def robot_arm():
    reset()
    p = Prop("RobotArm")
    base = p.g(0, "Base")
    p.cyl(base, "MetalDark", (0, 0.3, 0), 0.7, 0.6, segs=36, bevel=0.05)
    for k in range(8):
        a = math.radians(k * 45)
        p.cyl(base, "Metal", (0.58 * math.cos(a), 0.62, 0.58 * math.sin(a)), 0.05, 0.05)
    turret = p.g(1, "Turret")
    p.cyl(turret, "Hazard", (0, 0.8, 0), 0.45, 0.5, segs=32, bevel=0.05)
    p.box(turret, "Hazard", (0, 1.12, 0.1), (0.6, 0.35, 0.6), bevel=0.1)
    p.cyl(turret, "MetalDark", (0, 1.15, 0.1), 0.2, 0.7, axis="x")
    lower = p.g(2, "Lower Arm")
    p.box(lower, "Hazard", (0, 1.9, 0.3), (0.4, 2.0, 0.4), bevel=0.1, euler=(18, 0, 0))
    p.rod(lower, "Metal", (0.26, 1.2, 0.15), (0.26, 2.5, 0.55), 0.05)
    p.rod(lower, "MetalDark", (0.26, 1.2, 0.15), (0.26, 1.85, 0.35), 0.075)
    elbow = p.g(3, "Elbow")
    p.sphere(elbow, "MetalDark", (0, 2.9, 0.65), (0.55, 0.55, 0.55))
    p.cyl(elbow, "Metal", (0, 2.9, 0.65), 0.18, 0.62, axis="x")
    upper = p.g(4, "Upper Arm")
    p.box(upper, "Hazard", (0, 2.75, 1.45), (0.32, 0.32, 1.6), bevel=0.08, euler=(-12, 0, 0))
    p.cyl(upper, "MetalDark", (0, 2.58, 2.22), 0.16, 0.2, axis="z", euler=(-12, 0, 0))
    gripper = p.g(5, "Gripper")
    for x in (-0.15, 0.15):
        p.box(gripper, "MetalDark", (x, 2.45, 2.25), (0.08, 0.35, 0.12), bevel=0.02, euler=(0, 0, 10 if x < 0 else -10))
    p.rod(gripper, "Rubber", (0.25, 0.9, -0.1), (0.25, 2.1, 0.4), 0.04)
    p.rod(gripper, "Rubber", (0.2, 2.95, 0.7), (0.2, 2.75, 1.9), 0.035)
    rust = p.g(6, "Rust")
    streaks(p, rust, [(0.21, 2.2, 0.3, 0.6, "x"), (-0.46, 0.95, 0.0, 0.25, "x")])
    p.save(PROPS, "RobotArm.fbx")


def hydraulic_press():
    reset()
    p = Prop("HydraulicPress")
    bed = p.g(0, "Bed")
    p.box(bed, "MetalDark", (0, 0.5, 0), (3.4, 1.0, 2.0), bevel=0.08)
    p.box(bed, "Metal", (0, 1.03, 0), (2.4, 0.06, 1.3), bevel=0.02)
    pillars = p.g(1, "Pillars")
    for x in (-1.5, 1.5):
        p.box(pillars, "LockedGrey", (x, 2.6, 0), (0.5, 4.2, 1.2), bevel=0.08)
        for k in range(4):
            p.box(pillars, "Hazard" if k % 2 == 0 else "Rubber", (x, 0.95 + k * 0.18, 0.61), (0.5, 0.18, 0.02), bevel=0.0)
    crown = p.g(2, "Crown")
    p.box(crown, "LockedGrey", (0, 4.9, 0), (3.6, 0.9, 1.5), bevel=0.1)
    p.cyl(crown, "Metal", (0, 4.2, 0), 0.25, 1.0)
    p.cyl(crown, "MetalDark", (0, 5.5, 0), 0.45, 0.4)
    ram = p.g(3, "Ram")
    p.box(ram, "Hazard", (0, 3.4, 0), (2.2, 1.2, 1.1), bevel=0.08)
    for x in (-0.8, 0.8):
        p.rod(ram, "Rubber", (x, 4.0, 0.5), (x * 1.6, 4.6, 0.7), 0.05)
    control = p.g(4, "Control")
    p.box(control, "VendingRed", (2.3, 1.4, 0.6), (0.6, 0.9, 0.4), bevel=0.06)
    p.box(control, "Screen", (2.3, 1.6, 0.81), (0.4, 0.22, 0.02), bevel=0.01)
    for k, col in enumerate(("BotEye", "Hazard", "TrimOrange")):
        p.sphere(control, col, (2.15 + k * 0.15, 1.25, 0.82), (0.07, 0.07, 0.04))
    p.rod(control, "MetalDark", (2.3, 0.0, 0.6), (2.3, 0.95, 0.6), 0.05)
    rust = p.g(5, "Rust")
    streaks(p, rust, [(-1.76, 3.0, 0.2, 1.4, "x"), (1.76, 4.4, -0.3, 0.9, "x"), (0.5, 1.0, 1.01, 0.4, "z")])
    p.save(PROPS, "HydraulicPress.fbx")


def assembly_line():
    reset()
    p = Prop("AssemblyLine")
    frame = p.g(0, "Frame")
    p.box(frame, "MetalDark", (0, 0.7, 0), (1.8, 0.4, 22.0), bevel=0.06)
    belt = p.g(1, "Belt")
    p.box(belt, "Rubber", (0, 0.92, 0), (1.5, 0.04, 21.6), bevel=0.01)
    for x in (-0.86, 0.86):
        p.box(belt, "Hazard", (x, 0.95, 0), (0.08, 0.1, 22.0), bevel=0.01)
    for z in (-10.8, 10.8):
        p.cyl(belt, "Metal", (0, 0.82, z), 0.16, 1.6, axis="x")
    legs = p.g(2, "Legs")
    for i in range(-5, 6):
        for x in (-0.8, 0.8):
            p.box(legs, "Metal", (x, 0.25, i * 2.0), (0.12, 0.5, 0.12), bevel=0.02)
            p.box(legs, "MetalDark", (x, 0.02, i * 2.0), (0.25, 0.04, 0.25), bevel=0.01)
    crates = p.g(3, "Crates")
    for i in range(-2, 3):
        mat = "Rust" if i % 2 == 0 else "ContainerTeal"
        z = i * 4.0 + 1.0
        p.box(crates, mat, (0, 1.2, z), (0.9, 0.55, 0.9), bevel=0.05)
        p.box(crates, "MetalDark", (0, 1.2, z + 0.46), (0.5, 0.2, 0.02), bevel=0.0)
    gantry = p.g(4, "Gantry")
    for x in (-1.4, 1.4):
        p.box(gantry, "Hazard", (x, 2.0, -4.0), (0.3, 4.0, 0.3), bevel=0.05)
    p.box(gantry, "Hazard", (0, 4.0, -4.0), (3.1, 0.35, 0.4), bevel=0.06)
    p.box(gantry, "MetalDark", (0, 3.5, -4.0), (0.7, 0.5, 0.5), bevel=0.06)
    p.box(gantry, "Screen", (0, 3.5, -3.74), (0.5, 0.3, 0.02), bevel=0.01)
    p.sphere(gantry, "TrimOrange", (1.4, 4.3, -4.0), (0.22, 0.22, 0.22))
    rust = p.g(5, "Rust")
    streaks(p, rust, [(0.91, 0.88, 5.0, 0.3, "x"), (0.91, 0.88, -7.0, 0.25, "x"), (-0.91, 0.88, 2.0, 0.3, "x")])
    p.save(PROPS, "AssemblyLine.fbx")


def factory_crane():
    reset()
    p = Prop("FactoryCrane")
    beam = p.g(0, "Main Beam")
    p.box(beam, "Hazard", (0, 8.6, 0), (10.0, 0.8, 1.0), bevel=0.08)
    for k in range(-4, 5):
        p.box(beam, "MetalDark", (k * 1.1, 8.6, 0.51), (0.08, 0.6, 0.02), bevel=0.0, euler=(0, 0, 40))
    legs = p.g(1, "Legs")
    for x in (-4.6, 4.6):
        for z in (-3.0, 3.0):
            p.box(legs, "LockedGrey", (x, 4.3, z), (0.5, 8.6, 0.5), bevel=0.06)
            p.box(legs, "MetalDark", (x, 0.1, z), (0.9, 0.2, 0.9), bevel=0.03)
            for k in range(3):
                p.box(legs, "Hazard", (x, 0.6 + k * 0.5, z), (0.52, 0.22, 0.52), bevel=0.0)
        p.rod(legs, "LockedGrey", (x, 1.0, -3.0), (x, 7.5, 3.0), 0.08)
    sides = p.g(2, "Side Beams")
    for x in (-4.6, 4.6):
        p.box(sides, "LockedGrey", (x, 8.6, 0), (0.6, 0.6, 6.5), bevel=0.06)
    trolley = p.g(3, "Trolley")
    p.box(trolley, "MetalDark", (1.2, 8.0, 0), (1.4, 0.8, 1.4), bevel=0.1)
    for z in (-0.5, 0.5):
        p.cyl(trolley, "Metal", (1.2, 8.45, z), 0.15, 1.5, axis="x")
    hook = p.g(4, "Hook")
    p.rod(hook, "Rubber", (1.05, 7.6, 0), (1.05, 3.35, 0), 0.04)
    p.rod(hook, "Rubber", (1.35, 7.6, 0), (1.35, 3.35, 0), 0.04)
    p.box(hook, "Hazard", (1.2, 3.0, 0), (0.6, 0.7, 0.4), bevel=0.08)
    p.torus(hook, "Metal", (1.2, 2.45, 0), 0.22, 0.06, axis="z")
    rust = p.g(5, "Rust")
    streaks(p, rust, [(-1.5, 8.9, -0.51, 0.5, "z"), (2.0, 8.9, -0.51, 0.4, "z"), (4.86, 6.0, 3.0, 1.6, "x")])
    p.save(PROPS, "FactoryCrane.fbx")


# ───────────────────────── Spaceport Graveyard ─────────────────────────

def cargo_pod():
    reset()
    p = Prop("CargoPod")
    pod = p.g(0, "Pod")
    p.capsule(pod, "ContainerBlue", (0, 1.2, 0), 1.1, 4.4, axis="x")
    band = p.g(1, "Band")
    p.cyl(band, "Hazard", (0, 1.2, 0), 1.15, 0.4, axis="x", segs=40)
    for x in (-1.6, 1.6):
        p.cyl(band, "MetalDark", (x, 1.2, 0), 1.12, 0.12, axis="x", segs=40)
    hatch = p.g(2, "Hatch")
    p.box(hatch, "BaseWhite", (0.9, 1.75, 0.78), (0.9, 0.6, 0.05), bevel=0.03, euler=(-45, 0, 0))
    p.box(hatch, "MetalDark", (0.9, 1.75, 0.82), (0.4, 0.08, 0.04), bevel=0.01, euler=(-45, 0, 0))
    skids = p.g(3, "Skids")
    for z in (-0.6, 0.6):
        p.box(skids, "MetalDark", (0, 0.12, z), (3.6, 0.24, 0.2), bevel=0.04)
    rust = p.g(4, "Rust")
    p.box(rust, "RustDark", (1.4, 2.27, 0), (1.0, 0.03, 0.5), bevel=0.0)
    p.box(rust, "RustDark", (-1.0, 2.0, 0.7), (0.4, 0.5, 0.03), bevel=0.0, euler=(-40, 0, 0))
    p.save(PROPS, "CargoPod.fbx")


def ship_nose():
    reset()
    p = Prop("ShipNose")
    body = p.g(0, "Cockpit")
    p.cyl(body, "BaseWhite", (0, 3.2, 0), 3.5, 6.0, axis="x", segs=48, bevel=0.2)
    p.sphere(body, "BaseWhite", (-3.0, 3.2, 0), (5.4, 6.8, 6.8), segs=48)
    canopy = p.g(1, "Canopy")
    p.sphere(canopy, "Glass", (-3.2, 5.0, 0), (3.6, 2.2, 3.8), segs=40)
    p.torus(canopy, "MetalDark", (-3.2, 5.0, 0), 1.85, 0.08, axis="y")
    stripe = p.g(2, "Stripe")
    p.cyl(stripe, "TrimOrange", (0.6, 3.2, 0), 3.55, 0.7, axis="x", segs=48)
    p.cyl(stripe, "TrimOrange", (-0.4, 3.2, 0), 3.55, 0.2, axis="x", segs=48)
    broken = p.g(3, "Broken Edge")
    p.torus(broken, "MetalDark", (3.0, 3.2, 0), 3.4, 0.18, axis="x")
    for k in range(6):
        a = math.radians(k * 60 + 15)
        p.box(broken, "LockedGrey", (3.3, 3.2 + 3.1 * math.sin(a), 3.1 * math.cos(a)), (0.6, 0.9, 0.12), bevel=0.02, euler=(math.degrees(a), 0, 20))
    p.rod(broken, "Rubber", (3.0, 2.4, 1.0), (3.9, 0.1, 1.6), 0.07)
    p.rod(broken, "Rubber", (3.0, 4.0, -0.6), (4.2, 0.1, -1.2), 0.06)
    rust = p.g(4, "Rust")
    p.box(rust, "RustDark", (1.0, 1.0, -3.2), (3.0, 1.0, 0.3), bevel=0.05)
    p.save(PROPS, "ShipNose.fbx")


def ship_hull():
    reset()
    p = Prop("ShipHull")
    hull = p.g(0, "Hull")
    p.cyl(hull, "TruckGrey", (0, 3.6, 0), 4.0, 18.0, axis="x", segs=48, bevel=0.1)
    ribs = p.g(1, "Ribs")
    for k in range(-3, 4):
        p.torus(ribs, "MetalDark", (k * 2.4, 3.6, 0), 4.05, 0.14, axis="x")
    door = p.g(2, "Cargo Door")
    p.box(door, "Rust", (0, 3.0, 3.98), (6.0, 3.6, 0.12), bevel=0.05)
    for k in range(-2, 3):
        p.box(door, "RustDark", (k * 1.2, 3.0, 4.05), (0.08, 3.4, 0.03), bevel=0.0)
    breach = p.g(3, "Breach")
    p.box(breach, "Rubber", (2.0, 7.2, 1.8), (3.0, 0.8, 2.0), bevel=0.2, euler=(-25, 0, 0))
    for k in range(5):
        p.box(breach, "LockedGrey", (0.7 + k * 0.65, 7.45, 0.6 + (k % 2) * 0.3), (0.5, 0.1, 0.9), bevel=0.02, euler=(30 + k * 7, k * 12, 0))
    marks = p.g(4, "Markings")
    p.box(marks, "BaseWhite", (-6.0, 6.4, 2.2), (2.5, 0.6, 0.05), bevel=0.01, euler=(-33, 0, 0))
    p.box(marks, "TrimOrange", (6.0, 3.6, 4.02), (3.0, 0.5, 0.05), bevel=0.01)
    streaks(p, marks, [(-3.0, 3.0, 4.03, 1.2, "z"), (4.0, 2.8, 4.03, 1.6, "z"), (-7.5, 3.4, -4.03, 1.4, "z")])
    p.save(PROPS, "ShipHull.fbx")


def ship_engine():
    reset()
    p = Prop("ShipEngine")
    block = p.g(0, "Block")
    p.cyl(block, "LockedGrey", (0, 3.6, 0), 3.7, 8.0, axis="x", segs=48, bevel=0.15)
    p.torus(block, "MetalDark", (-4.0, 3.6, 0), 3.6, 0.2, axis="x")
    nozzles = p.g(1, "Nozzles")
    for (dy, dz) in ((1.8, 1.8), (1.8, -1.8), (-1.8, 1.8), (-1.8, -1.8)):
        p.cyl(nozzles, "MetalDark", (5.2, 3.6 + dy, dz), 1.0, 2.8, axis="x", radius2=1.25, segs=32, bevel=0.0)
        p.cyl(nozzles, "Rubber", (6.62, 3.6 + dy, dz), 1.05, 0.04, axis="x", segs=32, bevel=0.0)
    reactor = p.g(2, "Reactor")
    p.sphere(reactor, "Crystal", (0, 6.6, 0), (1.3, 1.3, 1.3))
    for axis in ("x", "z", "y"):
        p.torus(reactor, "Metal", (0, 6.6, 0), 0.8, 0.06, axis=axis)
    p.cyl(reactor, "MetalDark", (0, 7.2, 0), 0.9, 0.3)
    pipes = p.g(3, "Pipes")
    for z in (-2.2, 2.2):
        p.rod(pipes, "Metal", (-3.5, 5.6, z), (3.5, 5.6, z), 0.18)
    p.rod(pipes, "Rubber", (-2.0, 6.8, 0.6), (-4.6, 0.1, 3.0), 0.1)
    rust = p.g(4, "Rust")
    p.box(rust, "RustDark", (0, 1.4, 3.3), (4.0, 1.4, 0.6), bevel=0.05)
    streaks(p, rust, [(-1.5, 6.0, 3.6, 2.0, "z"), (2.4, 6.4, -3.5, 1.6, "z")])
    p.save(PROPS, "ShipEngine.fbx")


def ship_wing():
    """Authored flat at the origin; the Unity builder tilts it per placement."""
    reset()
    p = Prop("ShipWing")
    wing = p.g(0, "Wing")
    p.box(wing, "BaseWhite", (0, 0, 0), (14.0, 0.8, 9.0), bevel=0.25)
    p.box(wing, "BaseWhite", (-7.5, 0, 0), (1.6, 0.6, 7.0), bevel=0.25)
    flap = p.g(1, "Flap")
    p.box(flap, "TrimOrange", (5.5, -0.05, 0), (3.0, 0.5, 8.0), bevel=0.1)
    seams = p.g(2, "Seams")
    for k in range(-2, 3):
        p.box(seams, "SuitGrey", (k * 2.6, 0.41, 0), (0.06, 0.02, 8.6), bevel=0.0)
    p.sphere(seams, "TrimOrange", (-8.3, 0, 3.2), (0.4, 0.4, 0.4))
    p.sphere(seams, "BotEye", (-8.3, 0, -3.2), (0.4, 0.4, 0.4))
    broken = p.g(3, "Broken")
    for k in range(5):
        p.box(broken, "LockedGrey", (7.1, 0.1 * (k % 2), -3.6 + k * 1.8), (0.4, 0.15, 1.2), bevel=0.02, euler=(0, 15 * (k - 2), 10))
    p.box(broken, "RustDark", (-3.0, 0.42, 1.2), (4.0, 0.03, 2.5), bevel=0.0)
    p.save(PROPS, "ShipWing.fbx")


def shuttle_cabin():
    reset()
    p = Prop("ShuttleCabin")
    cabin = p.g(0, "Cabin")
    p.box(cabin, "BaseWhite", (0, 2.2, 0), (8.0, 3.4, 4.4), bevel=0.8)
    windows = p.g(1, "Windows")
    p.box(windows, "Glass", (-3.98, 2.7, 0), (0.1, 1.0, 3.2), bevel=0.05)
    for z in (-2.21, 2.21):
        for k in range(3):
            p.box(windows, "Glass", (-1.5 + k * 1.4, 2.8, z), (0.8, 0.6, 0.04), bevel=0.05)
    stripe = p.g(2, "Stripe")
    for z in (-2.22, 2.22):
        p.box(stripe, "TrimOrange", (0, 2.0, z), (7.4, 0.5, 0.04), bevel=0.02)
    p.box(stripe, "LockedGrey", (2.5, 1.9, 2.23), (1.2, 2.2, 0.04), bevel=0.04)          # door
    skids = p.g(3, "Skids")
    for z in (-1.5, 1.5):
        p.box(skids, "MetalDark", (0, 0.25, z), (8.4, 0.3, 0.3), bevel=0.08)
        for x in (-2.5, 2.5):
            p.rod(skids, "Metal", (x, 0.35, z), (x, 0.65, z * 0.8), 0.08)
    thrusters = p.g(4, "Thrusters")
    for z in (-1.2, 1.2):
        p.cyl(thrusters, "MetalDark", (4.4, 2.2, z), 0.6, 1.0, axis="x", radius2=0.75, bevel=0.0)
    p.rod(thrusters, "MetalDark", (0, 3.9, 0), (0, 4.8, 0), 0.04)
    p.sphere(thrusters, "TrimOrange", (0, 4.85, 0), (0.15, 0.15, 0.15))
    p.save(PROPS, "ShuttleCabin.fbx")


# ───────────────────────── Deep Core ─────────────────────────

def drilling_rig():
    reset()
    p = Prop("DrillingRig")
    base = p.g(0, "Base")
    p.box(base, "MetalDark", (0, 0.6, 0), (5.0, 1.2, 5.0), bevel=0.12)
    for x in (-2.0, 2.0):
        p.box(base, "Hazard", (x, 1.21, 0), (0.6, 0.02, 4.6), bevel=0.0)
    tower = p.g(1, "Tower")
    for x in (-1.08, 1.08):
        for z in (-1.08, 1.08):
            p.rod(tower, "Hazard", (x, 1.2, z), (x * 0.9, 8.7, z * 0.9), 0.15)
    for y in (3.0, 5.5, 8.0):
        for (a, b) in (((-1, -1), (1, -1)), ((1, -1), (1, 1)), ((1, 1), (-1, 1)), ((-1, 1), (-1, -1))):
            s = 1.0 - (y - 1.2) / 7.5 * 0.1
            p.rod(tower, "Hazard", (a[0] * 1.08 * s, y, a[1] * 1.08 * s), (b[0] * 1.08 * s, y, b[1] * 1.08 * s), 0.06)
    top = p.g(2, "Top")
    p.box(top, "Hazard", (0, 9.0, 0), (2.6, 0.6, 2.6), bevel=0.08)
    p.cyl(top, "MetalDark", (0, 9.5, 0), 0.5, 0.5)
    p.sphere(top, "TrimOrange", (1.1, 9.5, 1.1), (0.25, 0.25, 0.25))
    drill = p.g(3, "Drill")
    p.cyl(drill, "Metal", (0, 4.6, 0), 0.3, 7.6, segs=24)
    for k in range(10):
        p.torus(drill, "MetalDark", (0, 1.5 + k * 0.7, 0), 0.32, 0.05)
    motor = p.g(4, "Motor")
    p.box(motor, "VendingRed", (1.8, 1.8, 1.8), (1.4, 1.2, 1.4), bevel=0.12)
    for k in range(4):
        p.box(motor, "MetalDark", (1.8, 1.5 + k * 0.2, 2.51), (1.0, 0.06, 0.02), bevel=0.0)
    p.rod(motor, "Metal", (1.4, 2.4, 1.4), (1.4, 3.2, 1.4), 0.1)
    rust = p.g(5, "Rust")
    streaks(p, rust, [(2.51, 1.15, 0.5, 0.8, "x"), (2.51, 1.15, -1.2, 0.5, "x"), (-0.5, 1.15, 2.51, 0.7, "z")])
    p.save(PROPS, "DrillingRig.fbx")


def ore_crusher():
    reset()
    p = Prop("OreCrusher")
    housing = p.g(0, "Housing")
    p.box(housing, "LockedGrey", (0, 2.0, 0), (6.0, 4.0, 4.0), bevel=0.15)
    p.box(housing, "MetalDark", (0, 0.12, 0), (6.3, 0.24, 4.3), bevel=0.04)
    hopper = p.g(1, "Hopper")
    p.box(hopper, "Hazard", (0, 4.6, 0), (4.0, 1.2, 3.0), bevel=0.08)
    for x in (-2.1, 2.1):
        p.box(hopper, "Hazard", (x, 5.4, 0), (0.15, 0.8, 3.2), bevel=0.03, euler=(0, 0, -25 if x < 0 else 25))
    p.box(hopper, "CaveRock", (0, 5.15, 0), (3.6, 0.2, 2.6), bevel=0.05)
    roller = p.g(2, "Roller")
    p.cyl(roller, "MetalDark", (0, 3.2, 2.05), 0.6, 2.6, axis="x", segs=24)
    for k in range(8):
        a = math.radians(k * 45)
        p.box(roller, "Metal", (0, 3.2 + 0.62 * math.sin(a), 2.05 + 0.62 * math.cos(a)), (2.4, 0.12, 0.12), bevel=0.02, euler=(-k * 45, 0, 0))
    chute = p.g(3, "Chute")
    p.box(chute, "Metal", (3.6, 1.0, 0), (1.6, 0.6, 1.6), bevel=0.05, euler=(0, 0, -15))
    p.box(chute, "CaveRock", (4.1, 0.5, 0), (0.9, 0.3, 1.0), bevel=0.1)
    panel = p.g(4, "Panel")
    p.box(panel, "MetalDark", (-1.5, 2.6, -2.02), (1.4, 1.0, 0.05), bevel=0.03)
    p.box(panel, "Screen", (-1.5, 2.7, -2.05), (1.0, 0.5, 0.02), bevel=0.01)
    for k in range(6):
        p.box(panel, "MetalDark", (1.2, 1.4 + k * 0.25, -2.02), (2.0, 0.08, 0.04), bevel=0.0)
    rust = p.g(5, "Rust")
    streaks(p, rust, [(-3.02, 3.4, 0.4, 1.6, "x"), (-3.02, 3.0, -1.1, 1.0, "x"), (3.02, 3.8, 1.2, 1.2, "x")])
    p.save(PROPS, "OreCrusher.fbx")


def crystal_vein():
    reset()
    p = Prop("CrystalVein")
    rock = p.g(0, "Rock")
    rng = random.Random(9)
    p.sphere(rock, "CaveRock", (0, 0.7, 0), (3.0, 1.8, 2.4), segs=10)
    for k in range(5):
        p.sphere(rock, "CaveRock", (rng.uniform(-1.2, 1.2), rng.uniform(0.3, 0.9), rng.uniform(-0.9, 0.9)),
                 (rng.uniform(0.8, 1.4), rng.uniform(0.6, 1.1), rng.uniform(0.8, 1.3)), segs=8)
    for group in (1, 2):
        g = p.g(group, "Crystals")
        for k in range(4):
            x, z = rng.uniform(-1.0, 1.0), rng.uniform(-0.7, 0.7)
            h = rng.uniform(0.9, 1.9)
            tilt = (rng.uniform(-22, 22), rng.uniform(0, 90), rng.uniform(-22, 22))
            r = rng.uniform(0.14, 0.24)
            base = (x, 1.0 + h / 2, z)
            p.cyl(g, "Crystal", base, r * 0.3, h, radius2=r, segs=6, bevel=0.0, euler=tilt)
    p.save(PROPS, "CrystalVein.fbx")


def mine_cart():
    reset()
    p = Prop("MineCart")
    tub = p.g(0, "Tub")
    p.box(tub, "Rust", (0, 1.0, 0), (1.6, 1.1, 2.4), bevel=0.1)
    p.box(tub, "MetalDark", (0, 1.57, 0), (1.7, 0.08, 2.5), bevel=0.03)
    for z in (-0.6, 0.0, 0.6):
        p.box(tub, "MetalDark", (0.81, 1.0, z), (0.03, 1.0, 0.1), bevel=0.0)
        p.box(tub, "MetalDark", (-0.81, 1.0, z), (0.03, 1.0, 0.1), bevel=0.0)
    ore = p.g(1, "Ore")
    rng = random.Random(4)
    for k in range(7):
        p.sphere(ore, "CaveRock", (rng.uniform(-0.5, 0.5), 1.62, rng.uniform(-0.9, 0.9)), (rng.uniform(0.4, 0.7), 0.4, rng.uniform(0.4, 0.7)), segs=8)
    p.cyl(ore, "Crystal", (0.2, 1.8, 0.3), 0.1, 0.4, segs=6, bevel=0.0, euler=(15, 0, -20))
    wheels = p.g(2, "Wheels")
    for x in (-0.7, 0.7):
        for z in (-0.8, 0.8):
            p.cyl(wheels, "MetalDark", (x, 0.3, z), 0.3, 0.12, axis="x", segs=24)
            p.cyl(wheels, "Metal", (x * 1.1, 0.3, z), 0.1, 0.06, axis="x")
    for z in (-0.8, 0.8):
        p.rod(wheels, "Metal", (-0.7, 0.3, z), (0.7, 0.3, z), 0.04)
    rails = p.g(3, "Rails")
    for x in (-0.6, 0.6):
        p.box(rails, "Metal", (x, 0.05, 0), (0.1, 0.1, 6.0), bevel=0.01)
    for k in range(-5, 6):
        p.box(rails, "Leather", (0, 0.02, k * 0.55), (1.6, 0.06, 0.22), bevel=0.01)
    rust = p.g(4, "Rust")
    streaks(p, rust, [(0.82, 1.5, 0.3, 0.5, "x"), (-0.82, 1.5, -0.9, 0.7, "x")])
    p.save(PROPS, "MineCart.fbx")


# ───────────────────────── Mega-Factory ─────────────────────────

def factory_pylon():
    reset()
    p = Prop("FactoryPylon")
    base = p.g(0, "Base")
    p.box(base, "MetalDark", (0, 1.0, 0), (3.6, 2.0, 3.6), bevel=0.15)
    column = p.g(1, "Column")
    p.box(column, "LockedGrey", (0, 6.0, 0), (1.6, 8.0, 1.6), bevel=0.12)
    for k in range(4):
        p.torus(column, "TrimOrange" if k % 2 else "Hazard", (0, 3.4 + k * 1.6, 0), 1.0, 0.12)
    orb = p.g(2, "Orb")
    p.sphere(orb, "BotEye", (0, 10.6, 0), (2.0, 2.0, 2.0))
    for axis in ("x", "z"):
        p.torus(orb, "Metal", (0, 10.6, 0), 1.15, 0.07, axis=axis)
    p.cyl(orb, "MetalDark", (0, 9.8, 0), 0.8, 0.4)
    stripes = p.g(3, "Stripes")
    for (x, z, ry) in ((0, 1.82, 0), (0, -1.82, 0), (1.82, 0, 90), (-1.82, 0, 90)):
        p.box(stripes, "Hazard", (x, 2.2, z), (3.0, 0.6, 0.05), bevel=0.01, euler=(0, ry, 0))
    conduits = p.g(4, "Conduits")
    for (x, z) in ((0.95, 0.95), (-0.95, -0.95)):
        p.rod(conduits, "Metal", (x, 2.0, z), (x * 0.9, 9.6, z * 0.9), 0.08)
    p.save(PROPS, "FactoryPylon.fbx")


def reactor_core():
    reset()
    p = Prop("ReactorCore")
    base = p.g(0, "Base")
    p.cyl(base, "MetalDark", (0, 1.5, 0), 7.0, 3.0, segs=64, bevel=0.2)
    p.cyl(base, "LockedGrey", (0, 3.15, 0), 5.6, 0.3, segs=64)
    body = p.g(1, "Body")
    p.cyl(body, "LockedGrey", (0, 7.0, 0), 4.5, 10.0, segs=64, bevel=0.2)
    for k in range(8):
        a = math.radians(k * 45)
        p.box(body, "MetalDark", (4.52 * math.cos(a), 7.0, 4.52 * math.sin(a)), (0.25, 8.0, 0.25), bevel=0.03)
    rings = p.g(2, "Rings")
    for i in range(4):
        p.torus(rings, "Hazard" if i % 2 == 0 else "TrimOrange", (0, 4.0 + i * 2.4, 0), 4.8, 0.3)
    heart = p.g(3, "Heart")
    p.cyl(heart, "MetalDark", (0, 12.2, 0), 3.0, 1.0, segs=48)
    p.sphere(heart, "Crystal", (0, 13.5, 0), (5.0, 5.0, 5.0), segs=40)
    for k in range(6):
        a = k * 30
        p.torus(heart, "Metal", (0, 13.5, 0), 2.65, 0.1, axis="x", euler=(0, a, 0))
    pipes = p.g(4, "Pipes")
    for k in range(6):
        a = math.radians(k * 60 + 30)
        c, s = math.cos(a), math.sin(a)
        p.rod(pipes, "Metal", (6.2 * c, 3.0, 6.2 * s), (4.6 * c, 9.0, 4.6 * s), 0.3)
        p.box(pipes, "Hazard", (6.4 * c, 3.2, 6.4 * s), (0.9, 0.4, 0.9), bevel=0.05)
    p.save(PROPS, "ReactorCore.fbx")


def smelter():
    reset()
    p = Prop("Smelter")
    furnace = p.g(0, "Furnace")
    p.cyl(furnace, "MetalDark", (0, 3.5, 0), 3.0, 7.0, segs=48, bevel=0.15)
    for k in range(5):
        p.torus(furnace, "LockedGrey", (0, 0.8 + k * 1.4, 0), 3.02, 0.08)
    crucible = p.g(1, "Crucible")
    p.cyl(crucible, "Rust", (0, 7.6, 0), 2.2, 1.6, radius2=2.5, segs=48, bevel=0.0)
    p.sphere(crucible, "TrimOrange", (0, 8.25, 0), (4.2, 0.5, 4.2))
    spout = p.g(2, "Spout")
    p.box(spout, "Metal", (3.4, 5.0, 0), (2.0, 0.6, 0.8), bevel=0.08, euler=(0, 0, -12))
    p.box(spout, "TrimOrange", (3.6, 5.12, 0), (1.6, 0.1, 0.5), bevel=0.02, euler=(0, 0, -12))
    p.box(spout, "MetalDark", (4.4, 1.5, 0), (1.4, 1.0, 1.4), bevel=0.1)
    hazard = p.g(3, "Hazard")
    p.cyl(hazard, "Hazard", (0, 1.2, 0), 3.06, 0.6, segs=48)
    for k in range(12):
        a = math.radians(k * 30)
        p.box(hazard, "Rubber", (3.08 * math.cos(a), 1.2, 3.08 * math.sin(a)), (0.25, 0.6, 0.04), bevel=0.0, euler=(0, -k * 30 + 90, 25))
    vent = p.g(4, "Vent")
    p.cyl(vent, "LockedGrey", (-1.8, 9.5, 0), 0.4, 3.2)
    p.torus(vent, "MetalDark", (-1.8, 11.1, 0), 0.42, 0.06)
    p.rod(vent, "Metal", (2.0, 0.2, 2.0), (2.2, 6.8, 1.2), 0.18)
    p.save(PROPS, "Smelter.fbx")


if __name__ == "__main__":
    robot_arm()
    hydraulic_press()
    assembly_line()
    factory_crane()
    cargo_pod()
    ship_nose()
    ship_hull()
    ship_engine()
    ship_wing()
    shuttle_cabin()
    drilling_rig()
    ore_crusher()
    crystal_vein()
    mine_cart()
    factory_pylon()
    reactor_core()
    smelter()
    print("[props-late] done")
