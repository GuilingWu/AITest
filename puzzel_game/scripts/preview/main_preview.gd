extends Control

const CATALOG_PATH := "res://assets/themes/theme_catalog.json"
const PIECE_CARD_SIZE := Vector2(96, 96)
const STACK_SOURCE_HEIGHT := 132.0
const BOARD_PADDING := 16.0
const BOARD_GAP := 8.0
const PIECE_PREVIEW_SIZE := 144
const TAB_WIDTH_RATIO := 0.42
const TAB_DEPTH_RATIO := 0.18

@onready var theme_select_ui: Control = $ThemeSelectUI
@onready var image_select_ui: Control = $ImageSelectUI
@onready var difficulty_setup_ui: Control = $DifficultySetupUI
@onready var puzzle_game_ui: Control = $PuzzleGameUI
@onready var theme_list_box: VBoxContainer = $ThemeSelectUI/SafeArea/ContentColumn/ThemeList
@onready var image_list_box: VBoxContainer = $ImageSelectUI/SafeArea/ContentColumn/Scroll/ImageList
@onready var title_label: Label = $ImageSelectUI/SafeArea/ContentColumn/HeaderRow/Title
@onready var difficulty_title: Label = $DifficultySetupUI/SafeArea/ContentColumn/TitlePanel/TitleMargin/Title
@onready var stars_label: Label = $DifficultySetupUI/SafeArea/ContentColumn/StarsPanel/StarsMargin/StarsLabel
@onready var rows_box: SpinBox = $DifficultySetupUI/SafeArea/ContentColumn/RowsBox
@onready var cols_box: SpinBox = $DifficultySetupUI/SafeArea/ContentColumn/ColumnsBox
@onready var rotation_toggle: CheckButton = $DifficultySetupUI/SafeArea/ContentColumn/RotationToggle
@onready var storage_toggle: CheckButton = $DifficultySetupUI/SafeArea/ContentColumn/StorageToggle
@onready var puzzle_title_label: Label = $PuzzleGameUI/SafeArea/RootColumn/TopBarPanel/TopBar/ProgressLabel
@onready var puzzle_star_label: Label = $PuzzleGameUI/SafeArea/RootColumn/TopBarPanel/TopBar/StarLabel
@onready var puzzle_back_button: Button = puzzle_game_ui.find_child("BackButton", true, false) as Button
@onready var puzzle_viewport_container: Control = $PuzzleGameUI/SafeArea/RootColumn/PuzzleViewportContainer
@onready var storage_mode_tabs: Control = $PuzzleGameUI/SafeArea/RootColumn/BottomPanel/BottomColumn/StorageModeTabs
@onready var horizontal_piece_list: ScrollContainer = $PuzzleGameUI/SafeArea/RootColumn/BottomPanel/BottomColumn/HorizontalPieceList
@onready var horizontal_piece_items: HBoxContainer = $PuzzleGameUI/SafeArea/RootColumn/BottomPanel/BottomColumn/HorizontalPieceList/Items
@onready var puzzle_hint_label: Label = $PuzzleGameUI/SafeArea/RootColumn/BottomPanel/BottomColumn/StackHintOverlay

var _result_overlay: Control
var _result_stars_label: Label
var _result_summary_label: Label
var _theme_catalog: Array = []
var _theme_lookup := {}
var _theme_id := "scenery"
var _theme_name := "Scenery"
var _selected_image_id := ""
var _selected_image_title := ""
var _selected_image_file := ""

var _board_overlay: Control
var _board_panel: Panel
var _board_slots_layer: Control
var _placed_pieces_layer: Control
var _stack_source_panel: Panel
var _stack_source_items: HFlowContainer
var _drag_layer: Control

var _slot_controls: Array[Control] = []
var _piece_textures: Array[Texture2D] = []
var _piece_sources := {}
var _piece_slots := {}
var _slot_pieces := {}
var _placed_piece_nodes := {}
var _piece_groups := {}
var _groups := {}
var _next_group_id := 1

var _drag_piece_index := -1
var _drag_piece_indices: Array[int] = []
var _drag_origin_slots := {}
var _drag_preview: Control
var _drag_target_slots := {}
var _drag_from_board := false
var _drag_anchor_piece_index := -1
var _current_rows := 0
var _current_cols := 0
var _current_use_horizontal_list := true
var _result_shown := false

func _ready() -> void:
	_load_catalog()
	_bind_buttons()
	_configure_puzzle_storage_ui()
	_ensure_puzzle_overlay()
	_ensure_result_overlay()
	_populate_theme_list()
	_open_theme(_theme_id)
	_show_screen(theme_select_ui)
	_refresh_stars()

func _bind_buttons() -> void:
	$ThemeSelectUI/SafeArea/ContentColumn/AlbumButton.pressed.connect(_on_album_pressed)
	$ImageSelectUI/SafeArea/ContentColumn/HeaderRow/BackButton.pressed.connect(_back_to_theme_select)

	rows_box.value_changed.connect(_on_difficulty_value_changed)
	cols_box.value_changed.connect(_on_difficulty_value_changed)
	rotation_toggle.toggled.connect(_on_rotation_toggled)
	storage_toggle.toggled.connect(_on_storage_toggled)
	$DifficultySetupUI/SafeArea/ContentColumn/BackButton.pressed.connect(_back_to_image_select)
	$DifficultySetupUI/SafeArea/ContentColumn/StartButton.pressed.connect(_open_puzzle_game)

	if puzzle_back_button != null:
		puzzle_back_button.pressed.connect(_back_to_difficulty_setup)

