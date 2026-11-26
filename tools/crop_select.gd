extends SceneTree

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var art := Image.load_from_file(args[0])
	if art == null or art.get_size() != Vector2i(1920, 1200):
		push_error("Missing or unexpected original character select artwork.")
		quit(1)
		return
	# Match the 132 × 195 portrait used by the other Downfall characters.
	var crop := art.get_region(Rect2i(664, 75, 704, 1040))
	crop.resize(132, 195, Image.INTERPOLATE_LANCZOS)
	var output := args[1]
	if crop.save_png(output.path_join("char_select.png")) != OK:
		quit(1)
		return
	crop.convert(Image.FORMAT_L8)
	if crop.save_png(output.path_join("char_select_locked.png")) != OK:
		quit(1)
		return
	for entry in [["button_colored", "head_icon"], ["button", "head_icon_outline"], ["leaderboard", "map_marker"]]:
		var source := Image.load_from_file(String(args[0]).get_base_dir().path_join(entry[0] + ".png"))
		if source == null or source.get_region(source.get_used_rect()).save_png(output.path_join(entry[1] + ".png")) != OK:
			quit(1)
			return
	quit()
