extends Control

@onready var theme_select_ui: Control = $ThemeSelectUI
@onready var image_select_ui: Control = $ImageSelectUI
@onready var difficulty_setup_ui: Control = $DifficultySetupUI
@onready var title_label: Label = $ImageSelectUI/SafeArea/ContentColumn/HeaderRow/Title
@onready var difficulty_title: Label = $DifficultySetupUI/SafeArea/ContentColumn/TitlePanel/TitleMargin/Title
@onready var stars_label: Label = $DifficultySetupUI/SafeArea/ContentColumn/StarsPanel/StarsMargin/StarsLabel
@onready var rows_box: SpinBox = $DifficultySetupUI/SafeArea/ContentColumn/RowsBox
@onready var cols_box: SpinBox = $DifficultySetupUI/SafeArea/ContentColumn/ColumnsBox
@onready var rotation_toggle: CheckButton = $DifficultySetupUI/SafeArea/ContentColumn/RotationToggle
@onready var storage_toggle: CheckButton = $DifficultySetupUI/SafeArea/ContentColumn/StorageToggle

var _theme_name := "Scenery"

func _ready() -> void:
    _bind_buttons()
    _show_screen(theme_select_ui)
    _refresh_stars()

func _bind_buttons() -> void:
    $ThemeSelectUI/SafeArea/ContentColumn/ThemeList/AnimalsButton.pressed.connect(func(): _open_theme("Animals"))
    $ThemeSelectUI/SafeArea/ContentColumn/ThemeList/SceneryButton.pressed.connect(func(): _open_theme("Scenery"))
    $ThemeSelectUI/SafeArea/ContentColumn/ThemeList/ArchitectureButton.pressed.connect(func(): _open_theme("Architecture"))
    $ThemeSelectUI/SafeArea/ContentColumn/AlbumButton.pressed.connect(func(): _open_theme("Album"))

    $ImageSelectUI/SafeArea/ContentColumn/HeaderRow/BackButton.pressed.connect(func(): _show_screen(theme_select_ui))
    $ImageSelectUI/SafeArea/ContentColumn/Scroll/ImageList/ImageCard01.pressed.connect(_open_difficulty)
    $ImageSelectUI/SafeArea/ContentColumn/Scroll/ImageList/ImageCard02.pressed.connect(_open_difficulty)
    $ImageSelectUI/SafeArea/ContentColumn/Scroll/ImageList/ImageCard03.pressed.connect(_open_difficulty)
    $ImageSelectUI/SafeArea/ContentColumn/Scroll/ImageList/ImageCard04.pressed.connect(_open_difficulty)

    rows_box.value_changed.connect(func(_v): _refresh_stars())
    cols_box.value_changed.connect(func(_v): _refresh_stars())
    rotation_toggle.toggled.connect(func(_v): _refresh_stars())
    storage_toggle.toggled.connect(func(_v): _refresh_stars())
    $DifficultySetupUI/SafeArea/ContentColumn/BackButton.pressed.connect(func(): _show_screen(image_select_ui))
    $DifficultySetupUI/SafeArea/ContentColumn/StartButton.pressed.connect(func(): _show_screen(theme_select_ui))

func _open_theme(theme_name: String) -> void:
    _theme_name = theme_name
    title_label.text = theme_name
    difficulty_title.text = theme_name + " Difficulty"
    _show_screen(image_select_ui)

func _open_difficulty() -> void:
    difficulty_title.text = _theme_name + " Difficulty"
    _refresh_stars()
    _show_screen(difficulty_setup_ui)

func _show_screen(target: Control) -> void:
    theme_select_ui.visible = target == theme_select_ui
    image_select_ui.visible = target == image_select_ui
    difficulty_setup_ui.visible = target == difficulty_setup_ui

func _refresh_stars() -> void:
    var largest := int(max(rows_box.value, cols_box.value))
    var stars := 1 if largest <= 3 else (2 if largest == 4 else 3)
    if rotation_toggle.button_pressed:
        stars += 1
    if not storage_toggle.button_pressed:
        stars += 1
    stars = min(stars, 5)
    stars_label.text = "Current Difficulty: %d / 5" % stars