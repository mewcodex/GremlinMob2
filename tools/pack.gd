extends SceneTree

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 2:
		quit(1)
		return
	var root := args[0].simplify_path()
	var output := args[1].simplify_path()
	var staging := root.path_join("build/resources")
	DirAccess.make_dir_recursive_absolute(staging)
	var packer := PCKPacker.new()
	if packer.pck_start(output) != OK:
		quit(1)
		return
	var legacy := root.path_join("build/legacy")
	var manifest := FileAccess.get_file_as_string(legacy.path_join("manifest.txt"))
	var has_feminization := manifest.contains("feminization/char/angry/skeleton.png")
	var picture_count := 0
	for entry in manifest.split("\n", false):
		var img := Image.load_from_file(legacy.path_join("source").path_join(entry))
		var filename := entry.get_basename() + ".res"
		var stage_path := staging.path_join("original").path_join(filename)
		var pack_path := "res://gremlin_mob_2/original/" + filename
		DirAccess.make_dir_recursive_absolute(stage_path.get_base_dir())
		if img == null or img.is_empty() or ResourceSaver.save(ImageTexture.create_from_image(img), stage_path, ResourceSaver.FLAG_COMPRESS) != OK or packer.add_file(pack_path, stage_path) != OK:
			push_error("Could not pack original texture " + entry)
			quit(1)
			return
		if has_feminization and entry.begins_with("card_art/"):
			var fighter := entry.get_file().get_basename().get_slice("_", 1)
			var source := root.path_join("assets/feminization_card_art").path_join(fighter + ".png")
			if not pack_skin_sticker(packer, img, source, staging, filename):
				push_error("Could not pack skin sticker " + entry)
				quit(1)
				return
		if entry.ends_with("/skeleton.png"):
			var remap := FileAccess.open(stage_path.get_basename() + ".remap", FileAccess.WRITE)
			remap.store_string("[remap]\npath=\"" + pack_path + "\"\n")
			remap.close()
			if packer.add_file("res://gremlin_mob_2/original/" + entry + ".remap", stage_path.get_basename() + ".remap") != OK:
				quit(1)
				return
		picture_count += 1
	for orb in [
		["cost", "1024", "res://images/atlases/ui_atlas.sprites/card/energy_gremlin_mob_2.tres"],
		["inline", "512", "res://images/packed/sprite_fonts/gremlin_mob_2_energy_icon.png"]
	]:
		var legacy_orb := Image.load_from_file(legacy.path_join("source").path_join(orb[1]).path_join("card_gremlin_orb.png"))
		var region := legacy_orb.get_used_rect()
		if region.size.x == 0 or region.size.y == 0:
			push_error("The legacy card orb has no visible pixels.")
			quit(1)
			return
		var crop := legacy_orb.get_region(region)
		var canvas_size: int = 74 if orb[0] == "cost" else 24
		var inner: int = canvas_size if orb[0] == "cost" else 22
		var ratio := float(inner) / maxf(crop.get_width(), crop.get_height())
		crop.resize(roundi(crop.get_width() * ratio), roundi(crop.get_height() * ratio), Image.INTERPOLATE_LANCZOS)
		var fitted := Image.create_empty(canvas_size, canvas_size, false, Image.FORMAT_RGBA8)
		fitted.blit_rect(crop, Rect2i(Vector2i.ZERO, crop.get_size()), (Vector2i(canvas_size, canvas_size) - crop.get_size()) / 2)
		var orb_res := "res://gremlin_mob_2/original/orb_" + String(orb[0]) + ".res"
		var cropped_orb := staging.path_join(orb_res.get_file())
		if ResourceSaver.save(ImageTexture.create_from_image(fitted), cropped_orb, ResourceSaver.FLAG_COMPRESS) != OK or packer.add_file(orb_res, cropped_orb) != OK:
			quit(1)
			return
		var alias_file := staging.path_join(String(orb[2]).get_file() + ".remap")
		var remap := FileAccess.open(alias_file, FileAccess.WRITE)
		remap.store_string("[remap]\npath=\"" + orb_res + "\"\n")
		remap.close()
		if packer.add_file(orb[2] + ".remap", alias_file) != OK:
			quit(1)
			return
	var rig_dirs := []
	for fighter in ["angry", "fat", "shield", "sneak", "wizard", "nob"]:
		rig_dirs.append("char/" + fighter)
		if manifest.contains("feminization/char/" + fighter + "/skeleton.png"):
			rig_dirs.append("feminization/char/" + fighter)
	for rig_dir: String in rig_dirs:
		for extension in [".atlas", ".v3.json", ".spine-json", ".skel"]:
			var filename: String = rig_dir + "/skeleton" + extension
			var source_file := legacy.path_join("data").path_join(filename)
			if extension == ".skel" and not FileAccess.file_exists(source_file):
				continue
			if packer.add_file("res://gremlin_mob_2/original/" + filename, source_file) != OK:
				quit(1)
				return
	if packer.add_file("res://gremlin_mob_2/original/text.json", legacy.path_join("data/text.json")) != OK or packer.add_file("res://gremlin_mob_2/original/card_art.json", legacy.path_join("data/card_art.json")) != OK:
		quit(1)
		return
	if packer.add_file("res://gremlin_mob_2/original/catalog.json", legacy.path_join("data/catalog.json")) != OK or packer.add_file("res://gremlin_mob_2/selection_original.tscn", root.path_join("assets/selection_original.tscn")) != OK:
		quit(1)
		return
	var portraits := root.path_join("build/select")
	if packer.add_file("res://gremlin_mob_2/character_icon.tscn", root.path_join("assets/character_icon.tscn")) != OK:
		quit(1)
		return
	for name in ["char_select", "char_select_locked", "character_icon_outline", "map_marker", "mp_point", "mp_rock", "mp_paper", "mp_scissors"]:
		var import_data := ConfigFile.new()
		if import_data.load(portraits.path_join(name + ".png.import")) != OK:
			push_error("Missing imported portrait " + name)
			quit(1)
			return
		var imported := String(import_data.get_value("remap", "path")).trim_prefix("res://")
		if packer.add_file("res://gremlin_mob_2/" + name + ".ctex", portraits.path_join(imported)) != OK:
			quit(1)
			return
	var head := Image.load_from_file(portraits.path_join("character_icon.png"))
	var head_path := staging.path_join("character_icon.res")
	if head == null or ResourceSaver.save(ImageTexture.create_from_image(head), head_path, ResourceSaver.FLAG_COMPRESS) != OK or packer.add_file("res://gremlin_mob_2/character_icon.res", head_path) != OK:
		quit(1)
		return
	var transition := FileAccess.get_file_as_string(portraits.path_join("transition_mat.tres"))
	var transition_image := Image.load_from_file(portraits.path_join("transition.png"))
	var transition_texture := staging.path_join("transition.res")
	if transition.is_empty() or transition_image == null or ResourceSaver.save(ImageTexture.create_from_image(transition_image), transition_texture, ResourceSaver.FLAG_COMPRESS) != OK:
		quit(1)
		return
	if packer.add_file("res://gremlin_mob_2/transition.res", transition_texture) != OK:
		quit(1)
		return
	var transition_path := staging.path_join("transition_mat.tres")
	var transition_file := FileAccess.open(transition_path, FileAccess.WRITE)
	transition_file.store_string(transition.replace("res://Gremlins/images/character/transition.png", "res://gremlin_mob_2/transition.res"))
	transition_file.close()
	if packer.add_file("res://gremlin_mob_2/transition_mat.tres", transition_path) != OK or packer.add_file("res://gremlin_mob_2/card_trail.tscn", portraits.path_join("card_trail.tscn")) != OK:
		quit(1)
		return
	# Match CharacterModel's native paths. The templates resolve through the installed game.
	var aliases := {
		"scenes/screens/char_select/char_select_bg_gremlin_mob_character.tscn": "gremlin_mob_2/selection_original.tscn",
		"images/ui/top_panel/character_icon_gremlin_mob_character.png": "gremlin_mob_2/character_icon.res",
		"images/ui/top_panel/character_icon_gremlin_mob_character_outline.png": "gremlin_mob_2/character_icon_outline.ctex",
		"materials/transitions/gremlin_mob_character_transition_mat.tres": "gremlin_mob_2/transition_mat.tres",
		"scenes/vfx/card_trail_gremlin_mob_character.tscn": "gremlin_mob_2/card_trail.tscn"
	}
	for hand in ["point", "rock", "paper", "scissors"]:
		aliases["images/ui/hands/multiplayer_hand_gremlin_mob_character_" + hand + ".png"] = "gremlin_mob_2/mp_" + hand + ".ctex"
	var effect_icons: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(legacy.path_join("data/icon_aliases.json")))
	aliases.merge(effect_icons)
	for template: String in [
		"scenes/creature_visuals/ironclad.tscn",
		"scenes/combat/energy_counters/ironclad_energy_counter.tscn",
		"scenes/merchant/characters/ironclad_merchant.tscn",
		"scenes/rest_site/characters/ironclad_rest_site.tscn"
	]:
		aliases[template.replace("ironclad", "gremlin_mob_character")] = template
	for native_path: String in aliases:
		var filename := staging.path_join(native_path.get_file() + ".remap")
		var remap := FileAccess.open(filename, FileAccess.WRITE)
		remap.store_string("[remap]\npath=\"res://" + String(aliases[native_path]) + "\"\n")
		remap.close()
		if packer.add_file("res://" + native_path + ".remap", filename) != OK:
			quit(1)
			return
	if packer.flush() != OK or not ProjectSettings.load_resource_pack(output):
		quit(1)
		return
	if not load("res://gremlin_mob_2/selection_original.tscn") is PackedScene:
		quit(1)
		return
	if not load("res://gremlin_mob_2/transition_mat.tres") is ShaderMaterial:
		quit(1)
		return
	for name in ["char_select", "char_select_locked"]:
		var portrait := load("res://gremlin_mob_2/" + name + ".ctex") as CompressedTexture2D
		if portrait == null or portrait.get_size() != Vector2(132, 195):
			quit(1)
			return
	print("Packed ", picture_count, " original textures, the source rigs, and the character select scene.")
	quit()