func _input(event: InputEvent) -> void:
	if _drag_preview == null:
		return

	if event is InputEventMouseMotion:
		_update_drag_preview_position(event.global_position)
	elif event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and not event.pressed:
		_finish_drag(event.global_position)

func _configure_puzzle_storage_ui() -> void:
	storage_mode_tabs.visible = false
	horizontal_piece_list.set("horizontal_scroll_mode", 1)
	horizontal_piece_list.set("vertical_scroll_mode", 0)
	horizontal_piece_list.follow_focus = false

func _ensure_puzzle_overlay() -> void:
	if _board_overlay != null:
		return

	_board_overlay = Control.new()
	_board_overlay.name = "PreviewBoardOverlay"
	_board_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	_board_overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
	puzzle_viewport_container.add_child(_board_overlay)

	_board_panel = Panel.new()
	_board_panel.name = "BoardPanel"
	_board_panel.anchor_left = 0.0
	_board_panel.anchor_top = 0.0
	_board_panel.anchor_right = 1.0
	_board_panel.anchor_bottom = 1.0
	_board_panel.offset_left = BOARD_PADDING
	_board_panel.offset_top = BOARD_PADDING
	_board_panel.offset_right = -BOARD_PADDING
	_board_panel.offset_bottom = -BOARD_PADDING
	_board_panel.mouse_filter = Control.MOUSE_FILTER_PASS
	_board_overlay.add_child(_board_panel)

	_board_slots_layer = Control.new()
	_board_slots_layer.name = "BoardSlots"
	_board_slots_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
	_board_slots_layer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_board_panel.add_child(_board_slots_layer)

	_placed_pieces_layer = Control.new()
	_placed_pieces_layer.name = "PlacedPieces"
	_placed_pieces_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
	_placed_pieces_layer.mouse_filter = Control.MOUSE_FILTER_PASS
	_board_panel.add_child(_placed_pieces_layer)

	_stack_source_panel = Panel.new()
	_stack_source_panel.name = "StackSourcePanel"
	_stack_source_panel.anchor_left = 0.0
	_stack_source_panel.anchor_top = 1.0
	_stack_source_panel.anchor_right = 1.0
	_stack_source_panel.anchor_bottom = 1.0
	_stack_source_panel.offset_left = BOARD_PADDING
	_stack_source_panel.offset_top = -STACK_SOURCE_HEIGHT
	_stack_source_panel.offset_right = -BOARD_PADDING
	_stack_source_panel.offset_bottom = 0.0
	_stack_source_panel.visible = false
	_board_overlay.add_child(_stack_source_panel)

	_stack_source_items = HFlowContainer.new()
	_stack_source_items.name = "Items"
	_stack_source_items.set_anchors_preset(Control.PRESET_FULL_RECT)
	_stack_source_items.offset_left = 12.0
	_stack_source_items.offset_top = 12.0
	_stack_source_items.offset_right = -12.0
	_stack_source_items.offset_bottom = -12.0
	_stack_source_items.add_theme_constant_override("h_separation", 10)
	_stack_source_items.add_theme_constant_override("v_separation", 10)
	_stack_source_panel.add_child(_stack_source_items)

	_drag_layer = Control.new()
	_drag_layer.name = "DragLayer"
	_drag_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
	_drag_layer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	puzzle_game_ui.add_child(_drag_layer)

func _ensure_result_overlay() -> void:
	if _result_overlay != null:
		return

	_result_overlay = Control.new()
	_result_overlay.name = "ResultOverlay"
	_result_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	_result_overlay.visible = false
	_result_overlay.mouse_filter = Control.MOUSE_FILTER_STOP
	puzzle_game_ui.add_child(_result_overlay)

	var backdrop := ColorRect.new()
	backdrop.set_anchors_preset(Control.PRESET_FULL_RECT)
	backdrop.color = Color(0.02, 0.04, 0.08, 0.72)
	backdrop.mouse_filter = Control.MOUSE_FILTER_STOP
	_result_overlay.add_child(backdrop)

	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_result_overlay.add_child(center)

	var card := Panel.new()
	card.custom_minimum_size = Vector2(420, 260)
	center.add_child(card)

	var content := VBoxContainer.new()
	content.set_anchors_preset(Control.PRESET_FULL_RECT)
	content.offset_left = 24.0
	content.offset_top = 24.0
	content.offset_right = -24.0
	content.offset_bottom = -24.0
	content.alignment = BoxContainer.ALIGNMENT_CENTER
	content.add_theme_constant_override("separation", 14)
	card.add_child(content)

	var title := Label.new()
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.text = "Puzzle Complete"
	content.add_child(title)

	_result_stars_label = Label.new()
	_result_stars_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_result_stars_label.text = "Matched 0 / 0"
	content.add_child(_result_stars_label)

	_result_summary_label = Label.new()
	_result_summary_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_result_summary_label.text = ""
	content.add_child(_result_summary_label)

	var retry_button := Button.new()
	retry_button.text = "Retry"
	retry_button.custom_minimum_size = Vector2(0, 48)
	retry_button.pressed.connect(_open_puzzle_game)
	content.add_child(retry_button)

	var back_button := Button.new()
	back_button.text = "Back To Themes"
	back_button.custom_minimum_size = Vector2(0, 48)
	back_button.pressed.connect(_back_to_theme_select)
	content.add_child(back_button)

