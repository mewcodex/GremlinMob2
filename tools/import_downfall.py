"""Import local assets from a first-generation Downfall JAR."""
import argparse
import configparser
import io
import json
from pathlib import Path
import re
import subprocess
import zipfile
from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
IMAGE_ROOT = "gremlinResources/images/"
FIGHTERS = ("angry", "fat", "shield", "sneak", "wizard", "nob")
PSEUDOCARDS = {"MadGremlin", "FatGremlin", "ShieldGremlin", "SneakyGremlin", "GremlinWizard"}

def bezier(frame, following, channels):
    points = frame.get("curve")
    if not isinstance(points, list):
        return
    t1, t2 = frame.get("time", 0), following.get("time", 0)
    absolute = []
    for key, default in channels:
        first, last = frame.get(key, default), following.get(key, default)
        absolute.extend((t1 + (t2-t1)*points[0], first + (last-first)*points[1],
                         t1 + (t2-t1)*points[2], first + (last-first)*points[3]))
    frame["curve"] = absolute

def convert(source):
    source["skeleton"]["spine"] = "4.2.43"
    source["skins"] = [{"name": name, "attachments": attachments} for name, attachments in source["skins"].items()]
    orders = 0
    for kind in ("ik", "transform", "path"):
        for constraint in source.get(kind, []):
            constraint["order"] = orders
            orders += 1
            if kind in ("transform", "path"):
                for old, new in (("rotateMix", "mixRotate"), ("translateMix", "mixX"), ("scaleMix", "mixScaleX"), ("shearMix", "mixShearY")):
                    if old in constraint:
                        constraint[new] = constraint.pop(old)
                if "mixX" in constraint:
                    constraint["mixY"] = constraint["mixX"]
                if "mixScaleX" in constraint:
                    constraint["mixScaleY"] = constraint["mixScaleX"]
    for bone in source["bones"]:
        rotation = bone.pop("inheritRotation", True)
        scale = bone.pop("inheritScale", True)
        if not rotation and not scale:
            bone["inherit"] = "onlyTranslation"
        elif not rotation:
            bone["inherit"] = "noRotationOrReflection"
        elif not scale:
            bone["inherit"] = "noScale"
    for anim in source["animations"].values():
        for timelines in anim.get("bones", {}).values():
            for name, frames in timelines.items():
                if name == "rotate":
                    for frame in frames:
                        if "angle" in frame:
                            frame["value"] = frame.pop("angle")
                    channels = [("value", 0)]
                else:
                    default = 1 if name == "scale" else 0
                    channels = [("x", default), ("y", default)]
                for frame, following in zip(frames, frames[1:]):
                    bezier(frame, following, channels)
        if "deform" in anim:
            attachments = {}
            for skin, slots in anim.pop("deform").items():
                attachments[skin] = {}
                for slot, timelines in slots.items():
                    attachments[skin][slot] = {}
                    for attachment, frames in timelines.items():
                        for frame, following in zip(frames, frames[1:]):
                            if isinstance(frame.get("curve"), list):
                                t1, t2 = frame.get("time", 0), following.get("time", 0)
                                x1, y1, x2, y2 = frame["curve"]
                                frame["curve"] = [t1+(t2-t1)*x1,y1,t1+(t2-t1)*x2,y2]
                        attachments[skin][slot][attachment] = {"deform": frames}
            anim["attachments"] = attachments
    return source

def write(root, name, content):
    path = root / name
    if not path.resolve().is_relative_to(root.resolve()):
        raise ValueError(f"Invalid resource path: {name}")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(content)

def store_image(root, name, image):
    data = io.BytesIO()
    image.save(data, "PNG")
    write(root, "source/" + name, data.getvalue())

