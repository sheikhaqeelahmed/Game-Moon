"""
Planet Scrappers - procedural props and buildings (art pass 2a: Rust Valley).

Run headless (Blender 4.2+):
    blender -b --factory-startup --python tools/blender/build_props.py

Writes FBX files to Assets/Art/Models/Props and Assets/Art/Models/Buildings, using the
same conventions as build_models.py (Unity coordinates, "<Part>__<MaterialKey>" mesh names).

Props are split into groups named P00, P01, ... The Unity builder sorts them by name, and
destruction removes them in that order: P00 is the main body that stays to the end, the
higher groups are the panels and details that come off first as an object is damaged.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_models as bm  # noqa: E402
from build_models import Model, reset  # noqa: E402

PROPS = os.path.join(bm.OUT, "Props")
BUILDINGS = os.path.join(bm.OUT, "Buildings")


class Prop(Model):
    def __init__(self, name):
        super().__init__(name)
        self.groups = {}

    def g(self, index, label):
        """Part group P<index> (an empty at the origin)."""
        key = f"P{index:02d} {label}"
        if key not in self.groups:
            self.groups[key] = self.empty(key, self.root, (0, 0, 0))
        return self.groups[key]

    def save(self, folder, filename):
        os.makedirs(folder, exist_ok=True)
        bm.OUT, old = folder, bm.OUT
        try:
            self.export(filename)
        finally:
            bm.OUT = old


# ───────────────────────── Tier 1 junk ─────────────────────────

def oil_drum():
    reset()
    p = Prop("OilDrum")
    body = p.g(0, "Body")
    p.cyl(body, "Rust", (0, 0.45, 0), 0.3, 0.88, segs=36, bevel=0.03)
    rings = p.g(1, "Rings")
    for y in (0.3, 0.6):
        p.torus(rings, "RustDark", (0, y, 0), 0.302, 0.02)
    p.torus(rings, "RustDark", (0, 0.885, 0), 0.285, 0.016)
    lid = p.g(2, "Lid")
    p.cyl(lid, "MetalDark", (0.13, 0.9, 0.06), 0.045, 0.04)
    p.cyl(lid, "MetalDark", (-0.14, 0.9, -0.05), 0.03, 0.03)
    p.box(lid, "Hazard", (0, 0.47, 0.29), (0.26, 0.2, 0.025), bevel=0.008)
    p.box(lid, "Rubber", (0, 0.47, 0.302), (0.08, 0.08, 0.01), bevel=0.003)
    grime = p.g(3, "Grime")
    p.box(grime, "RustDark", (0.21, 0.2, 0.21), (0.14, 0.28, 0.02), bevel=0.006, euler=(0, 45, 0))
    p.box(grime, "RustDark", (-0.27, 0.72, -0.1), (0.02, 0.14, 0.18), bevel=0.006, euler=(0, 20, 0))
    p.save(PROPS, "OilDrum.fbx")


def vending_bot():
    reset()
    p = Prop("VendingBot")
    body = p.g(0, "Body")
    p.box(body, "VendingRed", (0, 1.0, 0), (1.1, 2.0, 0.8), bevel=0.09)
    window = p.g(1, "Window")
    p.box(window, "MetalDark", (-0.1, 1.2, 0.4), (0.74, 1.0, 0.04), bevel=0.02)
    p.box(window, "Glass", (-0.1, 1.2, 0.42), (0.64, 0.9, 0.02), bevel=0.01)
    for i, y in enumerate((0.92, 1.18, 1.44)):                                          # product rows
        for j, x in enumerate((-0.3, -0.1, 0.1)):
            p.box(window, ("TrimYellow", "TrimTeal", "TrimOrange")[(i + j) % 3], (x, y, 0.44), (0.12, 0.16, 0.02), bevel=0.01)
    panel = p.g(2, "Panel")
    p.box(panel, "MetalDark", (0.39, 1.2, 0.41), (0.2, 0.8, 0.04), bevel=0.02)
    for k, col in enumerate(("BotEye", "TrimYellow", "TrimYellow", "TrimOrange")):
        p.sphere(panel, col, (0.39, 1.45 - k * 0.15, 0.44), (0.07, 0.07, 0.04))
    p.box(panel, "Screen", (0, 1.83, 0.41), (0.62, 0.22, 0.03), bevel=0.015)
    slot = p.g(3, "Slot")
    p.box(slot, "MetalDark", (-0.1, 0.38, 0.41), (0.62, 0.2, 0.06), bevel=0.02)
    p.box(slot, "Rubber", (-0.1, 0.38, 0.44), (0.5, 0.08, 0.02), bevel=0.005)
    head = p.g(4, "Head")
    p.sphere(head, "Metal", (0, 2.0, 0), (0.62, 0.42, 0.6))
    for x in (-0.12, 0.12):
        p.sphere(head, "BotEye", (x, 2.08, 0.25), (0.09, 0.09, 0.05))
    p.cyl(head, "MetalDark", (0.12, 2.32, -0.05), 0.012, 0.25)
    p.sphere(head, "TrimOrange", (0.12, 2.46, -0.05), (0.06, 0.06, 0.06))
    limbs = p.g(5, "Limbs")
    for x in (-0.6, 0.6):
        p.capsule(limbs, "MetalDark", (x, 1.2, 0), 0.07, 0.5, euler=(0, 0, 15 if x > 0 else -15))
        p.box(limbs, "Rubber", (x * 0.6, 0.04, 0), (0.3, 0.08, 0.7), bevel=0.03)
    p.box(limbs, "Rust", (0.551, 0.6, -0.15), (0.02, 0.4, 0.3), bevel=0.005)
    p.save(PROPS, "VendingBot.fbx")


def hover_scooter():
    reset()
    p = Prop("HoverScooter")
    deck = p.g(0, "Deck")
    p.box(deck, "ScooterOrange", (0, 0.47, 0), (0.7, 0.2, 1.8), bevel=0.08)
    p.box(deck, "ScooterOrange", (0, 0.6, -0.82), (0.5, 0.18, 0.2), bevel=0.06)       # tail fin
    grip = p.g(1, "Grip")
    p.box(grip, "Rubber", (0, 0.58, 0.05), (0.56, 0.02, 1.2), bevel=0.01)
    pods = p.g(2, "Pods")
    for x in (-0.24, 0.24):
        p.capsule(pods, "MetalDark", (x, 0.29, 0), 0.12, 1.4, axis="z")
        for z in (-0.62, 0.62):
            p.torus(pods, "BotEye", (x, 0.29, z), 0.1, 0.022, axis="z")
    stem = p.g(3, "Stem")
    p.rod(stem, "Metal", (0, 0.56, 0.7), (0, 1.38, 0.82), 0.045)
    p.rod(stem, "MetalDark", (-0.36, 1.4, 0.82), (0.36, 1.4, 0.82), 0.03)
    for x in (-0.33, 0.33):
        p.capsule(stem, "Rubber", (x, 1.4, 0.82), 0.04, 0.16, axis="x")
    p.sphere(stem, "Headlight", (0, 1.22, 0.88), (0.12, 0.1, 0.08))
    rust = p.g(4, "Rust")
    p.box(rust, "Rust", (0.2, 0.58, -0.35), (0.25, 0.02, 0.45), bevel=0.005)
    p.box(rust, "RustDark", (-0.351, 0.45, 0.4), (0.02, 0.12, 0.3), bevel=0.005)
    p.save(PROPS, "HoverScooter.fbx")


# ───────────────────────── Tier 2 junk ─────────────────────────

def cargo_container():
    reset()
    p = Prop("CargoContainer")
    shell = p.g(0, "Shell")
    p.box(shell, "ContainerRed", (0, 1.3, 0), (2.4, 2.6, 6.0), bevel=0.05)
    ribs = p.g(1, "Ribs")
    for i in range(-4, 5):
        z = i * 0.62
        for x in (-1.215, 1.215):
            p.box(ribs, "ContainerRed", (x, 1.3, z), (0.06, 2.35, 0.16), bevel=0.02)
        p.box(ribs, "ContainerRed", (0, 2.615, z), (2.2, 0.04, 0.16), bevel=0.012)
    corners = p.g(2, "Corners")
    for x in (-1.15, 1.15):
        for y in (0.12, 2.48):
            for z in (-2.92, 2.92):
                p.box(corners, "MetalDark", (x, y, z), (0.18, 0.2, 0.18), bevel=0.02)
    doors = p.g(3, "Doors")
    for x in (-0.58, 0.58):
        p.box(doors, "ContainerRed", (x, 1.3, 3.02), (1.12, 2.4, 0.05), bevel=0.02)
        for dx in (-0.3, 0.3):
            p.rod(doors, "MetalDark", (x + dx, 0.15, 3.07), (x + dx, 2.45, 3.07), 0.025)
            p.box(doors, "Metal", (x + dx, 1.2, 3.1), (0.06, 0.22, 0.04), bevel=0.01)
    wear = p.g(4, "Wear")
    for (x, z, top, length) in ((-1.25, 1.55, 2.5, 1.1), (-1.25, 0.3, 2.45, 0.7), (1.25, -1.9, 2.5, 1.4), (1.25, 2.2, 2.5, 0.6)):
        p.box(wear, "RustDark", (x, top - length / 2, z), (0.02, length, 0.07), bevel=0.0)
    p.box(wear, "RustDark", (0.6, 2.62, 1.5), (0.35, 0.02, 0.5), bevel=0.0)
    p.box(wear, "BaseWhite", (1.226, 1.7, 0.8), (0.02, 0.35, 1.6), bevel=0.004)       # stencil plate
    p.save(PROPS, "CargoContainer.fbx")


def mule7_truck():
    reset()
    p = Prop("Mule7Truck")
    box = p.g(0, "Cargo Box")
    p.box(box, "TruckGrey", (0, 1.9, -1.1), (2.6, 2.4, 5.0), bevel=0.08)
    cab = p.g(1, "Cab")
    p.box(cab, "TruckGrey", (0, 1.45, 2.4), (2.4, 1.8, 2.0), bevel=0.28)
    p.box(cab, "Glass", (0, 1.95, 3.38), (2.0, 0.7, 0.06), bevel=0.03, euler=(-8, 0, 0))
    for x in (-1.2, 1.2):
        p.box(cab, "Glass", (x, 1.95, 2.45), (0.05, 0.6, 1.1), bevel=0.02)
    chassis = p.g(2, "Chassis")
    p.box(chassis, "MetalDark", (0, 0.65, 0.3), (2.3, 0.4, 7.2), bevel=0.05)
    p.box(chassis, "Rubber", (0, 0.75, 3.5), (2.4, 0.3, 0.2), bevel=0.06)
    p.box(chassis, "MetalDark", (0, 1.05, 3.42), (1.0, 0.35, 0.05), bevel=0.02)
    p.box(chassis, "Headlight", (-0.9, 1.05, 3.42), (0.32, 0.2, 0.04), bevel=0.03)
    p.box(chassis, "Hazard", (0.9, 1.05, 3.42), (0.32, 0.2, 0.04), bevel=0.03)
    wheels = p.g(3, "Wheels")
    for (x, z) in ((-1.25, 2.8), (1.25, 2.8), (-1.25, -2.2)):
        p.cyl(wheels, "Rubber", (x, 0.55, z), 0.55, 0.42, axis="x", segs=32, bevel=0.07)
        p.cyl(wheels, "Metal", (x * 1.17, 0.55, z), 0.28, 0.05, axis="x")
    p.cyl(wheels, "Rubber", (2.6, 0.15, -3.5), 0.55, 0.3, segs=32, bevel=0.07)        # the loose wheel
    p.cyl(wheels, "Metal", (2.6, 0.31, -3.5), 0.28, 0.04)
    p.cyl(wheels, "MetalDark", (1.25, 0.55, -2.2), 0.12, 0.3, axis="x")                # bare hub
    detail = p.g(4, "Detail")
    for z in (-3.0, -2.0, -1.0, 0.0, 0.8):
        p.box(detail, "MetalDark", (0, 3.12, z), (2.5, 0.05, 0.1), bevel=0.01)
    p.box(detail, "TrimOrange", (1.31, 1.2, -1.1), (0.02, 0.25, 4.6), bevel=0.005)
    p.box(detail, "TrimOrange", (-1.31, 1.2, -1.1), (0.02, 0.25, 4.6), bevel=0.005)
    rust = p.g(5, "Rust")
    for (x, z, length) in ((1.32, -2.2, 1.3), (1.32, -0.9, 0.8), (-1.32, 0.1, 1.1), (-1.32, -3.0, 0.6)):
        p.box(rust, "RustDark", (x, 3.05 - length / 2, z), (0.02, length, 0.08), bevel=0.0)
    p.box(rust, "Rust", (0.6, 2.36, 2.4), (0.8, 0.02, 0.9), bevel=0.006)
    p.save(PROPS, "Mule7Truck.fbx")


# ───────────────────────── Tier 3: Machine Row ─────────────────────────

def generator_unit():
    reset()
    p = Prop("GeneratorUnit")
    housing = p.g(0, "Housing")
    p.box(housing, "LockedGrey", (0, 1.4, 0), (4.0, 2.8, 3.0), bevel=0.14)
    p.box(housing, "MetalDark", (0, 0.1, 0), (4.2, 0.2, 3.2), bevel=0.04)
    vents = p.g(1, "Vents")
    for x in (-2.01, 2.01):
        for k in range(6):
            p.box(vents, "MetalDark", (x, 0.9 + k * 0.22, 0.2), (0.04, 0.08, 1.8), bevel=0.01)
    turbine = p.g(2, "Turbine")
    p.cyl(turbine, "MetalDark", (0, 3.05, 0), 0.95, 0.5, segs=36)
    p.torus(turbine, "Hazard", (0, 3.3, 0), 0.92, 0.05)
    for k in range(6):
        a = k * 60
        p.box(turbine, "Metal", (0.42 * math.cos(math.radians(a)), 3.34, 0.42 * math.sin(math.radians(a))),
              (0.7, 0.04, 0.18), bevel=0.01, euler=(0, -a, 20))
    p.cyl(turbine, "Metal", (0, 3.36, 0), 0.14, 0.08)
    panel = p.g(3, "Panel")
    p.box(panel, "Hazard", (0, 1.6, -1.52), (1.6, 1.0, 0.04), bevel=0.02)
    for k in range(4):
        p.box(panel, "Rubber", (-0.6 + k * 0.4, 1.6, -1.55), (0.12, 1.0, 0.02), bevel=0.004, euler=(0, 0, 30))
    p.box(panel, "MetalDark", (1.3, 1.9, -1.53), (0.7, 0.5, 0.05), bevel=0.02)
    p.box(panel, "Screen", (1.3, 1.95, -1.56), (0.5, 0.25, 0.02), bevel=0.01)
    for k, col in enumerate(("BotEye", "TrimOrange", "Hazard")):
        p.sphere(panel, col, (1.12 + k * 0.18, 1.74, -1.56), (0.07, 0.07, 0.04))
    pipes = p.g(4, "Pipes")
    p.rod(pipes, "Metal", (-1.6, 2.8, 1.0), (-1.6, 3.9, 1.0), 0.12)
    p.torus(pipes, "MetalDark", (-1.6, 3.9, 1.0), 0.13, 0.03)
    p.rod(pipes, "Metal", (1.2, 1.0, 1.52), (1.2, 2.5, 1.52), 0.07)
    p.rod(pipes, "Metal", (1.2, 2.5, 1.52), (0.2, 2.5, 1.52), 0.07)
    p.rod(pipes, "Rubber", (2.0, 0.3, 0.9), (2.6, 0.03, 1.6), 0.05)
    rust = p.g(5, "Rust")
    p.box(rust, "Rust", (2.015, 0.8, -0.6), (0.02, 1.2, 1.0), bevel=0.006)
    p.box(rust, "RustDark", (-0.9, 2.81, 0.6), (1.2, 0.02, 0.8), bevel=0.006)
    p.save(PROPS, "GeneratorUnit.fbx")


# ───────────────────────── Buildings ─────────────────────────

def building(name, w, h, d, trim, roller_door=False):
    """Modular base building: front faces +z, footprint w x d, height h (matches the colliders)."""
    reset()
    p = Prop(name)
    r = p.g(0, "Shell")
    p.box(r, "MetalDark", (0, 0.12, 0), (w + 0.6, 0.24, d + 0.6), bevel=0.05)           # plinth
    p.box(r, "BaseWhite", (0, h / 2, 0), (w, h, d), bevel=0.3)
    p.box(r, "SuitGrey", (0, h + 0.1, 0), (w - 0.2, 0.2, d - 0.2), bevel=0.06)          # roof cap
    # panel seams
    n = max(2, int(w / 2.6))
    for i in range(1, n):
        x = -w / 2 + i * w / n
        p.box(r, "SuitGrey", (x, h * 0.36, d / 2 + 0.005), (0.05, h * 0.62, 0.02), bevel=0.0)
        p.box(r, "SuitGrey", (x, h * 0.36, -d / 2 - 0.005), (0.05, h * 0.62, 0.02), bevel=0.0)
    for side in (-1, 1):
        for i in range(1, max(2, int(d / 2.6))):
            z = -d / 2 + i * d / max(2, int(d / 2.6))
            p.box(r, "SuitGrey", (side * (w / 2 + 0.005), h * 0.36, z), (0.02, h * 0.62, 0.05), bevel=0.0)
    # coloured band
    p.box(r, trim, (0, h * 0.72, 0), (w + 0.08, 0.55, d + 0.08), bevel=0.04)
    p.box(r, "BaseWhite", (0, h * 0.72 - 0.34, 0), (w + 0.1, 0.06, d + 0.1), bevel=0.01)
    # hazard corners
    for x in (-w / 2, w / 2):
        p.box(r, "TrimYellow", (x, 0.5, d / 2), (0.2, 0.6, 0.2), bevel=0.03)
    # door
    if roller_door:
        dw, dh = 6.0, min(5.0, h * 0.62)
        p.box(r, "MetalDark", (0, dh / 2 + 0.1, d / 2 + 0.03), (dw + 0.5, dh + 0.4, 0.12), bevel=0.04)
        for k in range(int(dh / 0.3)):
            p.box(r, "LockedGrey", (0, 0.35 + k * 0.3, d / 2 + 0.1), (dw, 0.24, 0.05), bevel=0.02)
        p.box(r, "TrimYellow", (0, dh + 0.45, d / 2 + 0.12), (dw + 0.5, 0.15, 0.05), bevel=0.01)
    else:
        p.box(r, "MetalDark", (0, 1.45, d / 2 + 0.04), (2.7, 2.9, 0.12), bevel=0.05)
        for x in (-0.55, 0.55):
            p.box(r, "LockedGrey", (x, 1.35, d / 2 + 0.1), (1.04, 2.6, 0.05), bevel=0.02)
            p.box(r, "Glass", (x, 1.85, d / 2 + 0.13), (0.5, 0.9, 0.02), bevel=0.01)
        p.box(r, trim, (0, 3.05, d / 2 + 0.6), (3.6, 0.14, 1.3), bevel=0.04)             # canopy
        p.box(r, "Headlight", (0, 2.95, d / 2 + 0.7), (0.6, 0.05, 0.18), bevel=0.01)
        p.box(r, "MetalDark", (0, 0.08, d / 2 + 0.75), (3.2, 0.16, 1.0), bevel=0.03)     # step
    # windows
    for sx in (-1, 1):
        x = sx * w * 0.3
        ww = w * 0.18
        p.box(r, "MetalDark", (x, h * 0.42, d / 2 + 0.03), (ww + 0.24, 1.84, 0.1), bevel=0.04)
        p.box(r, "Glass", (x, h * 0.42, d / 2 + 0.08), (ww, 1.6, 0.04), bevel=0.02)
        p.box(r, "MetalDark", (x, h * 0.42, d / 2 + 0.1), (0.06, 1.6, 0.03), bevel=0.0)
    for sx in (-1, 1):
        p.box(r, "MetalDark", (sx * (w / 2 + 0.03), h * 0.42, 0), (0.1, 1.5, d * 0.3 + 0.2), bevel=0.04)
        p.box(r, "Glass", (sx * (w / 2 + 0.07), h * 0.42, 0), (0.04, 1.3, d * 0.3), bevel=0.02)
    # side pipes
    p.rod(r, "Metal", (w / 2 + 0.25, 0.3, -d / 2 + 0.6), (w / 2 + 0.25, h + 0.4, -d / 2 + 0.6), 0.12)
    p.rod(r, "Metal", (w / 2 + 0.5, 0.3, -d / 2 + 0.6), (w / 2 + 0.5, h * 0.6, -d / 2 + 0.6), 0.08)
    # roof kit
    for sx in (-1, 1):
        x = sx * w * 0.25
        p.box(r, "MetalDark", (x, h + 0.65, -d * 0.15), (1.6, 0.9, 1.2), bevel=0.08)
        p.cyl(r, "Metal", (x, h + 1.12, -d * 0.15), 0.42, 0.06)
        p.cyl(r, "Rubber", (x, h + 1.14, -d * 0.15), 0.34, 0.04)
    p.box(r, "Glass", (0, h + 0.8, d * 0.2), (w * 0.35, 0.06, d * 0.25), bevel=0.01, euler=(-20, 0, 0))
    p.box(r, "Metal", (0, h + 0.55, d * 0.2), (0.1, 0.6, 0.1), bevel=0.01)
    p.rod(r, "MetalDark", (-w * 0.4, h + 0.2, d * 0.3), (-w * 0.4, h + 2.6, d * 0.3), 0.04)
    p.sphere(r, "TrimOrange", (-w * 0.4, h + 2.65, d * 0.3), (0.14, 0.14, 0.14))
    p.cyl(r, "Metal", (w * 0.38, h + 0.9, d * 0.3), 0.6, 0.06, radius2=0.3, euler=(-35, 0, 0))   # dish
    p.rod(r, "MetalDark", (w * 0.38, h + 0.2, d * 0.3), (w * 0.38, h + 0.85, d * 0.3), 0.05)
    p.save(BUILDINGS, name + ".fbx")


if __name__ == "__main__":
    oil_drum()
    vending_bot()
    hover_scooter()
    cargo_container()
    mule7_truck()
    generator_unit()
    building("Base", 20, 6, 12, "TrimOrange")
    building("UpgradeStation", 14, 5, 10, "TrimTeal")
    building("RecyclingDepot", 14, 5, 10, "TrimYellow")
    building("Warehouse", 18, 8, 16, "TrimOrange", roller_door=True)
    print("[props] done")