func _show_result_overlay(correct_count: int, total: int) -> void:
	if _result_overlay == null:
		return
	_result_shown = true
	_result_stars_label.text = "Matched %d / %d" % [correct_count, total]
	_result_summary_label.text = "%s  %d x %d" % [_selected_image_title if not _selected_image_title.is_empty() else _theme_name, _current_rows, _current_cols]
	_result_overlay.visible = true

func _hide_result_overlay() -> void:
	if _result_overlay != null:
		_result_overlay.visible = false

func _load_catalog() -> void:
	_theme_catalog.clear()
	_theme_lookup.clear()

	if not FileAccess.file_exists(CATALOG_PATH):
		return

	var raw_text := FileAccess.get_file_as_string(CATALOG_PATH)
	var parsed = JSON.parse_string(raw_text)
	if typeof(parsed) != TYPE_DICTIONARY:
		return

	var themes: Array = parsed.get("themes", [])
	for theme in themes:
		if typeof(theme) != TYPE_DICTIONARY:
			continue
		_theme_catalog.append(theme)
		_theme_lookup[theme.get("id", "")] = theme

func _populate_theme_list() -> void:
	_clear_children(theme_list_box)
	for theme in _theme_catalog:
		var button := Button.new()
		button.custom_minimum_size = Vector2(0, 56)
		button.text = "%s  %d images" % [theme.get("title", theme.get("id", "Theme")), (theme.get("images", []) as Array).size()]
		button.pressed.connect(func(): _open_theme(theme.get("id", "")))
		theme_list_box.add_child(button)

func _populate_image_list(theme: Dictionary) -> void:
	_clear_children(image_list_box)

	var images: Array = theme.get("images", [])
	var row: HBoxContainer = null
	for index in range(images.size()):
		if index % 3 == 0:
			row = HBoxContainer.new()
			row.add_theme_constant_override("separation", 12)
			image_list_box.add_child(row)

		if row != null:
			row.add_child(_create_image_card(theme, images[index]))

func _create_image_card(theme: Dictionary, image: Dictionary) -> Button:
	var button := Button.new()
	button.custom_minimum_size = Vector2(160, 100)
	button.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	button.flat = true
	button.text = ""

	var placeholder := ColorRect.new()
	placeholder.set_anchors_preset(Control.PRESET_FULL_RECT)
	placeholder.mouse_filter = Control.MOUSE_FILTER_IGNORE
	placeholder.color = Color(0.101961, 0.219608, 0.309804, 1)
	button.add_child(placeholder)

	var preview := TextureRect.new()
	preview.set_anchors_preset(Control.PRESET_FULL_RECT)
	preview.mouse_filter = Control.MOUSE_FILTER_IGNORE
	preview.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	preview.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_COVERED

	var texture_path := _build_texture_path(theme.get("folder", _theme_id), image.get("file", ""))
	if ResourceLoader.exists(texture_path):
		preview.texture = load(texture_path)
	button.add_child(preview)

	var caption := Label.new()
	caption.anchor_top = 1.0
	caption.anchor_right = 1.0
	caption.anchor_bottom = 1.0
	caption.offset_top = -28.0
	caption.grow_horizontal = Control.GROW_DIRECTION_BOTH
	caption.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	caption.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	caption.mouse_filter = Control.MOUSE_FILTER_IGNORE
	caption.text = str(image.get("title", image.get("id", "")))
	button.add_child(caption)

	button.pressed.connect(func(): _open_difficulty(image))
	return button

func _clear_children(node: Node) -> void:
	for child in node.get_children():
		child.queue_free()

func _open_theme(theme_id: String) -> void:
	var theme: Dictionary = _theme_lookup.get(theme_id, {})
	if theme.is_empty():
		return

	_theme_id = theme_id
	_theme_name = str(theme.get("title", theme_id.capitalize()))
	_selected_image_id = ""
	_selected_image_title = ""
	_selected_image_file = ""
	title_label.text = _theme_name
	difficulty_title.text = _theme_name + " Difficulty"
	_populate_image_list(theme)
	_show_screen(image_select_ui)

func _open_difficulty(image: Dictionary = {}) -> void:
	_selected_image_id = str(image.get("id", ""))
	_selected_image_title = str(image.get("title", _theme_name))
	_selected_image_file = str(image.get("file", ""))
	difficulty_title.text = _theme_name + " Difficulty"
	_refresh_stars()
	_show_screen(difficulty_setup_ui)

func _on_album_pressed() -> void:
	_selected_image_id = "custom_album"
	_selected_image_title = "Album"
	_selected_image_file = ""
	difficulty_title.text = "Album Difficulty"
	_show_screen(difficulty_setup_ui)

func _back_to_theme_select() -> void:
	_show_screen(theme_select_ui)

func _back_to_image_select() -> void:
	_show_screen(image_select_ui)

func _back_to_difficulty_setup() -> void:
	_cancel_drag()
	_show_screen(difficulty_setup_ui)

func _on_difficulty_value_changed(_value: float) -> void:
	_refresh_stars()

func _on_rotation_toggled(_pressed: bool) -> void:
	_refresh_stars()

func _on_storage_toggled(_pressed: bool) -> void:
	_refresh_stars()

func _show_screen(target: Control) -> void:
	theme_select_ui.visible = target == theme_select_ui
	image_select_ui.visible = target == image_select_ui
	difficulty_setup_ui.visible = target == difficulty_setup_ui
	puzzle_game_ui.visible = target == puzzle_game_ui