def icon_assets(jar, base, root, catalog, pictures):
    aliases = {}

    def add_native(path, source):
        aliases[path] = "gremlin_mob_2/original/" + source.removesuffix(".png") + ".res"

    def add_power(model, small, large=None):
        key = re.sub(r"(?<!^)(?=[A-Z])", "_", model).lower()
        add_native(f"images/atlases/power_atlas.sprites/{key}.tres", small)
        add_native(f"images/powers/{key}.png", large or small)

    for source, model in catalog["powers"].items():
        add_power(model, f"powers/{source}.png")
    add_power("MobFighterPassive", "powers/gremlinPower.png")

    # Reconstruct the trimmed LibGDX regions using the atlas's bottom-origin offsets.
    atlas = (base / "powers/powers.atlas").read_text(encoding="utf-8")
    with Image.open(base / "powers/powers.png") as sheet:
        for model, region in (("MobMadStrength", "flex"), ("MobWeakenedStrength", "shackle")):
            paths = []
            for size in (48, 128):
                entry = re.search(rf"^{size}/{region}\r?\n((?:[ \t]+[^\n]*\n)+)", atlas, re.MULTILINE)[1]
                fields = dict(tuple(line.strip().split(": ", 1)) for line in entry.splitlines())
                x, y = map(int, fields["xy"].split(", "))
                w, h = map(int, fields["size"].split(", "))
                width, height = map(int, fields["orig"].split(", "))
                dx, dy = map(int, fields["offset"].split(", "))
                tile = sheet.crop((x, y, x + w, y + h)) if fields["rotate"] == "false" else (
                    sheet.crop((x, y, x + h, y + w)).transpose(Image.Transpose.ROTATE_270))
                canvas = Image.new("RGBA", (width, height))
                canvas.paste(tile, (dx, height - h - dy))
                path = f"effects/{region}_{size}.png"
                store_image(root, path, canvas)
                pictures.add(path)
                paths.append(path)
            add_power(model, *paths)

    for source, path in {
        "guardianResources/GuardianImages/powers/ThornsDown32.png": "effects/thorns_down_32.png",
        "guardianResources/GuardianImages/powers/ThornsDown84.png": "effects/thorns_down_84.png",
        "sneckomodResources/images/powers/tempTempHP.png": "effects/temporary_hp.png",
    }.items():
        write(root, "source/" + path, jar.read(source))
        pictures.add(path)
    add_power("MobTemporaryThorns", "effects/thorns_down_32.png", "effects/thorns_down_84.png")
    add_power("MobTemporaryGuard", "effects/temporary_hp.png")

    potions = base / "images/potion"
    for key, layers, outline in (
        ("mob_random_handoff_potion", [("potion_s_liquid.png", "#7f7f7f"), ("potion_s_hybrid.png", "#3f3f3f"),
                                      ("potion_s_glass.png", "white")], "potion_s_outline.png"),
        ("mob_crew_tonic", [("sphere/liquid.png", "white"), ("sphere/body.png", "white")], "sphere/outline.png"),
        ("mob_returning_spirit", [("ghost/liquid.png", "#ffa500"), ("ghost/hybrid.png", "#32cd32"),
                                 ("ghost/body.png", "white")], "ghost/outline.png"),
    ):
        with Image.open(potions / outline) as shape:
            result = Image.new("RGBA", shape.size)
            for layer, tint in layers:
                with Image.open(potions / layer) as texture:
                    result.alpha_composite(ImageChops.multiply(texture.convert("RGBA"), Image.new("RGBA", shape.size, tint)))
            for suffix, image in (("", result), ("_outline", shape)):
                path = f"effects/{key}{suffix}.png"
                store_image(root, path, image)
                pictures.add(path)
        add_native(f"images/atlases/potion_atlas.sprites/{key}.tres", f"effects/{key}.png")
        add_native(f"images/atlases/potion_outline_atlas.sprites/{key}.tres", f"effects/{key}_outline.png")
        add_native(f"images/potions/large/{key}.png", f"effects/{key}.png")
    return aliases