func pack_skin_sticker(packer: PCKPacker, original: Image, source: String, staging: String, filename: String) -> bool:
	var sprite := Image.load_from_file(source)
	var bounds := original.get_used_rect()
	if sprite == null or sprite.is_empty() or not bounds.has_area():
		return false
	sprite.convert(Image.FORMAT_RGBA8)
	var original_fit := minf(float(bounds.size.x) / sprite.get_width(), float(bounds.size.y) / sprite.get_height())
	var max_height := maxf(bounds.size.y * 1.12, original.get_height() * 0.30)
	var scale_factor := minf(original_fit * 1.45, max_height / sprite.get_height())
	var fitted := Vector2i(roundi(sprite.get_width() * scale_factor), roundi(sprite.get_height() * scale_factor))
	sprite.resize(fitted.x, fitted.y, Image.INTERPOLATE_LANCZOS)
	var canvas := Image.create_empty(original.get_width(), original.get_height(), false, Image.FORMAT_RGBA8)
	var margin := roundi(original.get_width() * 0.07)
	var centered_x := bounds.position.x + (bounds.size.x - fitted.x) / 2
	var offset := Vector2i(clampi(centered_x, margin, original.get_width() - margin - fitted.x), bounds.end.y - fitted.y)
	canvas.blit_rect(sprite, Rect2i(Vector2i.ZERO, fitted), offset)
	var stage_path := staging.path_join("feminization").path_join(filename)
	DirAccess.make_dir_recursive_absolute(stage_path.get_base_dir())
	return ResourceSaver.save(ImageTexture.create_from_image(canvas), stage_path, ResourceSaver.FLAG_COMPRESS) == OK and \
		packer.add_file("res://gremlin_mob_2/original/feminization/" + filename, stage_path) == OK