func _open_puzzle_game() -> void:
	_current_rows = int(rows_box.value)
	_current_cols = int(cols_box.value)
	_current_use_horizontal_list = storage_toggle.button_pressed
	_result_shown = false
	_hide_result_overlay()
	var title_source := _selected_image_title if not _selected_image_title.is_empty() else _theme_name
	puzzle_title_label.text = "%s   %d x %d" % [title_source, _current_rows, _current_cols]
	_apply_storage_mode(_current_use_horizontal_list)
	_rebuild_board_slots(_current_rows, _current_cols, _current_use_horizontal_list)
	_rebuild_piece_sources(_current_rows, _current_cols, _current_use_horizontal_list)
	_refresh_board_feedback()
	_show_screen(puzzle_game_ui)

func _apply_storage_mode(use_horizontal_list: bool) -> void:
	horizontal_piece_list.visible = use_horizontal_list
	_stack_source_panel.visible = not use_horizontal_list
	puzzle_hint_label.visible = not use_horizontal_list
	puzzle_hint_label.text = "Stack mode enabled. Drag pieces from the area below the board."

func _rebuild_board_slots(rows: int, cols: int, use_horizontal_list: bool) -> void:
	for child in _board_slots_layer.get_children():
		child.queue_free()
	for child in _placed_pieces_layer.get_children():
		child.queue_free()

	_slot_controls.clear()
	_slot_pieces.clear()
	_piece_slots.clear()
	_placed_piece_nodes.clear()
	_piece_groups.clear()
	_groups.clear()
	_next_group_id = 1

	var board_size := puzzle_viewport_container.size
	if board_size.x <= 0.0 or board_size.y <= 0.0:
		board_size = Vector2(680, 420)

	var reserved_bottom: float = STACK_SOURCE_HEIGHT + BOARD_PADDING if not use_horizontal_list else BOARD_PADDING
	var available_width: float = max(220.0, board_size.x - BOARD_PADDING * 2.0)
	var available_height: float = max(180.0, board_size.y - reserved_bottom - BOARD_PADDING)
	var cell_size: float = floor(min((available_width - BOARD_GAP * float(cols - 1)) / float(cols), (available_height - BOARD_GAP * float(rows - 1)) / float(rows)))
	cell_size = clamp(cell_size, 48.0, 110.0)

	var board_width: float = float(cols) * cell_size + float(cols - 1) * BOARD_GAP
	var board_height: float = float(rows) * cell_size + float(rows - 1) * BOARD_GAP
	_board_panel.custom_minimum_size = Vector2(board_width + 20.0, board_height + 20.0)
	_board_panel.size = Vector2(board_width + 20.0, board_height + 20.0)
	_board_panel.position = Vector2((board_size.x - _board_panel.size.x) * 0.5, BOARD_PADDING)

	for row in range(rows):
		for col in range(cols):
			var slot := Panel.new()
			slot.custom_minimum_size = Vector2(cell_size, cell_size)
			slot.size = Vector2(cell_size, cell_size)
			slot.position = Vector2(10.0 + col * (cell_size + BOARD_GAP), 10.0 + row * (cell_size + BOARD_GAP))
			slot.modulate = Color(1.0, 1.0, 1.0, 0.6)
			slot.mouse_filter = Control.MOUSE_FILTER_IGNORE
			_board_slots_layer.add_child(slot)
			_slot_controls.append(slot)

func _rebuild_piece_sources(rows: int, cols: int, use_horizontal_list: bool) -> void:
	_cancel_drag()
	_clear_children(horizontal_piece_items)
	_clear_children(_stack_source_items)
	_piece_textures.clear()
	_piece_sources.clear()

	var texture := _load_selected_texture()
	var total: int = rows * cols
	for index in range(total):
		var piece_texture := _build_piece_texture(texture, rows, cols, index)
		_piece_textures.append(piece_texture)
		var source := _create_source_piece(piece_texture, index)
		_piece_sources[index] = source
		if use_horizontal_list:
			horizontal_piece_items.add_child(source)
		else:
			_stack_source_items.add_child(source)

	if use_horizontal_list:
		var total_width: float = float(total) * (PIECE_CARD_SIZE.x + 10.0)
		horizontal_piece_items.custom_minimum_size = Vector2(total_width, PIECE_CARD_SIZE.y)
	else:
		horizontal_piece_items.custom_minimum_size = Vector2.ZERO

func _create_source_piece(texture: Texture2D, piece_index: int) -> Button:
	var button := Button.new()
	button.custom_minimum_size = PIECE_CARD_SIZE
	button.flat = true
	button.text = ""
	button.clip_contents = false
	button.gui_input.connect(_on_source_piece_gui_input.bind(button, piece_index))

	var preview := TextureRect.new()
	preview.set_anchors_preset(Control.PRESET_FULL_RECT)
	preview.mouse_filter = Control.MOUSE_FILTER_IGNORE
	preview.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	preview.stretch_mode = TextureRect.STRETCH_SCALE
	preview.texture = texture
	button.add_child(preview)

	return button

func _on_source_piece_gui_input(event: InputEvent, source: Button, piece_index: int) -> void:
	if source.disabled:
		return
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		_start_drag([piece_index], piece_index, event.global_position, false)
		get_viewport().set_input_as_handled()

func _on_board_piece_gui_input(event: InputEvent, piece_index: int, _slot_index: int) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		var drag_indices := _get_drag_piece_indices(piece_index)
		_start_drag(drag_indices, piece_index, event.global_position, true)
		get_viewport().set_input_as_handled()