def card_art(jar, output):
    art = []
    for kind in ("attack", "skill", "power"):
        stem = f"1024/bg_{kind}_gremlin"
        plain = Image.open(io.BytesIO(jar.read(IMAGE_ROOT + stem + ".png"))).convert("RGBA")
        for fighter in ("fat", "thief", "tsundere", "sneaky", "wiz", "nob"):
            decorated = Image.open(io.BytesIO(jar.read(IMAGE_ROOT + stem + f"_{fighter}.png"))).convert("RGBA")
            difference = ImageChops.difference(plain.convert("RGB"), decorated.convert("RGB"))
            red, green, blue = difference.split()
            mask = ImageChops.lighter(red, ImageChops.lighter(green, blue)).point(lambda value: 255 if value else 0)
            decorated.putalpha(ImageChops.multiply(mask, decorated.getchannel("A")))
            name = f"card_art/{kind}_{fighter}.png"
            store_image(output, name, decorated.crop(plain.getbbox()))
            art.append(name)
    return art


def relic_art(jar, output, catalog, pictures, refs):
    for source, model in catalog["relics"].items():
        with Image.open(io.BytesIO(jar.read(f"{IMAGE_ROOT}relics/{source}.png"))) as raw, \
                Image.open(io.BytesIO(jar.read(f"{IMAGE_ROOT}relics/relics/{source}.png"))) as raw_outline:
            if raw.size != (128, 128) or raw_outline.size != raw.size:
                raise ValueError(f"Unexpected source relic canvas for {source}")

            # Downfall relic inputs fill roughly twice the area across each axis of the legacy padded sprites.
            # Keep a common scale for all relics and reserve the same 90% inner frame.
            inner = round(256 * 0.9)
            transform = (64 / inner, 0, 64 - 128 * 64 / inner, 0, 64 / inner, 64 - 128 * 64 / inner)
            image = raw.convert("RGBA").transform((256, 256), Image.Transform.AFFINE, transform, Image.Resampling.BICUBIC)
            outline = raw_outline.convert("RGBA").transform((256, 256), Image.Transform.AFFINE, transform, Image.Resampling.BICUBIC)
        alpha = outline.getchannel("A")
        shadow = Image.new("RGBA", image.size)
        shadow.putalpha(alpha.point(lambda value: value // 2))
        large = Image.alpha_composite(shadow, image)
        outline = Image.new("RGBA", image.size, "white")
        outline.putalpha(alpha)
        for variant, art in (("large", large), ("small", image.resize((85, 85), Image.Resampling.LANCZOS)),
                             ("outline", outline.resize((85, 85), Image.Resampling.LANCZOS))):
            name = f"relic_variants/{source}_{variant}.png"
            store_image(output, name, art)
            pictures.add(name)
            refs[model + ":" + variant] = "res://gremlin_mob_2/original/" + name.removesuffix(".png") + ".res"


def keyword_records(jar, namespace, lang):
    path = f"{namespace}Resources/localization/{lang}/KeywordStrings.json"
    if path not in jar.namelist():
        return []
    raw = jar.read(path).decode("utf-8-sig")
    records = []
    for record in re.findall(r"\{[^{}]*\}", raw):
        try:
            fields = json.loads(record)
        except ValueError:
            continue
        if all(key in fields for key in ("NAMES", "PROPER_NAME", "DESCRIPTION")):
            records.append(fields)
    return records


def link_keywords(jar, base, lang, out):
    keywords_path = base / "localization" / lang / "keywords.json"
    if not keywords_path.exists():
        keywords_path = base / "localization/eng/keywords.json"
    vanilla = json.loads(keywords_path.read_text(encoding="utf-8-sig"))["Game Dictionary"]
    definitions = {"base_" + key.lower(): entry for key, entry in vanilla.items() if isinstance(entry, dict) and entry.get("NAMES")}
    definitions.update({"gremlin_" + str(index): entry for index, entry in enumerate(out.get("keywords", []))})
    shared = {
        "bronze": {"insert", "插入"},
        "expansioncontent": {"temp_hp", "临时生命", "暫時生命", "臨時生命", "echo", "回响", "回響"},
    }
    for namespace, names in shared.items():
        for entry in keyword_records(jar, namespace, lang):
            if not names.intersection(name.lower() for name in entry["NAMES"]):
                continue
            key = namespace + "_" + ("insert" if namespace == "bronze" else
                                      "echo" if names.intersection({"echo", "回响", "回響"}).intersection(entry["NAMES"]) else "temp_hp")
            definitions[key] = entry
    # Some locale files do not include the shared terms.
    for key, source_name in (("expansioncontent_echo", "echo"), ("expansioncontent_temp_hp", "temp_hp"), ("bronze_insert", "insert")):
        if key not in definitions:
            namespace = key.split("_", 1)[0]
            definitions[key] = next(entry for entry in keyword_records(jar, namespace, "eng") if source_name in entry["NAMES"])

    aliases = {}
    for key, entry in definitions.items():
        namespace = key.split("_", 1)[0]
        for name in entry["NAMES"]:
            token = name.lower().replace(" ", "_")
            aliases.setdefault(token, key)
            if namespace != "base":
                aliases[namespace + ":" + token] = key
    for token in ("temp_hp", "临时生命", "暫時生命", "臨時生命"):
        aliases[token.lower()] = "expansioncontent_temp_hp"
        aliases["expansioncontent:" + token.lower()] = "expansioncontent_temp_hp"
    def references(text):
        tokens = re.findall(r"(?<![\w*])(?:gremlin:|bronze:|expansioncontent:)?[\w-]+", text.lower())
        return list(dict.fromkeys(aliases[token] for token in tokens if token in aliases))

    for model, entry in out.get("cards", {}).items():
        entry["KEYWORD_TIPS"] = references(entry.get("DESCRIPTION", ""))
        if entry.get("UPGRADE_DESCRIPTION"):
            entry["UPGRADE_KEYWORD_TIPS"] = references(entry["UPGRADE_DESCRIPTION"])
        if model == "MobHideNeedles":
            for field in ("KEYWORD_TIPS", "UPGRADE_KEYWORD_TIPS"):
                if field in entry and "bronze_insert" not in entry[field]:
                    entry[field].insert(0, "bronze_insert")
        if model == "MobDance" and len(entry.get("EXTENDED_DESCRIPTION", [])) >= 7:
            head, *parts = entry["EXTENDED_DESCRIPTION"]
            for index, part in enumerate(parts, 1):
                entry[f"FRONT_{index}_KEYWORD_TIPS"] = references(head + part)
    out["hover_keywords"] = definitions


def localizations(jar, catalog, base):
    """Import source strings and apply the card descriptions synchronized to Simplified Chinese."""
    corrections = json.loads((ROOT / "tools/card_text_corrections.json").read_text(encoding="utf-8"))
    files = {"cards": "CardStrings", "relics": "RelicStrings", "powers": "PowerStrings",
             "potions": "PotionStrings", "characters": "CharacterStrings", "ui": "UIStrings",
             "keywords": "KeywordStrings", "orbs": "OrbStrings"}
    relic_ids = {
        "gremlinKnob": "GremlinKnob", "gremlinKnobUpgrade": "GremlinKnobUpgrade",
    }
    power_ids = {
        "gremlinPower": "GremlinNob", "bashed": "Bashed", "making_magic": "MakingMagic"
    }
    prefix = "gremlinResources/localization/"
    path_names = set(jar.namelist())
    result = {}
    langs = sorted({name.split("/")[2] for name in path_names if name.startswith(prefix)
                    and name.endswith("/CharacterStrings.json")})
    def normalized(key):
        return re.sub(r"[^a-z0-9]", "", key.rsplit(":", 1)[-1].lower())
    for lang in langs:
        out = {}
        for group, file in files.items():
            path = f"{prefix}{lang}/{file}.json"
            if path not in path_names:
                continue
            data = json.loads(jar.read(path).decode("utf-8-sig"))
            if group == "keywords":
                out[group] = data
                continue
            if group in ("characters", "ui", "orbs"):
                out[group] = {key.rsplit(":", 1)[-1]: value for key, value in data.items()}
                continue

            lookup = {normalized(key): value for key, value in data.items()}
            if group == "potions":
                mapped = {"GremlinPotion": "MobCrewTonic", "NecromancyPotion": "MobReturningSpirit",
                          "SwapPotion": "MobRandomHandoffPotion"}
            else:
                mapped = dict(catalog[group])
            if group == "powers":
                mapped["Temp_HP"] = "MobTemporaryGuard"
            out[group] = {}
            for source, model in mapped.items():
                lookup_key = normalized(relic_ids.get(source, power_ids.get(source, source)))
                if lookup_key in lookup:
                    out[group][model] = lookup[lookup_key]
            if group == "powers":
                temp_path = f"expansioncontentResources/localization/{lang}/PowerStrings.json"
                if temp_path in path_names:
                    expansion = json.loads(jar.read(temp_path).decode("utf-8-sig"))
                    hp = next((value for key, value in expansion.items() if normalized(key) in {"temphp", "temporaryhp"}), None)
                    if hp:
                        out[group]["MobTemporaryGuard"] = hp
        expansion_kw = f"expansioncontentResources/localization/{lang}/KeywordStrings.json"
        if expansion_kw in path_names:
            # Some archived language files contain malformed neighboring records.
            raw = jar.read(expansion_kw).decode("utf-8-sig")
            for record in re.findall(r"\{[^{}]*\}", raw):
                if not re.search(r'"(?:temp_hp|临时生命|暫時生命)"', record, re.IGNORECASE):
                    continue
                fields = {}
                for key in ("PROPER_NAME", "DESCRIPTION"):
                    value = re.search(r'"' + key + r'"\s*:\s*("(?:[^"\\]|\\.)*")', record)
                    if value:
                        fields[key] = json.loads(value[1])
                if len(fields) == 2:
                    out["powers"]["MobTemporaryGuard"] = {
                        "NAME": fields["PROPER_NAME"], "DESCRIPTIONS": [fields["DESCRIPTION"]]}
                    break
        for model, description in corrections.get(lang, {}).items():
            if model in out["cards"]:
                entry = out["cards"][model]
                entry["DESCRIPTION"] = description
                entry.pop("UPGRADE_DESCRIPTION", None)
            else:
                out["powers"][model]["DESCRIPTIONS"] = [description]
        for model in ("MobFullRotation", "MobWizardSchool"):
            out["cards"][model].pop("UPGRADE_DESCRIPTION", None)
        link_keywords(jar, base, lang, out)
        result[lang] = out
    return result

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("archive", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--native-rigs", type=Path)
    parser.add_argument("--feminization", type=Path)
    parser.add_argument("--base-sts1", type=Path, default=ROOT.parent / "desktop-1.0")
    args = parser.parse_args()
    catalog = configparser.ConfigParser()
    catalog.optionxform = str
    catalog.read(ROOT / "tools" / "catalog.ini", encoding="utf-8-sig")
    refs = {}
    with zipfile.ZipFile(args.archive) as jar:
        pictures = {n.removeprefix(IMAGE_ROOT) for n in jar.namelist() if n.startswith(IMAGE_ROOT) and n.endswith((".png", ".jpg"))}
        pictures = {name for name in pictures if name.rsplit("/", 1)[-1].lower() not in {
            "prickly_shields.png", "supply_scroll.png", "supplyscrollcard.png", "supplyscrollcard_p.png"}}
        for name in sorted(pictures):
            raw = jar.read(IMAGE_ROOT + name)
            if name.startswith("cards/"):
                art = Image.open(io.BytesIO(raw)).convert("RGBA")
                store_image(args.output, name, Image.alpha_composite(Image.new("RGBA", art.size, "black"), art).convert("RGB"))
            else:
                write(args.output, "source/" + name, raw)
        placeholder = "cards/ancient_placeholder.png"
        art = Image.open(ROOT / "assets/ancient_placeholder.png").convert("RGBA")
        store_image(args.output, placeholder, Image.alpha_composite(Image.new("RGBA", art.size, "black"), art).convert("RGB"))
        pictures.add(placeholder)
        for category in ("cards", "relics", "powers"):
            for original, model in catalog[category].items():
                key = re.sub(r"(?<!^)(?=[A-Z])", "_", original).lower() if category == "cards" and original not in PSEUDOCARDS else original
                name = f"{category}/{key}.png"
                if name not in pictures and f"relics/{name}" in pictures:
                    name = f"relics/{name}"
                if name not in pictures:
                    alternate = f"{category}/{key}_p.png"
                    if alternate not in pictures:
                        raise ValueError(f"Missing original picture for {original}: {name}")
                    name = alternate
                refs[model] = "res://gremlin_mob_2/original/" + name.removesuffix(".png") + ".res"
                if category == "cards" and (large := f"{category}/{key}_p.png") in pictures:
                    refs[model + ":large"] = "res://gremlin_mob_2/original/" + large.removesuffix(".png") + ".res"
                beta = f"cards/betacards/{original}.png"
                if category == "cards" and beta in pictures:
                    refs[model + ":beta"] = "res://gremlin_mob_2/original/" + beta.removesuffix(".png") + ".res"
        for fighter in FIGHTERS:
            base = f"char/{fighter}/skeleton"
            original = jar.read(IMAGE_ROOT + base + ".json")
            atlas = jar.read(IMAGE_ROOT + base + ".atlas")
            write(args.output, "data/" + base + ".v3.json", original)
            write(args.output, "data/" + base + ".atlas", atlas)
            write(args.output, "data/" + base + ".spine-json", json.dumps(convert(json.loads(original)), separators=(",", ":")).encode())
            if args.native_rigs:
                folder = args.native_rigs / fighter
                if (folder / f"{fighter}.png").read_bytes() != jar.read(IMAGE_ROOT + base + ".png"):
                    raise ValueError(f"Native rig has a different atlas texture: {fighter}")
                binary = (folder / f"{fighter}.spskel").read_bytes()
                if not binary[9:].startswith(b"4.2."):
                    raise ValueError(f"Not a Spine 4.2 binary: {fighter}")
                write(args.output, "data/" + base + ".skel", binary)

        text_data = localizations(jar, catalog, args.base_sts1)
        pictures.update(card_art(jar, args.output))
        if args.feminization:
            for fighter in FIGHTERS:
                stem = f"feminization/char/{fighter}/skeleton"
                source = f"DownfallFeminization/{IMAGE_ROOT}char/{fighter}/skeleton"
                def extract(extension):
                    return subprocess.run(["tar", "-xOf", str(args.feminization), source + extension],
                                          check=True, stdout=subprocess.PIPE).stdout
                write(args.output, "source/" + stem + ".png", extract(".png"))
                pictures.add(stem + ".png")
                write(args.output, "data/" + stem + ".atlas", extract(".atlas"))
                original = extract(".json")
                write(args.output, "data/" + stem + ".v3.json", original)
                write(args.output, "data/" + stem + ".spine-json",
                      json.dumps(convert(json.loads(original)), separators=(",", ":")).encode())
            for direction in ("Left", "Right"):
                source = args.base_sts1 / "images/ui/charSelect" / f"tiny{direction}Arrow.png"
                name = f"feminization/ui/{direction.lower()}.png"
                write(args.output, "source/" + name, source.read_bytes())
                pictures.add(name)
            for lang, groups in text_data.items():
                path = f"reskinContent/localization/{lang}/UIStrings.json"
                if path in jar.namelist():
                    labels = json.loads(jar.read(path).decode("utf-8-sig"))["reskinContent:ReSkin"]
                    groups["ui"]["SkinPicker"] = labels
        relic_art(jar, args.output, catalog, pictures, refs)
        art = {catalog["cards"][name]: fighter for name, fighter in catalog["card_art"].items()}
        write(args.output, "data/card_art.json", json.dumps(art).encode())
        write(args.output, "data/text.json", json.dumps(text_data, ensure_ascii=False).encode("utf-8"))
        aliases = icon_assets(jar, args.base_sts1, args.output, catalog, pictures)
        write(args.output, "data/icon_aliases.json", json.dumps(aliases).encode())
    write(args.output, "data/catalog.json", json.dumps(refs, indent=2).encode())
    write(args.output, "manifest.txt", "\n".join(sorted(pictures)).encode())
    print(f"{len(pictures)} original textures, {len(refs)} asset bindings, and {len(FIGHTERS)} original Spine rigs.")

if __name__ == "__main__":
    main()