func _start_drag(piece_indices: Array[int], anchor_piece_index: int, global_position: Vector2, from_board: bool) -> void:
	_cancel_drag()
	_drag_piece_indices = []
	for piece_index in piece_indices:
		_drag_piece_indices.append(piece_index)
	_drag_piece_index = anchor_piece_index
	_drag_anchor_piece_index = anchor_piece_index
	_drag_from_board = from_board
	_drag_origin_slots.clear()
	_drag_target_slots.clear()

	if from_board:
		for piece_index in _drag_piece_indices:
			if _piece_slots.has(piece_index):
				_drag_origin_slots[piece_index] = int(_piece_slots[piece_index])
		for piece_index in _drag_piece_indices:
			_remove_piece_from_slot(piece_index, false)
		_refresh_board_feedback()

	_drag_preview = _create_drag_preview(_drag_piece_indices, anchor_piece_index)
	_drag_layer.add_child(_drag_preview)
	_update_drag_preview_position(global_position)

func _create_drag_preview(piece_indices: Array[int], anchor_piece_index: int) -> Control:
	var panel := Control.new()
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE

	var min_col := 0
	var max_col := 0
	var min_row := 0
	var max_row := 0
	for piece_index in piece_indices:
		var delta: Vector2i = _get_piece_delta(anchor_piece_index, piece_index)
		min_col = min(min_col, delta.x)
		max_col = max(max_col, delta.x)
		min_row = min(min_row, delta.y)
		max_row = max(max_row, delta.y)

	var panel_size := Vector2(float(max_col - min_col + 1) * PIECE_CARD_SIZE.x, float(max_row - min_row + 1) * PIECE_CARD_SIZE.y)
	panel.custom_minimum_size = panel_size
	panel.size = panel_size

	for piece_index in piece_indices:
		var tile := Panel.new()
		tile.size = PIECE_CARD_SIZE
		tile.mouse_filter = Control.MOUSE_FILTER_IGNORE
		var delta: Vector2i = _get_piece_delta(anchor_piece_index, piece_index)
		tile.position = Vector2(float(delta.x - min_col) * PIECE_CARD_SIZE.x, float(delta.y - min_row) * PIECE_CARD_SIZE.y)

		var preview := TextureRect.new()
		preview.set_anchors_preset(Control.PRESET_FULL_RECT)
		preview.mouse_filter = Control.MOUSE_FILTER_IGNORE
		preview.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		preview.stretch_mode = TextureRect.STRETCH_SCALE
		preview.texture = _piece_textures[piece_index]
		tile.add_child(preview)
		panel.add_child(tile)

	return panel

func _update_drag_preview_position(global_position: Vector2) -> void:
	if _drag_preview == null:
		return
	_drag_preview.global_position = global_position - _drag_preview.size * 0.5

	var anchor_slot := _find_slot_at(global_position)
	_drag_target_slots = _get_drag_target_slots(anchor_slot)
	_refresh_board_feedback()

func _finish_drag(global_position: Vector2) -> void:
	if _drag_preview == null or _drag_piece_index < 0:
		return

	var anchor_slot := _find_slot_at(global_position)
	var target_slots := _get_drag_target_slots(anchor_slot)
	if not target_slots.is_empty():
		_place_drag_group(target_slots)
	elif _drag_from_board:
		_restore_drag_group()
	_cancel_drag()
	_refresh_board_feedback()

func _find_slot_at(global_position: Vector2) -> int:
	for index in range(_slot_controls.size()):
		var slot := _slot_controls[index]
		var rect := Rect2(slot.global_position, slot.size)
		if rect.has_point(global_position):
			return index
	return -1

func _get_drag_target_slots(anchor_slot: int) -> Dictionary:
	if anchor_slot < 0:
		return {}

	var targets := {}
	for piece_index in _drag_piece_indices:
		var target_slot: int = _get_target_slot(anchor_slot, _drag_anchor_piece_index, piece_index)
		if target_slot < 0:
			return {}
		if targets.has(target_slot):
			return {}
		if _slot_pieces.has(target_slot):
			return {}
		targets[target_slot] = piece_index
	return targets

func _place_drag_group(target_slots: Dictionary) -> void:
	for slot_variant in target_slots.keys():
		var target_slot: int = int(slot_variant)
		var piece_index: int = int(target_slots[slot_variant])
		_place_piece_in_slot(piece_index, target_slot, false)
	_refresh_board_feedback()
	_play_success_animation(target_slots)
func _restore_drag_group() -> void:
	for piece_index in _drag_piece_indices:
		if _drag_origin_slots.has(piece_index):
			_place_piece_in_slot(piece_index, int(_drag_origin_slots[piece_index]), false)
	_refresh_board_feedback()

func _get_target_slot(anchor_slot: int, anchor_piece_index: int, piece_index: int) -> int:
	var anchor_rc := _slot_to_row_col(anchor_slot)
	var delta := _get_piece_delta(anchor_piece_index, piece_index)
	return _row_col_to_slot(anchor_rc.y + delta.y, anchor_rc.x + delta.x)

func _place_piece_in_slot(piece_index: int, slot_index: int, refresh_feedback := true) -> void:
	var source: Button = _piece_sources.get(piece_index)
	if source != null:
		source.disabled = true
		source.modulate = Color(1.0, 1.0, 1.0, 0.35)

	var slot := _slot_controls[slot_index]
	var piece := Button.new()
	piece.flat = true
	piece.text = ""
	piece.focus_mode = Control.FOCUS_NONE
	piece.size = slot.size
	piece.position = slot.position
	piece.gui_input.connect(_on_board_piece_gui_input.bind(piece_index, slot_index))

	var preview := TextureRect.new()
	preview.set_anchors_preset(Control.PRESET_FULL_RECT)
	preview.mouse_filter = Control.MOUSE_FILTER_IGNORE
	preview.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	preview.stretch_mode = TextureRect.STRETCH_SCALE
	preview.texture = _piece_textures[piece_index]
	piece.add_child(preview)

	_placed_pieces_layer.add_child(piece)
	_piece_slots[piece_index] = slot_index
	_slot_pieces[slot_index] = piece_index
	_placed_piece_nodes[piece_index] = piece
	_try_merge_piece_into_neighbors(piece_index)
	if refresh_feedback:
		_refresh_board_feedback()

func _remove_piece_from_slot(piece_index: int, refresh_feedback := true) -> void:
	if not _piece_slots.has(piece_index):
		return

	var slot_index: int = _piece_slots[piece_index]
	_piece_slots.erase(piece_index)
	_slot_pieces.erase(slot_index)

	var piece_node: Control = _placed_piece_nodes.get(piece_index)
	if piece_node != null:
		piece_node.queue_free()
	_placed_piece_nodes.erase(piece_index)
	if refresh_feedback:
		_refresh_board_feedback()

func _cancel_drag() -> void:
	_drag_piece_index = -1
	_drag_piece_indices.clear()
	_drag_origin_slots.clear()
	_drag_target_slots.clear()
	_drag_from_board = false
	_drag_anchor_piece_index = -1
	if _drag_preview != null:
		_drag_preview.queue_free()
		_drag_preview = null

func _slot_to_row_col(slot_index: int) -> Vector2i:
	return Vector2i(slot_index % _current_cols, slot_index / _current_cols)

func _row_col_to_slot(row: int, col: int) -> int:
	if row < 0 or col < 0 or row >= _current_rows or col >= _current_cols:
		return -1
	return row * _current_cols + col

func _get_piece_delta(anchor_piece_index: int, piece_index: int) -> Vector2i:
	var anchor_rc := _slot_to_row_col(anchor_piece_index)
	var piece_rc := _slot_to_row_col(piece_index)
	return Vector2i(piece_rc.x - anchor_rc.x, piece_rc.y - anchor_rc.y)

func _get_drag_piece_indices(piece_index: int) -> Array[int]:
	if _piece_groups.has(piece_index):
		var group_id: int = int(_piece_groups[piece_index])
		if _groups.has(group_id):
			var grouped: Array[int] = []
			for member in _groups[group_id]:
				grouped.append(int(member))
			return grouped
	return [piece_index]

func _is_piece_in_correct_slot(piece_index: int) -> bool:
	return _piece_slots.has(piece_index) and int(_piece_slots[piece_index]) == piece_index

func _try_merge_piece_into_neighbors(piece_index: int) -> void:
	if not _piece_slots.has(piece_index):
		return

	for neighbor in _get_adjacent_piece_indices(piece_index):
		if not _piece_slots.has(neighbor):
			continue
		if not _pieces_match_as_neighbors(piece_index, neighbor):
			continue
		_merge_piece_pair(piece_index, neighbor)

func _pieces_match_as_neighbors(a: int, b: int) -> bool:
	if not _piece_slots.has(a) or not _piece_slots.has(b):
		return false

	var piece_a_slot := _slot_to_row_col(int(_piece_slots[a]))
	var piece_b_slot := _slot_to_row_col(int(_piece_slots[b]))
	var current_delta := Vector2i(piece_b_slot.x - piece_a_slot.x, piece_b_slot.y - piece_a_slot.y)
	var solved_a := _slot_to_row_col(a)
	var solved_b := _slot_to_row_col(b)
	var solved_delta := Vector2i(solved_b.x - solved_a.x, solved_b.y - solved_a.y)
	return current_delta == solved_delta and abs(current_delta.x) + abs(current_delta.y) == 1
func _merge_piece_pair(a: int, b: int) -> void:
	var has_a := _piece_groups.has(a)
	var has_b := _piece_groups.has(b)

	if has_a and has_b:
		var group_a: int = int(_piece_groups[a])
		var group_b: int = int(_piece_groups[b])
		if group_a == group_b:
			return
		_absorb_group(group_a, group_b)
		return

	if has_a:
		_add_piece_to_group(int(_piece_groups[a]), b)
		return

	if has_b:
		_add_piece_to_group(int(_piece_groups[b]), a)
		return

	_create_group([a, b])

func _create_group(members: Array[int]) -> void:
	var group_id: int = _next_group_id
	_next_group_id += 1
	var stored: Array[int] = []
	for member in members:
		if stored.has(member):
			continue
		stored.append(member)
		_piece_groups[member] = group_id
	_groups[group_id] = stored

func _add_piece_to_group(group_id: int, piece_index: int) -> void:
	if not _groups.has(group_id):
		return
	var members: Array = _groups[group_id]
	if not members.has(piece_index):
		members.append(piece_index)
	_groups[group_id] = members
	_piece_groups[piece_index] = group_id

func _absorb_group(target_group_id: int, source_group_id: int) -> void:
	if not _groups.has(target_group_id) or not _groups.has(source_group_id):
		return
	var target_members: Array = _groups[target_group_id]
	for member in _groups[source_group_id]:
		var piece_index: int = int(member)
		if not target_members.has(piece_index):
			target_members.append(piece_index)
		_piece_groups[piece_index] = target_group_id
	_groups[target_group_id] = target_members
	_groups.erase(source_group_id)

func _refresh_groups_after_move() -> void:
	if _drag_from_board:
		return
	var invalid_groups: Array[int] = []
	for group_variant in _groups.keys():
		var group_id: int = int(group_variant)
		var members: Array = _groups[group_id]
		var valid := true
		for member in members:
			var piece_index: int = int(member)
			if not _piece_slots.has(piece_index):
				valid = false
				break
		if not valid:
			invalid_groups.append(group_id)
	for group_id in invalid_groups:
		var members: Array = _groups[group_id]
		for member in members:
			_piece_groups.erase(int(member))
		_groups.erase(group_id)

func _get_adjacent_piece_indices(piece_index: int) -> Array[int]:
	var rc := _slot_to_row_col(piece_index)
	var result: Array[int] = []
	var offsets := [Vector2i(1, 0), Vector2i(-1, 0), Vector2i(0, 1), Vector2i(0, -1)]
	for offset in offsets:
		var neighbor: int = _row_col_to_slot(rc.y + offset.y, rc.x + offset.x)
		if neighbor >= 0:
			result.append(neighbor)
	return result

func _refresh_board_feedback() -> void:
	_refresh_groups_after_move()

	var correct_count := 0
	for slot_index in range(_slot_controls.size()):
		var slot := _slot_controls[slot_index]
		if _drag_target_slots.has(slot_index):
			slot.modulate = Color(0.62, 0.82, 1.0, 0.95)
			continue
		if _slot_pieces.has(slot_index):
			var piece_index: int = int(_slot_pieces[slot_index])
			if piece_index == slot_index:
				slot.modulate = Color(0.72, 0.95, 0.78, 0.95)
				correct_count += 1
			else:
				slot.modulate = Color(1.0, 0.82, 0.62, 0.95)
		else:
			slot.modulate = Color(1.0, 1.0, 1.0, 0.6)

	for piece_variant in _placed_piece_nodes.keys():
		var piece_index: int = int(piece_variant)
		var piece: Control = _placed_piece_nodes[piece_index] as Control
		if piece == null:
			continue
		if _piece_groups.has(piece_index):
			piece.modulate = Color(0.66, 0.9, 1.0, 1.0)
		elif _is_piece_in_correct_slot(piece_index):
			piece.modulate = Color(0.86, 1.0, 0.88, 1.0)
		else:
			piece.modulate = Color(1.0, 0.9, 0.78, 1.0)

	var total: int = max(1, _current_rows * _current_cols)
	puzzle_star_label.text = "Matched %d / %d" % [correct_count, total]
	if correct_count >= total and not _result_shown:
		_show_result_overlay(correct_count, total)

func _play_success_animation(target_slots: Dictionary) -> void:
	var animated_slots := {}
	var animated_pieces := {}

	for slot_variant in target_slots.keys():
		var slot_index: int = int(slot_variant)
		if not _slot_pieces.has(slot_index):
			continue
		var piece_index: int = int(_slot_pieces[slot_index])
		if piece_index != slot_index:
			continue

		if _piece_groups.has(piece_index):
			var group_id: int = int(_piece_groups[piece_index])
			for member in _groups.get(group_id, []):
				var member_index: int = int(member)
				if _piece_slots.has(member_index):
					animated_slots[int(_piece_slots[member_index])] = true
				animated_pieces[member_index] = true
		else:
			animated_slots[slot_index] = true
			animated_pieces[piece_index] = true

	for slot_variant in animated_slots.keys():
		var slot: Control = _slot_controls[int(slot_variant)]
		var slot_tween := create_tween()
		slot_tween.tween_property(slot, "scale", Vector2(1.06, 1.06), 0.08)
		slot_tween.tween_property(slot, "scale", Vector2.ONE, 0.12)

	for piece_variant in animated_pieces.keys():
		var piece: Control = _placed_piece_nodes.get(int(piece_variant)) as Control
		if piece == null:
			continue
		var base_modulate: Color = piece.modulate
		var piece_tween := create_tween()
		piece_tween.tween_property(piece, "scale", Vector2(1.08, 1.08), 0.08)
		piece_tween.parallel().tween_property(piece, "modulate", Color(1.0, 1.0, 1.0, 1.0), 0.08)
		piece_tween.tween_property(piece, "scale", Vector2.ONE, 0.14)
		piece_tween.parallel().tween_property(piece, "modulate", base_modulate, 0.14)
func _build_piece_texture(texture: Texture2D, rows: int, cols: int, index: int) -> Texture2D:
	var row := index / cols
	var col := index % cols
	var source_image := texture.get_image()
	var output := Image.create(PIECE_PREVIEW_SIZE, PIECE_PREVIEW_SIZE, false, Image.FORMAT_RGBA8)
	var polygon := PackedVector2Array(_build_piece_outline(rows, cols, row, col))
	var texture_width := source_image.get_width()
	var texture_height := source_image.get_height()
	var preview_half_extent := 0.5 + TAB_DEPTH_RATIO

	for y in range(PIECE_PREVIEW_SIZE):
		for x in range(PIECE_PREVIEW_SIZE):
			var local_x := lerpf(-preview_half_extent, preview_half_extent, (float(x) + 0.5) / float(PIECE_PREVIEW_SIZE))
			var local_y := lerpf(-preview_half_extent, preview_half_extent, (float(y) + 0.5) / float(PIECE_PREVIEW_SIZE))
			var local_point := Vector2(local_x, local_y)
			if not Geometry2D.is_point_in_polygon(local_point, polygon):
				output.set_pixel(x, y, Color(0.0, 0.0, 0.0, 0.0))
				continue

			var uv := Vector2(
				(float(col) + 0.5 + local_x) / float(cols),
				(float(row) + 0.5 + local_y) / float(rows)
			)
			uv.x = clamp(uv.x, 0.0, 1.0)
			uv.y = clamp(uv.y, 0.0, 1.0)
			var sample_x := clampi(int(round(uv.x * float(texture_width - 1))), 0, texture_width - 1)
			var sample_y := clampi(int(round(uv.y * float(texture_height - 1))), 0, texture_height - 1)
			output.set_pixel(x, y, source_image.get_pixel(sample_x, sample_y))

	return ImageTexture.create_from_image(output)

func _build_piece_outline(rows: int, cols: int, row: int, col: int) -> Array[Vector2]:
	var points: Array[Vector2] = []
	var half_size := 0.5

	_append_horizontal_edge(points, -half_size, half_size, -half_size, _get_piece_edge(rows, cols, row, col, "top"), -1.0)
	_append_vertical_edge(points, -half_size, half_size, half_size, _get_piece_edge(rows, cols, row, col, "right"), 1.0)
	_append_horizontal_edge(points, half_size, -half_size, half_size, _get_piece_edge(rows, cols, row, col, "bottom"), 1.0)
	_append_vertical_edge(points, half_size, -half_size, -half_size, _get_piece_edge(rows, cols, row, col, "left"), -1.0)
	return points

func _get_piece_edge(rows: int, cols: int, row: int, col: int, side: String) -> int:
	match side:
		"top":
			if row == 0:
				return 0
			return -_deterministic_edge(row - 1, col, 17)
		"right":
			if col == cols - 1:
				return 0
			return _deterministic_edge(row, col, 53)
		"bottom":
			if row == rows - 1:
				return 0
			return _deterministic_edge(row, col, 17)
		"left":
			if col == 0:
				return 0
			return -_deterministic_edge(row, col - 1, 53)
	return 0

func _deterministic_edge(row: int, col: int, salt: int) -> int:
	var hash := row * 92821 + col * 68917 + salt * 2971
	return 1 if (hash & 1) == 0 else -1

func _append_horizontal_edge(points: Array[Vector2], start_x: float, end_x: float, y: float, edge_shape: int, outward_sign: float) -> void:
	var direction: float = sign(end_x - start_x)
	var tab_width: float = TAB_WIDTH_RATIO
	var tab_depth: float = TAB_DEPTH_RATIO * outward_sign * float(edge_shape)
	var center_x: float = (start_x + end_x) * 0.5
	var tab_start: float = center_x - tab_width * 0.5 * direction
	var tab_end: float = center_x + tab_width * 0.5 * direction

	_add_point(points, Vector2(start_x, y))
	_add_point(points, Vector2(tab_start, y))

	if edge_shape != 0:
		for segment in range(1, 10):
			var t: float = float(segment) / 10.0
			var x: float = lerpf(tab_start, tab_end, t)
			var offset: float = sin(t * PI) * tab_depth
			_add_point(points, Vector2(x, y + offset))

	_add_point(points, Vector2(tab_end, y))
	_add_point(points, Vector2(end_x, y))

func _append_vertical_edge(points: Array[Vector2], start_y: float, end_y: float, x: float, edge_shape: int, outward_sign: float) -> void:
	var direction: float = sign(end_y - start_y)
	var tab_width: float = TAB_WIDTH_RATIO
	var tab_depth: float = TAB_DEPTH_RATIO * outward_sign * float(edge_shape)
	var center_y: float = (start_y + end_y) * 0.5
	var tab_start: float = center_y - tab_width * 0.5 * direction
	var tab_end: float = center_y + tab_width * 0.5 * direction

	_add_point(points, Vector2(x, start_y))
	_add_point(points, Vector2(x, tab_start))

	if edge_shape != 0:
		for segment in range(1, 10):
			var t: float = float(segment) / 10.0
			var y: float = lerpf(tab_start, tab_end, t)
			var offset: float = sin(t * PI) * tab_depth
			_add_point(points, Vector2(x + offset, y))

	_add_point(points, Vector2(x, tab_end))
	_add_point(points, Vector2(x, end_y))

func _add_point(points: Array[Vector2], point: Vector2) -> void:
	if points.is_empty() or points[-1].distance_to(point) > 0.0001:
		points.append(point)


func _load_selected_texture() -> Texture2D:
	var texture_path := _resolve_selected_texture_path()
	if not texture_path.is_empty() and ResourceLoader.exists(texture_path):
		var loaded := load(texture_path)
		if loaded is Texture2D:
			return loaded

	var image := Image.create(32, 32, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.75, 0.82, 0.93, 1.0))
	return ImageTexture.create_from_image(image)

func _resolve_selected_texture_path() -> String:
	if _selected_image_file.is_empty():
		return ""

	var theme: Dictionary = _theme_lookup.get(_theme_id, {})
	if theme.is_empty():
		return ""

	return _build_texture_path(str(theme.get("folder", _theme_id)), _selected_image_file)

func _build_texture_path(folder: String, file_name: String) -> String:
	if file_name.is_empty():
		return ""
	return "res://assets/textures/%s/%s" % [folder, file_name]

func _refresh_stars() -> void:
	var largest := int(max(rows_box.value, cols_box.value))
	var stars := 1 if largest <= 3 else (2 if largest == 4 else 3)
	if rotation_toggle.button_pressed:
		stars += 1
	if not storage_toggle.button_pressed:
		stars += 1
	stars = min(stars, 5)
	stars_label.text = "Current Difficulty: %d / 5" % stars
